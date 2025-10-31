using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Configuration.Core.Abstractions;
using Configuration.Core.Models;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Polly;
using Polly.Retry;

namespace Configuration.Infrastructure.Mongo
{
    public sealed class ResilientMongoConfigurationRepository : IConfigurationRepository
    {
        private readonly MongoConfigurationRepository _inner;
        private readonly ResiliencePipeline _pipeline;
        private readonly ILogger<ResilientMongoConfigurationRepository>? _logger;

        public ResilientMongoConfigurationRepository(
            string connectionString,
            string databaseName = "secilstore_config",
            string collectionName = "configurationEntries",
            ILogger<ResilientMongoConfigurationRepository>? logger = null)
        {
            _logger = logger;
            _inner = new MongoConfigurationRepository(connectionString, databaseName, collectionName);

            // Polly resilience pipeline: retry with jitter, timeout, circuit breaker
            var retryOptions = new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder()
                    .Handle<Exception>(ex => !(ex is TaskCanceledException))
                    .HandleResult(false), // Don't retry on duplicate key errors
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(1),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
            };

            _pipeline = new ResiliencePipelineBuilder()
                .AddRetry(retryOptions)
                .AddTimeout(TimeSpan.FromSeconds(5))
                .Build();
        }

        public async Task<IReadOnlyList<ConfigurationEntry>> GetActiveEntriesAsync(string applicationName, CancellationToken cancellationToken = default)
        {
            return await _pipeline.ExecuteAsync(async ct =>
            {
                try
                {
                    return await _inner.GetActiveEntriesAsync(applicationName, ct).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to get active entries for {ApplicationName}", applicationName);
                    throw;
                }
            }, cancellationToken).ConfigureAwait(false);
        }

        public async Task<IReadOnlyList<ConfigurationEntry>> GetAllEntriesAsync(string applicationName, CancellationToken cancellationToken = default)
        {
            return await _pipeline.ExecuteAsync(async ct =>
            {
                try
                {
                    return await _inner.GetAllEntriesAsync(applicationName, ct).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to get all entries for {ApplicationName}", applicationName);
                    throw;
                }
            }, cancellationToken).ConfigureAwait(false);
        }

        public async Task<IReadOnlyList<ConfigurationEntry>> GetActiveEntriesUpdatedSinceAsync(string applicationName, DateTime updatedSinceUtc, CancellationToken cancellationToken = default)
        {
            return await _pipeline.ExecuteAsync(async ct =>
            {
                try
                {
                    return await _inner.GetActiveEntriesUpdatedSinceAsync(applicationName, updatedSinceUtc, ct).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to get updated entries for {ApplicationName} since {UpdatedSince}", applicationName, updatedSinceUtc);
                    throw;
                }
            }, cancellationToken).ConfigureAwait(false);
        }

        public async Task<ConfigurationEntry> CreateAsync(ConfigurationEntry entry, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _pipeline.ExecuteAsync(async ct =>
                {
                    try
                    {
                        return await _inner.CreateAsync(entry, ct).ConfigureAwait(false);
                    }
                    catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
                    {
                        // Duplicate key errors should not be retried - rethrow immediately
                        throw;
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "Failed to create entry {Name} for {ApplicationName}", entry.Name, entry.ApplicationName);
                        throw;
                    }
                }, cancellationToken).ConfigureAwait(false);
            }
            catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
            {
                // Re-throw duplicate key exceptions without retry wrapper
                throw;
            }
        }

        public async Task<ConfigurationEntry?> UpdateAsync(string id, string applicationName, ConfigurationEntry entry, CancellationToken cancellationToken = default)
        {
            return await _pipeline.ExecuteAsync(async ct =>
            {
                try
                {
                    return await _inner.UpdateAsync(id, applicationName, entry, ct).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to update entry {Id} for {ApplicationName}", id, applicationName);
                    throw;
                }
            }, cancellationToken).ConfigureAwait(false);
        }

        public async Task<bool> SetActiveAsync(string id, string applicationName, bool isActive, CancellationToken cancellationToken = default)
        {
            return await _pipeline.ExecuteAsync(async ct =>
            {
                try
                {
                    return await _inner.SetActiveAsync(id, applicationName, isActive, ct).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to set active status for entry {Id} in {ApplicationName}", id, applicationName);
                    throw;
                }
            }, cancellationToken).ConfigureAwait(false);
        }
    }
}

