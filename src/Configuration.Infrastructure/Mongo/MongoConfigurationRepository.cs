using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Configuration.Core.Abstractions;
using Configuration.Core.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace Configuration.Infrastructure.Mongo
{
    public sealed class MongoConfigurationRepository : IConfigurationRepository
    {
        private readonly IMongoCollection<ConfigurationDocument> _collection;

        public MongoConfigurationRepository(string connectionString, string databaseName = "secilstore_config", string collectionName = "configurationEntries")
        {
            var client = new MongoClient(connectionString);
            var db = client.GetDatabase(databaseName);
            _collection = db.GetCollection<ConfigurationDocument>(collectionName);
            EnsureIndexesAsync(_collection).GetAwaiter().GetResult();
        }

        public async Task<IReadOnlyList<ConfigurationEntry>> GetActiveEntriesAsync(string applicationName, CancellationToken cancellationToken = default)
        {
            var filter = Builders<ConfigurationDocument>.Filter.And(
                Builders<ConfigurationDocument>.Filter.Eq(x => x.ApplicationName, applicationName),
                Builders<ConfigurationDocument>.Filter.Eq(x => x.IsActive, true)
            );
            var docs = await _collection.Find(filter).ToListAsync(cancellationToken);
            return docs.Select(MapToEntry).ToList();
        }

        public async Task<IReadOnlyList<ConfigurationEntry>> GetAllEntriesAsync(string applicationName, CancellationToken cancellationToken = default)
        {
            var filter = Builders<ConfigurationDocument>.Filter.Eq(x => x.ApplicationName, applicationName);
            var docs = await _collection.Find(filter).ToListAsync(cancellationToken);
            return docs.Select(MapToEntry).ToList();
        }

        public async Task<IReadOnlyList<ConfigurationEntry>> GetActiveEntriesUpdatedSinceAsync(string applicationName, DateTime updatedSinceUtc, CancellationToken cancellationToken = default)
        {
            var filter = Builders<ConfigurationDocument>.Filter.And(
                Builders<ConfigurationDocument>.Filter.Eq(x => x.ApplicationName, applicationName),
                Builders<ConfigurationDocument>.Filter.Eq(x => x.IsActive, true),
                Builders<ConfigurationDocument>.Filter.Gt(x => x.UpdatedAtUtc, updatedSinceUtc)
            );
            var docs = await _collection.Find(filter).ToListAsync(cancellationToken);
            return docs.Select(MapToEntry).ToList();
        }

        public async Task<ConfigurationEntry> CreateAsync(ConfigurationEntry entry, CancellationToken cancellationToken = default)
        {
            var toPersist = new ConfigurationEntry
            {
                Id = entry.Id,
                ApplicationName = entry.ApplicationName,
                Name = entry.Name,
                Type = entry.Type,
                Value = entry.Value,
                IsActive = entry.IsActive,
                UpdatedAtUtc = DateTime.UtcNow
            };
            var doc = MapToDocument(toPersist);
            await _collection.InsertOneAsync(doc, cancellationToken: cancellationToken);
            return MapToEntry(doc);
        }

        public async Task<ConfigurationEntry?> UpdateAsync(string id, string applicationName, ConfigurationEntry entry, CancellationToken cancellationToken = default)
        {
            // Güvenlik: Sadece belirtilen applicationName'e ait kayıt güncellenebilir
            var filter = Builders<ConfigurationDocument>.Filter.And(
                Builders<ConfigurationDocument>.Filter.Eq(x => x.Id, ObjectId.Parse(id)),
                Builders<ConfigurationDocument>.Filter.Eq(x => x.ApplicationName, applicationName)
            );
            var update = Builders<ConfigurationDocument>.Update
                .Set(x => x.Name, entry.Name)
                .Set(x => x.Type, (int)entry.Type)
                .Set(x => x.Value, entry.Value)
                .Set(x => x.IsActive, entry.IsActive)
                .Set(x => x.UpdatedAtUtc, DateTime.UtcNow);
            var result = await _collection.FindOneAndUpdateAsync(filter, update, new FindOneAndUpdateOptions<ConfigurationDocument> { ReturnDocument = ReturnDocument.After }, cancellationToken);
            return result is null ? null : MapToEntry(result);
        }

        public async Task<bool> SetActiveAsync(string id, string applicationName, bool isActive, CancellationToken cancellationToken = default)
        {
            // Güvenlik: Sadece belirtilen applicationName'e ait kayıt değiştirilebilir
            var filter = Builders<ConfigurationDocument>.Filter.And(
                Builders<ConfigurationDocument>.Filter.Eq(x => x.Id, ObjectId.Parse(id)),
                Builders<ConfigurationDocument>.Filter.Eq(x => x.ApplicationName, applicationName)
            );
            var update = Builders<ConfigurationDocument>.Update
                .Set(x => x.IsActive, isActive)
                .Set(x => x.UpdatedAtUtc, DateTime.UtcNow);
            var result = await _collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
            return result.IsAcknowledged && result.ModifiedCount > 0;
        }

        private static ConfigurationEntry MapToEntry(ConfigurationDocument d)
        {
            return new ConfigurationEntry
            {
                Id = d.Id.ToString(),
                ApplicationName = d.ApplicationName,
                Name = d.Name,
                Type = (ConfigurationValueType)d.Type,
                Value = d.Value,
                IsActive = d.IsActive,
                UpdatedAtUtc = d.UpdatedAtUtc
            };
        }

        private static ConfigurationDocument MapToDocument(ConfigurationEntry e)
        {
            return new ConfigurationDocument
            {
                Id = string.IsNullOrWhiteSpace(e.Id) ? ObjectId.GenerateNewId() : ObjectId.Parse(e.Id),
                ApplicationName = e.ApplicationName,
                Name = e.Name,
                Type = (int)e.Type,
                Value = e.Value,
                IsActive = e.IsActive,
                UpdatedAtUtc = e.UpdatedAtUtc == default ? DateTime.UtcNow : e.UpdatedAtUtc
            };
        }

        private static async Task EnsureIndexesAsync(IMongoCollection<ConfigurationDocument> collection)
        {
            var indexKeys = Builders<ConfigurationDocument>.IndexKeys
                .Ascending(x => x.ApplicationName)
                .Ascending(x => x.Name);
            var model = new CreateIndexModel<ConfigurationDocument>(indexKeys, new CreateIndexOptions { Unique = true, Name = "ux_app_name" });
            await collection.Indexes.CreateOneAsync(model);

            var activeIdx = Builders<ConfigurationDocument>.IndexKeys
                .Ascending(x => x.ApplicationName)
                .Ascending(x => x.IsActive)
                .Descending(x => x.UpdatedAtUtc);
            await collection.Indexes.CreateOneAsync(new CreateIndexModel<ConfigurationDocument>(activeIdx, new CreateIndexOptions { Name = "idx_active_app_updated" }));
        }

        private sealed class ConfigurationDocument
        {
            [BsonId]
            public ObjectId Id { get; set; }
            [BsonElement("applicationName")]
            public string ApplicationName { get; set; } = string.Empty;
            [BsonElement("name")]
            public string Name { get; set; } = string.Empty;
            [BsonElement("type")]
            public int Type { get; set; }
            [BsonElement("value")]
            public string Value { get; set; } = string.Empty;
            [BsonElement("isActive")]
            public bool IsActive { get; set; }
            [BsonElement("updatedAt")]
            public DateTime UpdatedAtUtc { get; set; }
        }
    }
}


