using System;
using System.Collections.Generic;
using System.Linq;

using Umbraco.Cms.Core;
using Umbraco.Commerce.Core.Api;
using Umbraco.Extensions;

using uSync.Core.Dependency;
using uSync.Core.Sync;

using static Umbraco.Commerce.Cms.Constants.Trees.Settings;

namespace uSync.Umbraco.Commerce.SyncManagers;
public class OrderSyncManager : CommerceManagerBase, ISyncItemManager
{
    public OrderSyncManager(ICommerceApi CommerceApi) : base(CommerceApi)
    { }


    private Dictionary<NodeType, string> _nodeToEntityMapping => new()
    {
        { NodeType.Store, CommerceConstants.UdiEntityType.Store },
        { NodeType.OrderStatuses, CommerceConstants.UdiEntityType.OrderStatus },
        { NodeType.ShippingMethods, CommerceConstants.UdiEntityType.ShippingMethod },
        { NodeType.Countries, CommerceConstants.UdiEntityType.Country },
        { NodeType.Currencies, CommerceConstants.UdiEntityType.Currency },
        { NodeType.PaymentMethods, CommerceConstants.UdiEntityType.PaymentMethod },
        { NodeType.TaxClasses, CommerceConstants.UdiEntityType.TaxClass },
        { NodeType.EmailTemplates, CommerceConstants.UdiEntityType.EmailTemplate },
        { NodeType.ExportTemplates, CommerceConstants.UdiEntityType.ExportTemplate },
        { NodeType.PrintTemplates, CommerceConstants.UdiEntityType.PrintTemplate }
    };

    public string[] Trees => [Alias];
    public string[] EntityTypes => [.. _nodeToEntityMapping.Values];

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
        {  CommerceConstants.UdiEntityType.OrderStatus, GetOrderStatuses  }
    };

    private IEnumerable<SyncItem> GetOrderStatuses(Guid storeId, DependencyFlags flags)
    {
        return _CommerceApi.GetOrderStatuses(storeId)
            .Select(x => new SyncItem
            {
                Name = x.Name,
                Udi = Udi.Create(CommerceConstants.UdiEntityType.OrderStatus, x.Id),
                Flags = flags,
            });
    
}
}