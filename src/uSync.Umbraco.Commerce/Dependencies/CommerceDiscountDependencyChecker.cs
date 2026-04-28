using System.Collections.Generic;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Commerce.Core.Models;
using Umbraco.Extensions;
using uSync.Core.Dependency;

namespace uSync.Umbraco.Commerce.Dependencies;

public class CommerceDiscountDependencyChecker : ISyncDependencyChecker<DiscountReadOnly>
{
    public UmbracoObjectTypes ObjectType => UmbracoObjectTypes.Unknown;

    public IEnumerable<uSyncDependency> GetDependencies(DiscountReadOnly item, DependencyFlags flags)
    {
        return new uSyncDependency  
        {
            Name = item.Name,
            Order = CommerceConstants.Priorites.Discount,
            Udi = Udi.Create(CommerceConstants.UdiEntityType.Discount, item.Id)
        }.AsEnumerableOfOne();
    }
}
