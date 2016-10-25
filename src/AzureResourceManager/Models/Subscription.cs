using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace AzureResourceManager.Models
{
    public class Subscription
    {
        public string Id { get; set; }
        public string DirectoryId { get; set; }
        public DateTime ConnectedOn { get; set; }
        public string ConnectedBy { get; set; }
        [NotMapped]
        public bool AzureAccessNeedsToBeRepaired { get; set; }
    }
}
