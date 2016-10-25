using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AzureResourceManager.Models
{
    public class ViewModel
    {
        public ICollection<Subscription> ConnectedSubscriptions { get; set; }
    }
}
