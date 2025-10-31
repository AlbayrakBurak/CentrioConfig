using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Configuration.Core.Abstractions;
using Configuration.Core.Models;
using Configuration.Core.Typing;
using Microsoft.Extensions.Logging;

namespace Configuration.Library
{
    public sealed class ConfigurationReader : IAsyncDisposable
    {
        private readonly string _applicationName;
        private readonly IConfigurationRepository _repository;
        private readonly ITypeConverter _typeConverter;
        private readonly ILogger? _logger;
        private readonly int _refreshIntervalMs;

        private ImmutableDictionary<string, (ConfigurationValueType type, string raw)> _cache = ImmutableDictionary<string, (ConfigurationValueType, string)>.Empty;
        private DateTime _lastUpdatedAtUtc = DateTime.MinValue;
        private CancellationTokenSource _cts = new();
        private Task? _refreshTask;

        public ConfigurationReader(
            string applicationName,
            string connectionString,
            int refreshTimerIntervalInMs)
        {
            if (string.IsNullOrWhiteSpace(applicationName)) throw new ArgumentException("Application name is required", nameof(applicationName));
            if (string.IsNullOrWhiteSpace(connectionString)) throw new ArgumentException("Connection string is required", nameof(connectionString));

            _applicationName = applicationName;
            _refreshIntervalMs = Math.Max(1000, refreshTimerIntervalInMs);
            _logger = null;
            _repository = new Configuration.Infrastructure.Mongo.ResilientMongoConfigurationRepository(connectionString, logger: null);
            _typeConverter = new InvariantTypeConverter();

            _refreshTask = Task.Run(() => RefreshLoopAsync(_cts.Token));
        }

        public T GetValue<T>(string key)
        {
            if (!TryGetValue<T>(key, out var value))
            {
                throw new KeyNotFoundException($"Configuration key '{key}' not found or cannot be converted to {typeof(T).Name}.");
            }
            return value!;
        }

        public bool TryGetValue<T>(string key, out T? value)
        {
            value = default;
            if (string.IsNullOrWhiteSpace(key)) return false;
            var snapshot = _cache;
            if (!snapshot.TryGetValue(key, out var item)) return false;

            if (_typeConverter.TryConvert(item.raw, typeof(T), out var boxed) && boxed is T casted)
            {
                value = casted;
                return true;
            }
            return false;
        }

        private async Task RefreshLoopAsync(CancellationToken cancellationToken)
        {
            await InitialLoadAsync(cancellationToken).ConfigureAwait(false);
            var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(_refreshIntervalMs));
            try
            {
                while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
                {
                    await RefreshDeltaAsync(cancellationToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                // graceful shutdown
            }
        }

        private async Task InitialLoadAsync(CancellationToken cancellationToken)
        {
            try
            {
                var all = await _repository.GetActiveEntriesAsync(_applicationName, cancellationToken).ConfigureAwait(false);
                SwapCache(all);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Initial configuration load failed; serving empty or last-known cache.");
            }
        }

        private async Task RefreshDeltaAsync(CancellationToken cancellationToken)
        {
            try
            {
                var changes = await _repository.GetActiveEntriesUpdatedSinceAsync(_applicationName, _lastUpdatedAtUtc, cancellationToken).ConfigureAwait(false);
                if (changes.Count == 0) return;

                var updatedList = new List<ConfigurationEntry>(_cache.Count + changes.Count);
                // Rebuild from existing cache, then apply changes
                foreach (var kvp in _cache)
                {
                    updatedList.Add(new ConfigurationEntry
                    {
                        Id = string.Empty,
                        ApplicationName = _applicationName,
                        Name = kvp.Key,
                        Type = kvp.Value.type,
                        Value = kvp.Value.raw,
                        IsActive = true,
                        UpdatedAtUtc = _lastUpdatedAtUtc
                    });
                }
                foreach (var change in changes)
                {
                    // Upsert or remove (if deactivated it wouldn't be returned here; we rely on full refresh if needed)
                    var idx = updatedList.FindIndex(e => e.Name == change.Name);
                    if (idx >= 0) updatedList[idx] = change; else updatedList.Add(change);
                }
                SwapCache(updatedList);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Configuration delta refresh failed; continuing with last-good cache.");
            }
        }

        private void SwapCache(IReadOnlyList<ConfigurationEntry> entries)
        {
            var builder = ImmutableDictionary.CreateBuilder<string, (ConfigurationValueType, string)>();
            var maxUpdated = DateTime.MinValue;
            foreach (var e in entries)
            {
                if (!e.IsActive) continue;
                builder[e.Name] = (e.Type, e.Value);
                if (e.UpdatedAtUtc > maxUpdated) maxUpdated = e.UpdatedAtUtc;
            }
            Interlocked.Exchange(ref _cache, builder.ToImmutable());
            if (maxUpdated > _lastUpdatedAtUtc)
            {
                _lastUpdatedAtUtc = maxUpdated;
            }
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                _cts.Cancel();
                if (_refreshTask is not null)
                {
                    await _refreshTask.ConfigureAwait(false);
                }
            }
            finally
            {
                _cts.Dispose();
            }
        }
    }

    internal sealed class InvariantTypeConverter : ITypeConverter
    {
        public object Convert(string value, Type targetType)
        {
            if (TryConvert(value, targetType, out var result) && result is not null) return result;
            throw new InvalidCastException($"Cannot convert '{value}' to {targetType.Name}");
        }

        public bool TryConvert(string value, Type targetType, out object? result)
        {
            result = null;
            if (targetType == typeof(string)) { result = value; return true; }
            if (targetType == typeof(int) || targetType == typeof(int?))
            {
                if (int.TryParse(value, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var iv)) { result = iv; return true; }
                return false;
            }
            if (targetType == typeof(double) || targetType == typeof(double?))
            {
                if (double.TryParse(value, System.Globalization.NumberStyles.Float | System.Globalization.NumberStyles.AllowThousands, System.Globalization.CultureInfo.InvariantCulture, out var dv)) { result = dv; return true; }
                return false;
            }
            if (targetType == typeof(bool) || targetType == typeof(bool?))
            {
                if (bool.TryParse(value, out var bv)) { result = bv; return true; }
                if (value == "1") { result = true; return true; }
                if (value == "0") { result = false; return true; }
                return false;
            }
            return false;
        }
    }
}


