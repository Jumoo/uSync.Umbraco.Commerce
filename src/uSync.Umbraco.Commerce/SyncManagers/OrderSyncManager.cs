using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Umbraco.Cms.Core;
using Umbraco.Commerce.Core.Api;
using Umbraco.Commerce.Core.Models;
using Umbraco.Extensions;

using uSync.Core.Dependency;
using uSync.Core.Sync;

using static Umbraco.Commerce.Cms.Constants.Trees.Settings;

namespace uSync.Umbraco.Commerce.SyncManagers;

[SyncItemManager(CommerceConstants.UdiEntityType.Store)]
public class OrderSyncManager : CommerceManagerBase, ISyncItemManager
{
    private readonly Dictionary<NodeType, string> _nodeToEntityMapping = new()
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
        { NodeType.PrintTemplates, CommerceConstants.UdiEntityType.PrintTemplate },
    };

    // the UDI types we hand out, plus the backoffice entity types the push/pull menus send
    // (uSync.Complete picks the item manager by the entity type it is given).
    public string[] EntityTypes =>
    [
        .. _nodeToEntityMapping.Values,
        CommerceConstants.UdiEntityType.Region,
        CommerceConstants.UdiEntityType.Location,
        CommerceConstants.BackofficeEntityType.Store,
        CommerceConstants.BackofficeEntityType.OrderStatus,
        CommerceConstants.BackofficeEntityType.ShippingMethod,
        CommerceConstants.BackofficeEntityType.Country,
        CommerceConstants.BackofficeEntityType.Region,
        CommerceConstants.BackofficeEntityType.Currency,
        CommerceConstants.BackofficeEntityType.PaymentMethod,
        CommerceConstants.BackofficeEntityType.TaxClass,
        CommerceConstants.BackofficeEntityType.EmailTemplate,
        CommerceConstants.BackofficeEntityType.ExportTemplate,
        CommerceConstants.BackofficeEntityType.PrintTemplate,
        CommerceConstants.BackofficeEntityType.Location,
    ];

    public string[] Trees => [Alias];

    public OrderSyncManager(ICommerceApi CommerceApi)
        : base(CommerceApi)
    { }

    protected override string LookupNodeEntityType(int id)
    {
        var CommerceNodeType = Ids.FirstOrDefault(x => x.Value == id).Key ;
        if (_nodeToEntityMapping.TryGetValue(CommerceNodeType, out string value))
            return value;
        return null;
    }


    protected override List<Func<Guid, Task<SyncEntity>>> SyncEntityFetchers =>
    [
        async key => (await _CommerceApi.GetStoreAsync(key)) is StoreReadOnly store
            ? new SyncEntity()
            {
                Name = store.Name,
                Icon = "icon-store",
                Udi = Udi.Create(CommerceConstants.UdiEntityType.Store, store.Id),
            }
            : null,
        async key => (await _CommerceApi.GetOrderStatusAsync(key)) is OrderStatusReadOnly orderStatus
            ? new SyncEntity()
            {
                Name = orderStatus.Name,
                Icon = "icon-file-cabinet",
                Udi = Udi.Create(CommerceConstants.UdiEntityType.OrderStatus, orderStatus.Id),
            }
            : null,
        async key => (await _CommerceApi.GetShippingMethodAsync(key)) is ShippingMethodReadOnly shippingMethod
            ? new SyncEntity()
            {
                Name = shippingMethod.Name,
                Icon = "icon-truck",
                Udi = Udi.Create(
                    CommerceConstants.UdiEntityType.ShippingMethod,
                    shippingMethod.Id
                ),
            }
            : null,
        async key => (await _CommerceApi.GetCountryAsync(key)) is CountryReadOnly country
            ? new SyncEntity()
            {
                Name = country.Name,
                Icon = "icon-globe",
                Udi = Udi.Create(CommerceConstants.UdiEntityType.Country, country.Id),
            }
            : null,
        async key => (await _CommerceApi.GetRegionAsync(key)) is RegionReadOnly region
            ? new SyncEntity()
            {
                Name = region.Name,
                Icon = "icon-map-location",
                Udi = Udi.Create(CommerceConstants.UdiEntityType.Region, region.Id),
            }
            : null,
        async key => (await _CommerceApi.GetLocationAsync(key)) is LocationReadOnly location
            ? new SyncEntity()
            {
                Name = location.Name,
                Icon = "icon-map-marker",
                Udi = Udi.Create(CommerceConstants.UdiEntityType.Location, location.Id),
            }
            : null,
        async key => (await _CommerceApi.GetCurrencyAsync(key)) is CurrencyReadOnly currency
            ? new SyncEntity()
            {
                Name = currency.Name,
                Icon = "icon-coins-dollar-alt",
                Udi = Udi.Create(CommerceConstants.UdiEntityType.Currency, currency.Id),
            }
            : null,
        async key => (await _CommerceApi.GetPaymentMethodAsync(key)) is PaymentMethodReadOnly paymentMethod
            ? new SyncEntity()
            {
                Name = paymentMethod.Name,
                Icon = "icon-multiple-credit-cards",
                Udi = Udi.Create(
                    CommerceConstants.UdiEntityType.PaymentMethod,
                    paymentMethod.Id
                ),
            }
            : null,
        async key => (await _CommerceApi.GetTaxClassAsync(key)) is TaxClassReadOnly taxClass
            ? new SyncEntity()
            {
                Name = taxClass.Name,
                Icon = "icon-library",
                Udi = Udi.Create(CommerceConstants.UdiEntityType.TaxClass, taxClass.Id),
            }
            : null,
        async key => (await _CommerceApi.GetEmailTemplateAsync(key)) is EmailTemplateReadOnly emailTemplate
            ? new SyncEntity()
            {
                Name = emailTemplate.Name,
                Icon = "icon-mailbox",
                Udi = Udi.Create(
                    CommerceConstants.UdiEntityType.EmailTemplate,
                    emailTemplate.Id
                ),
            }
            : null,
        async key => (await _CommerceApi.GetExportTemplateAsync(key)) is ExportTemplateReadOnly exportTemplate
            ? new SyncEntity()
            {
                Name = exportTemplate.Name,
                Icon = "icon-sharing-iphone",
                Udi = Udi.Create(
                    CommerceConstants.UdiEntityType.ExportTemplate,
                    exportTemplate.Id
                ),
            }
            : null,
        async key => (await _CommerceApi.GetPrintTemplateAsync(key)) is PrintTemplateReadOnly printTemplate
            ? new SyncEntity()
            {
                Name = printTemplate.Name,
                Icon = "icon-print",
                Udi = Udi.Create(
                    CommerceConstants.UdiEntityType.PrintTemplate,
                    printTemplate.Id
                ),
            }
            : null,
    ];

    protected override Dictionary<string, Func<Guid, DependencyFlags, Task<IEnumerable<SyncItem>>>> SyncItemFetchers => new()
    {
        { CommerceConstants.UdiEntityType.OrderStatus, 
            async (storeId, flags) => {
                return await _CommerceApi.GetOrderStatusesAsync(storeId)
                    .ContinueWith(task => task.Result.Select(x => new SyncItem()
                    {
                        Name = x.Name,
                        Udi = Udi.Create(CommerceConstants.UdiEntityType.OrderStatus, x.Id),
                        Flags = flags
                    }));
            }
        }
    };
}

