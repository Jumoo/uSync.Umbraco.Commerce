using System.Collections.Generic;
using System.Threading.Tasks;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Commerce.Core.Api;
using Umbraco.Commerce.Core.Models;
using Umbraco.Extensions;

using uSync.Core.Dependency;

namespace uSync.Umbraco.Commerce.Dependencies;

public class CommerceGiftCardDependencyChecker : ISyncDependencyChecker<GiftCardReadOnly>
{
    public UmbracoObjectTypes ObjectType => UmbracoObjectTypes.Unknown;

    public Task<IEnumerable<uSyncDependency>> GetDependenciesAsync(GiftCardReadOnly item, DependencyFlags flags)
    {
        // the gift card itself
        var dependency = new uSyncDependency
        {
            Name = item.Code,
            Order = CommerceConstants.Priorites.GiftCard,
            Udi = Udi.Create(CommerceConstants.UdiEntityType.GiftCard, item.Id)
        };

        return Task.FromResult<IEnumerable<uSyncDependency>>([dependency]);
    }
}
