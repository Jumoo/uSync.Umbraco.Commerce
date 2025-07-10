using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Umbraco.Cms.Core;
using Umbraco.Commerce.Core.Api;
using Umbraco.Commerce.Core.Models;
using Umbraco.Extensions;
using uSync.Core.Sync;
using static Umbraco.Commerce.Cms.Constants.Trees.Settings;

namespace uSync.Umbraco.Commerce.SyncManagers
{
    [SyncItemManager(CommerceConstants.UdiEntityType.Store)]
    public class OrderSyncManager : ISyncItemManager
    {
        private readonly Dictionary<NodeType, string> _nodeToEntityMapping = new Dictionary<
            NodeType,
            string
        >
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

        public string[] EntityTypes => _nodeToEntityMapping.Values.ToArray();

        public string[] Trees => new string[] { Alias };

        private readonly ICommerceApi _CommerceApi;

        public OrderSyncManager(ICommerceApi CommerceApi)
        {
            _CommerceApi = CommerceApi;
        }

        public async Task<IEnumerable<SyncItem>> GetItemsAsync(SyncItem item)
        {
            // for the store just return ths store item,
            // the depdency checker will do the rest.
            if (item.Udi.EntityType == CommerceConstants.UdiEntityType.Store)
                return item.AsEnumerableOfOne();

            // for other items the ID might be the store ID
            // which acts as a root Udi for that type in the store.
            if (item.Udi is GuidUdi guidUdi)
            {
                var store = await _CommerceApi.GetStoreAsync(guidUdi.Guid);
                if (store == null)
                    return item.AsEnumerableOfOne();

                // if it was the store, get all the items of that type

                // there might be a more generic way of doing this ?
                switch (item.Udi.EntityType)
                {
                    case CommerceConstants.UdiEntityType.OrderStatus:
                        return (await _CommerceApi.GetOrderStatusesAsync(store.Id)).Select(
                            x => new SyncItem
                            {
                                Name = x.Name,
                                Udi = Udi.Create(CommerceConstants.UdiEntityType.OrderStatus, x.Id),
                                Flags = item.Flags,
                            }
                        );
                }
            }
            return item.AsEnumerableOfOne();
        }

        /// <summary>
        ///  uSync Exporter - supply the info for it to open the picker.
        /// </summary>
        public SyncEntityInfo GetSyncInfo(string entityType)
        {
            var x = entityType;
            return null;
        }

        /// <summary>
        ///  tells usync what type of sync this is (settings, content, files)
        /// </summary>
        public SyncTreeType GetTreeType(SyncTreeItem treeItem)
        {
            var entityType = GetEntityTypeFromTree(treeItem);
            if (entityType != null)
                return SyncTreeType.Settings;

            return SyncTreeType.None;
        }

        private Guid? GetStoreGuid(string id)
        {
            if (Guid.TryParse(id, out Guid storeGuid))
                return storeGuid;

            return null;
        }

        private string GetEntityTypeFromTree(SyncTreeItem item)
        {
            if (GetStoreGuid(item.Id) != null)
                return CommerceConstants.UdiEntityType.Store;

            var storeId = item.QueryStrings?["storeId"];
            if (string.IsNullOrWhiteSpace(storeId))
                return string.Empty;

            var attempt = item.Id.TryConvertTo<int>();
            if (!attempt.Success)
                return string.Empty;

            var CommerceNodeType = Ids.FirstOrDefault(x => x.Value == attempt.Result).Key;

            if (_nodeToEntityMapping.ContainsKey(CommerceNodeType))
                return _nodeToEntityMapping[CommerceNodeType];

            return string.Empty;
        }

        public async Task<SyncEntity> GetSyncEntityAsync(string key)
        {
            if (Guid.TryParse(key, out var guidKey) is false)
                return null;

            if (await _CommerceApi.GetStoreAsync(guidKey) is StoreReadOnly store)
                return new SyncEntity()
                {
                    Name = store.Name,
                    Icon = "icon-store",
                    Udi = Udi.Create(CommerceConstants.UdiEntityType.Store, store.Id),
                };

            if (await _CommerceApi.GetOrderStatusAsync(guidKey) is OrderStatusReadOnly orderStatus)
                return new SyncEntity()
                {
                    Name = orderStatus.Name,
                    Icon = "icon-file-cabinet",
                    Udi = Udi.Create(CommerceConstants.UdiEntityType.OrderStatus, orderStatus.Id),
                };

            if (
                await _CommerceApi.GetShippingMethodAsync(guidKey)
                is ShippingMethodReadOnly shippingMethod
            )
                return new SyncEntity()
                {
                    Name = shippingMethod.Name,
                    Icon = "icon-truck",
                    Udi = Udi.Create(
                        CommerceConstants.UdiEntityType.ShippingMethod,
                        shippingMethod.Id
                    ),
                };

            if (await _CommerceApi.GetCountryAsync(guidKey) is CountryReadOnly country)
                return new SyncEntity()
                {
                    Name = country.Name,
                    Icon = "icon-truck",
                    Udi = Udi.Create(CommerceConstants.UdiEntityType.Country, country.Id),
                };

            if (await _CommerceApi.GetCurrencyAsync(guidKey) is CurrencyReadOnly currency)
                return new SyncEntity()
                {
                    Name = currency.Name,
                    Icon = "icon-coins-dollar-alt",
                    Udi = Udi.Create(CommerceConstants.UdiEntityType.Currency, currency.Id),
                };

            if (
                await _CommerceApi.GetPaymentMethodAsync(guidKey)
                is PaymentMethodReadOnly paymentMethod
            )
                return new SyncEntity()
                {
                    Name = paymentMethod.Name,
                    Icon = "icon-multiple-credit-cards",
                    Udi = Udi.Create(
                        CommerceConstants.UdiEntityType.PaymentMethod,
                        paymentMethod.Id
                    ),
                };

            if (await _CommerceApi.GetTaxClassAsync(guidKey) is TaxClassReadOnly taxClass)
                return new SyncEntity()
                {
                    Name = taxClass.Name,
                    Icon = "icon-library",
                    Udi = Udi.Create(CommerceConstants.UdiEntityType.TaxClass, taxClass.Id),
                };

            if (
                await _CommerceApi.GetEmailTemplateAsync(guidKey)
                is EmailTemplateReadOnly emailTemplate
            )
                return new SyncEntity()
                {
                    Name = emailTemplate.Name,
                    Icon = "icon-mailbox",
                    Udi = Udi.Create(
                        CommerceConstants.UdiEntityType.EmailTemplate,
                        emailTemplate.Id
                    ),
                };

            if (
                await _CommerceApi.GetExportTemplateAsync(guidKey)
                is ExportTemplateReadOnly exportTemplate
            )
                return new SyncEntity()
                {
                    Name = exportTemplate.Name,
                    Icon = "icon-sharing-iphone",
                    Udi = Udi.Create(
                        CommerceConstants.UdiEntityType.ExportTemplate,
                        exportTemplate.Id
                    ),
                };

            if (
                await _CommerceApi.GetPrintTemplateAsync(guidKey)
                is PrintTemplateReadOnly printTemplate
            )
                return new SyncEntity()
                {
                    Name = printTemplate.Name,
                    Icon = "icon-truck",
                    Udi = Udi.Create(
                        CommerceConstants.UdiEntityType.PrintTemplate,
                        printTemplate.Id
                    ),
                };

            return null;
        }
    }
}
