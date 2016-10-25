using System.Collections.Generic;

namespace AzureResourceManager.Models
{
    public class ViewModel
    {
        public ICollection<Subscription> ConnectedSubscriptions { get; set; }
    }
}
