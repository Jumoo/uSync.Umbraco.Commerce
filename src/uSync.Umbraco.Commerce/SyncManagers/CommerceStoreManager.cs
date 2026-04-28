using System;
using System.Collections.Generic;
using System.Linq;

using Umbraco.Cms.Core;
using Umbraco.Commerce.Core.Api;
using Umbraco.Commerce.Core.Models;
using Umbraco.Commerce.Core.Services;
using Umbraco.Extensions;

using uSync.Core.Dependency;
using uSync.Core.Sync;

using static Umbraco.Commerce.Cms.Constants.Trees.Stores;

namespace uSync.Umbraco.Commerce.SyncManagers;

public class CommerceStoreManager : CommerceManagerBase, ISyncItemManager
{
    private readonly IGiftCardService _giftCardService;

    public CommerceStoreManager(ICommerceApi commerceApi, IGiftCardService giftCardService) : base(commerceApi)
    {
        _giftCardService = giftCardService;
    }

    public string[] Trees => [Alias];
    public string[] EntityTypes => [.. _nodeToEntityMapping.Values];

    private Dictionary<NodeType, string> _nodeToEntityMapping => new()
    {
        { NodeType.Discounts, CommerceConstants.UdiEntityType.Discount },
        { NodeType.GiftCards, CommerceConstants.UdiEntityType.GiftCard }
    };

    protected override string GetEntityTypeFromTree(SyncTreeItem item)
    {
        if (GetStoreGuid(item.Id) != null) return CommerceConstants.UdiEntityType.Store;

        var storeId = item.QueryStrings?["storeId"];
        if (string.IsNullOrWhiteSpace(storeId)) return string.Empty;

        var attempt = item.Id.TryConvertTo<int>();
        if (!attempt.Success) return string.Empty;

        var CommerceNodeType = Ids.FirstOrDefault(x => x.Value == attempt.Result).Key;

        if (_nodeToEntityMapping.TryGetValue(CommerceNodeType, out string value))
            return value;

        return string.Empty;
    }

    protected override Dictionary<string, Func<Guid, DependencyFlags, IEnumerable<SyncItem>>> ItemFetchers => new()
    {
        { CommerceConstants.UdiEntityType.Discount, GetDiscounts },
        { CommerceConstants.UdiEntityType.GiftCard, GetGiftCards }
    };

    private IEnumerable<SyncItem> GetDiscounts(Guid storeId, DependencyFlags flags)
    {
        return _CommerceApi.GetDiscounts(storeId)
            .Select(x => new SyncItem
            {
                Name = x.Name,
                Udi = Udi.Create(CommerceConstants.UdiEntityType.Discount, x.Id),
                Flags = flags,
            });
    }

    private IEnumerable<SyncItem> GetGiftCards(Guid storeId, DependencyFlags flags)
    {
        return _giftCardService.GetGiftCards(storeId)
            .Select(x => new SyncItem
            {
                Name = x.Code,
                Udi = Udi.Create(CommerceConstants.UdiEntityType.GiftCard, x.Id),
                Flags = flags,
            });
    }
}
