using System.Collections.Generic;
using System.Threading.Tasks;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Commerce.Core.Models;
using Umbraco.Extensions;
using uSync.Core.Dependency;

namespace uSync.Umbraco.Commerce.Dependencies
{
    public class CommerceOrderStatusDependecyChecker : ISyncDependencyChecker<OrderStatusReadOnly>
    {
        public UmbracoObjectTypes ObjectType => UmbracoObjectTypes.Unknown;

        public Task<IEnumerable<uSyncDependency>> GetDependenciesAsync(
            OrderStatusReadOnly item,
            DependencyFlags flags
        )
        {
            var dependency = new uSyncDependency
            {
                Name = item.Name,
                Order = CommerceConstants.Priorites.OrderStatus,
                Udi = Udi.Create(CommerceConstants.UdiEntityType.OrderStatus, item.Id),
            };

            return Task.FromResult<IEnumerable<uSyncDependency>>([dependency]);
        }
    }
}
