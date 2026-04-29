using System.Collections.Generic;
using System.Threading.Tasks;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Commerce.Core.Models;
using Umbraco.Extensions;
using uSync.Core.Dependency;

namespace uSync.Umbraco.Commerce.Dependencies;

public class CommerceDiscountDependencyChecker : ISyncDependencyChecker<DiscountReadOnly>
{
    public UmbracoObjectTypes ObjectType => UmbracoObjectTypes.Unknown;

    public Task<IEnumerable<uSyncDependency>> GetDependenciesAsync(DiscountReadOnly item, DependencyFlags flags)
    {
        var dependency = new uSyncDependency  
        {
            Name = item.Name,
            Order = CommerceConstants.Priorites.Discount,
            Udi = Udi.Create(CommerceConstants.UdiEntityType.Discount, item.Id)
        };

        return Task.FromResult<IEnumerable<uSyncDependency>>([dependency]);
    }

}
