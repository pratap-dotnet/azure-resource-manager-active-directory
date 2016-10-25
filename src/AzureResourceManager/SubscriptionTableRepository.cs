using System;
using System.Collections.Generic;
using System.Linq;
using AzureResourceManager.Models;
using Microsoft.Extensions.Options;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace AzureResourceManager
{
    public class SubscriptionTableEntity: TableEntity
    {
        public string DirectoryId { get; set; }
        public DateTime ConnectedOn { get; set; }
        
        public SubscriptionTableEntity(string id, string directoryId, string connectedBy, DateTime connectedOn)
        {
            this.ConnectedOn = connectedOn;
            this.DirectoryId = directoryId;

            this.PartitionKey = connectedBy;
            this.RowKey = id;
        }

        public SubscriptionTableEntity()
        {

        }

        public Subscription ToModel()
        {
            return new Subscription
            {
                DirectoryId = DirectoryId,
                ConnectedBy = PartitionKey,
                ConnectedOn = ConnectedOn,
                Id = RowKey
            };
        }
    }


    public interface ISubscriptionRepository
    {
        IEnumerable<Subscription> GetAllSubscriptionsForUser(string user);
        void AddSubscription(Subscription subscription);
        Subscription GetByUserAndId(string user, string directoryId);

        void Remove(Subscription subscription);
    }

    public class SubscriptionTableRepository : ISubscriptionRepository
    {
        private readonly string tableConnectionString;
        private readonly CloudTable cloudTable;
        public SubscriptionTableRepository(IOptions<AzureADSettings> adSettings)
        {
            tableConnectionString = adSettings.Value.TokenStorageConnectionString;

            var cloudAccount = CloudStorageAccount.Parse(tableConnectionString);
            cloudTable = cloudAccount.CreateCloudTableClient().GetTableReference("azuresubscriptions");
            cloudTable.CreateIfNotExists();
        }

        public IEnumerable<Subscription> GetAllSubscriptionsForUser(string user)
        {
            TableQuery<SubscriptionTableEntity> query = new TableQuery<SubscriptionTableEntity>()
                .Where(TableQuery.GenerateFilterCondition("PartitionKey",QueryComparisons.Equal, user));

            return cloudTable.ExecuteQuery(query).Select(a => a.ToModel()).ToList();
        }

        public Subscription GetByUserAndId(string user, string subscriptionId)
        {
            TableQuery<SubscriptionTableEntity> query = new TableQuery<SubscriptionTableEntity>()
                .Where(TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, user),
                    TableOperators.And,
                    TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, subscriptionId)));

            return cloudTable.ExecuteQuery(query).Select(a=> a.ToModel()).FirstOrDefault();
        }

        public void AddSubscription(Subscription subscription)
        {
            var tableOp = TableOperation.InsertOrReplace(new SubscriptionTableEntity(
                subscription.Id,
                subscription.DirectoryId, subscription.ConnectedBy, subscription.ConnectedOn));
            cloudTable.Execute(tableOp);
        }

        public void Remove(Subscription subscription)
        {
            TableQuery<SubscriptionTableEntity> query = new TableQuery<SubscriptionTableEntity>()
                .Where(TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, subscription.ConnectedBy),
                    TableOperators.And,
                    TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, subscription.Id)));
            var entity = cloudTable.ExecuteQuery(query).FirstOrDefault();
            if(entity != null)
            {
                var delOp = TableOperation.Delete(entity);
                cloudTable.Execute(delOp);
            }

        }
    }
}
