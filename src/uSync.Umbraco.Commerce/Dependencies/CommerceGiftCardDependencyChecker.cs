using System.Collections.Generic;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Commerce.Core.Api;
using Umbraco.Commerce.Core.Models;
using Umbraco.Extensions;

using uSync.Core.Dependency;

namespace uSync.Umbraco.Commerce.Dependencies;

public class CommerceGiftCardDependencyChecker : ISyncDependencyChecker<GiftCardReadOnly>
{
    private readonly ICommerceApi _commerceApi;

    public CommerceGiftCardDependencyChecker(ICommerceApi commerceApi)
    {
        _commerceApi = commerceApi;
    }

    public UmbracoObjectTypes ObjectType => UmbracoObjectTypes.Unknown;

    public IEnumerable<uSyncDependency> GetDependencies(GiftCardReadOnly item, DependencyFlags flags)
    {
        var items = new List<uSyncDependency>();

        // the gift card itself
        items.Add(new uSyncDependency
        {
            Name = item.Code,
            Order = CommerceConstants.Priorites.GiftCard,
            Udi = Udi.Create(CommerceConstants.UdiEntityType.GiftCard, item.Id)
        });

        return items;
    }
}
