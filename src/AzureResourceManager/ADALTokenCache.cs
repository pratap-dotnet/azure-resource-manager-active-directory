using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace AzureResourceManager
{
    public class TokenEntity : TableEntity
    {
        public string WebUserUniqueId { get; set; }
        public byte[] CacheToken { get; set; }
        public DateTime LastWriteTime { get; set; }

        public TokenEntity(string webUniqueUserId, byte[] token, DateTime lastWriteTime)
        {
            WebUserUniqueId = webUniqueUserId;
            CacheToken = token;
            LastWriteTime = lastWriteTime;

            RowKey = webUniqueUserId;
            PartitionKey = string.Empty;
        }

        public TokenEntity()
        {

        }
    }

    public class TokenEntityRepository
    {
        private readonly CloudTable cloudTable;
        public TokenEntityRepository(string connectionString)
        {
            var cloudAccount = CloudStorageAccount.Parse(connectionString);
            cloudTable = cloudAccount.CreateCloudTableClient().GetTableReference("tokencache");
            cloudTable.CreateIfNotExists();
        }

        public IEnumerable<TokenEntity> GetAllTokensForUser(string userId)
        {
            TableQuery<TokenEntity> query = new TableQuery<TokenEntity>()
                .Where(TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, string.Empty),
                    TableOperators.And,
                    TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, userId)));

            return cloudTable.ExecuteQuery(query).ToList();
        }

        public void Delete(TokenEntity tokenEntity)
        {
            var tableOp = TableOperation.Delete(tokenEntity);
            cloudTable.Execute(tableOp);
        }

        public void InsertOrReplace(TokenEntity tokenEntity)
        {
            var tableOp = TableOperation.InsertOrReplace(tokenEntity);
            cloudTable.Execute(tableOp);
        }

    }

    public class TableTokenCache : TokenCache
    {
        private readonly string user;
        private readonly TokenEntityRepository repository;
        private TokenEntity localCache;
        public TableTokenCache(string user, string connectionString)
        {
            this.user = user;
            this.AfterAccess = AfterAccessNotification;
            this.BeforeAccess = BeforeAccessNotification;
            this.repository = new TokenEntityRepository(connectionString);
        }

        public override void Clear()
        {
            base.Clear();
            foreach (var item in repository.GetAllTokensForUser(user))
            {
                repository.Delete(item);
            }
        }

        void BeforeAccessNotification(TokenCacheNotificationArgs args)
        {
            var latestToken = repository.GetAllTokensForUser(user)
                    .OrderByDescending(a => a.LastWriteTime)
                    .FirstOrDefault();

            if (localCache == null || (latestToken != null && localCache.LastWriteTime < latestToken.LastWriteTime))
                localCache = latestToken;

            this.Deserialize(localCache?.CacheToken);
        }

        void AfterAccessNotification(TokenCacheNotificationArgs args)
        {
            if (this.HasStateChanged)
            {
                localCache = new TokenEntity(user, this.Serialize(), DateTime.UtcNow);
                repository.InsertOrReplace(localCache);
            }
        }
    }
}
