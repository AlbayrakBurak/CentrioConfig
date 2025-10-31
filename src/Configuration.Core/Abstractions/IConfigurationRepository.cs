using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Configuration.Core.Models;

namespace Configuration.Core.Abstractions
{
    public interface IConfigurationRepository
    {
        Task<IReadOnlyList<ConfigurationEntry>> GetActiveEntriesAsync(string applicationName, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<ConfigurationEntry>> GetAllEntriesAsync(string applicationName, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<ConfigurationEntry>> GetActiveEntriesUpdatedSinceAsync(string applicationName, DateTime updatedSinceUtc, CancellationToken cancellationToken = default);

        Task<ConfigurationEntry> CreateAsync(ConfigurationEntry entry, CancellationToken cancellationToken = default);

        Task<ConfigurationEntry?> UpdateAsync(string id, string applicationName, ConfigurationEntry entry, CancellationToken cancellationToken = default);

        Task<bool> SetActiveAsync(string id, string applicationName, bool isActive, CancellationToken cancellationToken = default);
    }
}


