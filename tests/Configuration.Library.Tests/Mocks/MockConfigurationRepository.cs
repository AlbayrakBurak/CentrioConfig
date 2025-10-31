using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Configuration.Core.Abstractions;
using Configuration.Core.Models;

namespace Configuration.Library.Tests.Mocks;

internal sealed class MockConfigurationRepository : IConfigurationRepository
{
    private readonly List<ConfigurationEntry> _entries = new();

    public void AddEntry(ConfigurationEntry entry)
    {
        _entries.Add(entry);
    }

    public void Clear()
    {
        _entries.Clear();
    }

    public Task<IReadOnlyList<ConfigurationEntry>> GetActiveEntriesAsync(string applicationName, CancellationToken cancellationToken = default)
    {
        var filtered = _entries.FindAll(e => e.ApplicationName == applicationName && e.IsActive);
        return Task.FromResult<IReadOnlyList<ConfigurationEntry>>(new ReadOnlyCollection<ConfigurationEntry>(filtered));
    }

    public Task<IReadOnlyList<ConfigurationEntry>> GetAllEntriesAsync(string applicationName, CancellationToken cancellationToken = default)
    {
        var filtered = _entries.FindAll(e => e.ApplicationName == applicationName);
        return Task.FromResult<IReadOnlyList<ConfigurationEntry>>(new ReadOnlyCollection<ConfigurationEntry>(filtered));
    }

    public Task<IReadOnlyList<ConfigurationEntry>> GetActiveEntriesUpdatedSinceAsync(string applicationName, DateTime updatedSinceUtc, CancellationToken cancellationToken = default)
    {
        var filtered = _entries.FindAll(e => 
            e.ApplicationName == applicationName && 
            e.IsActive && 
            e.UpdatedAtUtc > updatedSinceUtc);
        return Task.FromResult<IReadOnlyList<ConfigurationEntry>>(new ReadOnlyCollection<ConfigurationEntry>(filtered));
    }

    public Task<ConfigurationEntry> CreateAsync(ConfigurationEntry entry, CancellationToken cancellationToken = default)
    {
        var created = new ConfigurationEntry
        {
            Id = Guid.NewGuid().ToString(),
            ApplicationName = entry.ApplicationName,
            Name = entry.Name,
            Type = entry.Type,
            Value = entry.Value,
            IsActive = entry.IsActive,
            UpdatedAtUtc = entry.UpdatedAtUtc
        };
        _entries.Add(created);
        return Task.FromResult(created);
    }

    public Task<ConfigurationEntry?> UpdateAsync(string id, string applicationName, ConfigurationEntry entry, CancellationToken cancellationToken = default)
    {
        var index = _entries.FindIndex(e => e.Id == id && e.ApplicationName == applicationName);
        if (index < 0) return Task.FromResult<ConfigurationEntry?>(null);
        
        var updated = new ConfigurationEntry
        {
            Id = id,
            ApplicationName = entry.ApplicationName,
            Name = entry.Name,
            Type = entry.Type,
            Value = entry.Value,
            IsActive = entry.IsActive,
            UpdatedAtUtc = entry.UpdatedAtUtc
        };
        _entries[index] = updated;
        return Task.FromResult<ConfigurationEntry?>(updated);
    }

    public Task<bool> SetActiveAsync(string id, string applicationName, bool isActive, CancellationToken cancellationToken = default)
    {
        var index = _entries.FindIndex(e => e.Id == id && e.ApplicationName == applicationName);
        if (index < 0) return Task.FromResult(false);
        
        var entry = _entries[index];
        var updated = new ConfigurationEntry
        {
            Id = entry.Id,
            ApplicationName = entry.ApplicationName,
            Name = entry.Name,
            Type = entry.Type,
            Value = entry.Value,
            IsActive = isActive,
            UpdatedAtUtc = entry.UpdatedAtUtc
        };
        _entries[index] = updated;
        return Task.FromResult(true);
    }
}
