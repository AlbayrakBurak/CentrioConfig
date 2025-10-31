using System;

namespace Configuration.Core.Models
{
    public enum ConfigurationValueType
    {
        String = 0,
        Int = 1,
        Double = 2,
        Bool = 3
    }

    public sealed class ConfigurationEntry
    {
        public string Id { get; init; } = string.Empty;
        public string ApplicationName { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public ConfigurationValueType Type { get; init; }
        public string Value { get; init; } = string.Empty;
        public bool IsActive { get; init; }
        public DateTime UpdatedAtUtc { get; init; }
    }
}


