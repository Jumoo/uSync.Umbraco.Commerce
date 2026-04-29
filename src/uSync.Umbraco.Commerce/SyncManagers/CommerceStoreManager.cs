using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Umbraco.Cms.Core;
using Umbraco.Commerce.Core.Api;
using Umbraco.Commerce.Core.Models;

using uSync.Core.Dependency;
using uSync.Core.Sync;

using static Umbraco.Commerce.Cms.Constants.Trees.Stores;

namespace uSync.Umbraco.Commerce.SyncManagers;

public class CommerceStoreManager : CommerceManagerBase, ISyncItemManager
{
    public CommerceStoreManager(ICommerceApi commerceApi)
        : base(commerceApi)
    { }

    public string[] Trees => [Alias];
    public string[] EntityTypes => [.. _nodeToEntityMapping.Values];

    private Dictionary<NodeType, string> _nodeToEntityMapping => new()
    {
        { NodeType.Discounts, CommerceConstants.UdiEntityType.Discount },
        { NodeType.GiftCards, CommerceConstants.UdiEntityType.GiftCard }
    };

    protected override string LookupNodeEntityType(int id)
    {
        var CommerceNodeType = Ids.FirstOrDefault(x => x.Value == id).Key;
        if (_nodeToEntityMapping.TryGetValue(CommerceNodeType, out string value))
            return value;
        return null;
    }

    protected override List<Func<Guid, Task<SyncEntity>>> SyncEntityFetchers => new()
    {
        async key => (await _CommerceApi.GetDiscountsAsync(key)) is DiscountReadOnly discount
             ? new SyncEntity()
             {
                 Name = discount.Name,
                 Icon = "icon-discount",
                 Udi = Udi.Create(CommerceConstants.UdiEntityType.Discount, discount.Id),
             }
             : null,
        async key => (await _CommerceApi.GetGiftCardsAsync(key)) is GiftCardReadOnly giftCard
            ? new SyncEntity()
            {
                Name = giftCard.Code,
                Icon = "icon-gift",
                Udi = Udi.Create(CommerceConstants.UdiEntityType.GiftCard, giftCard.Id),
            }
            : null
    };

    protected override Dictionary<string, Func<Guid, DependencyFlags, Task<IEnumerable<SyncItem>>>> SyncItemFetchers => new()
    {
        {
            CommerceConstants.UdiEntityType.Discount, async (storeId, flags) =>
            {
                var discounts = await _CommerceApi.GetDiscountsAsync(storeId);
                return discounts.Select(x => new SyncItem()
                {
                    Name = x.Name,
                    Icon = "icon-discount",
                    Udi = Udi.Create(CommerceConstants.UdiEntityType.Discount, x.Id),
                    Flags = flags
                });
            }
        },
        {
            CommerceConstants.UdiEntityType.GiftCard, async (storeId, flags) =>
            {
                var giftCards = await _CommerceApi.GetGiftCardsAsync(storeId);
                return giftCards.Select(x => new SyncItem()
                {
                    Name = x.Code,
                    Icon = "icon-gift",
                    Udi = Udi.Create(CommerceConstants.UdiEntityType.GiftCard, x.Id),
                    Flags = flags
                });
            }
        }
    };

}
