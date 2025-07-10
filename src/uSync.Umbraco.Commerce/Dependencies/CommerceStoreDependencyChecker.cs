using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Commerce.Core.Api;
using Umbraco.Commerce.Core.Models;
using uSync.Core.Dependency;

namespace uSync.Umbraco.Commerce.Dependencies
{
    public class CommerceStoreDependencyChecker : ISyncDependencyChecker<StoreReadOnly>
    {
        private readonly ICommerceApi _CommerceApi;

        public CommerceStoreDependencyChecker(ICommerceApi CommerceApi)
        {
            _CommerceApi = CommerceApi;
        }

        public UmbracoObjectTypes ObjectType => UmbracoObjectTypes.Unknown;

        public async Task<IEnumerable<uSyncDependency>> GetDependenciesAsync(
            StoreReadOnly item,
            DependencyFlags flags
        )
        {
            if (item == null)
                return Enumerable.Empty<uSyncDependency>();

            var items = new List<uSyncDependency>();

            items.Add(
                new uSyncDependency
                {
                    Name = item.Name,
                    Order = CommerceConstants.Priorites.Stores,
                    Udi = Udi.Create(CommerceConstants.UdiEntityType.Store, item.Id),
                    Flags = DependencyFlags.None,
                }
            );

            var orderStatuses = GetOrderStatusesAsync(item.Id);
            var currencies = GetCurrenciesAsync(item.Id);
            var shippingMethods = GetShippingMethodsAsync(item.Id);
            var countries = GetCountriesAsync(item.Id);
            var regions = GetRegionsAsync(item.Id);
            var paymentMethods = GetPaymentMethodsAsync(item.Id);
            var taxClasses = GetTaxClassesAsync(item.Id);
            var emailTemplates = GetEmailTemplatesAsync(item.Id);
            var exportTemplates = GetExportTemplatesAsync(item.Id);
            var printTemplates = GetPrintTemplatesAsync(item.Id);

            await Task.WhenAll(
                orderStatuses,
                currencies,
                shippingMethods,
                countries,
                regions,
                paymentMethods,
                taxClasses,
                emailTemplates,
                exportTemplates,
                printTemplates
            );

            items.AddRange(orderStatuses.Result);
            items.AddRange(currencies.Result);
            items.AddRange(shippingMethods.Result);
            items.AddRange(countries.Result);
            items.AddRange(regions.Result);
            items.AddRange(paymentMethods.Result);
            items.AddRange(taxClasses.Result);
            items.AddRange(emailTemplates.Result);
            items.AddRange(exportTemplates.Result);
            items.AddRange(printTemplates.Result);

            return items;
        }

        public async Task<IEnumerable<uSyncDependency>> GetOrderStatusesAsync(Guid storeId) =>
            (await _CommerceApi.GetOrderStatusesAsync(storeId)).Select(x => new uSyncDependency
            {
                Name = x.Name,
                Order = CommerceConstants.Priorites.OrderStatus,
                Udi = Udi.Create(CommerceConstants.UdiEntityType.OrderStatus, x.Id),
            });

        private async Task<IEnumerable<uSyncDependency>> GetCurrenciesAsync(Guid storeId) =>
            (await _CommerceApi.GetCurrenciesAsync(storeId)).Select(x => new uSyncDependency
            {
                Name = x.Name,
                Order = CommerceConstants.Priorites.Currency,
                Udi = Udi.Create(CommerceConstants.UdiEntityType.Currency, x.Id),
            });

        private async Task<IEnumerable<uSyncDependency>> GetShippingMethodsAsync(Guid storeId) =>
            (await _CommerceApi.GetShippingMethodsAsync(storeId)).Select(x => new uSyncDependency
            {
                Name = x.Name,
                Order = CommerceConstants.Priorites.ShippingMethod,
                Udi = Udi.Create(CommerceConstants.UdiEntityType.ShippingMethod),
            });

        private async Task<IEnumerable<uSyncDependency>> GetCountriesAsync(Guid storeId) =>
            (await _CommerceApi.GetCountriesAsync(storeId)).Select(x => new uSyncDependency
            {
                Name = x.Name,
                Order = CommerceConstants.Priorites.Country,
                Udi = Udi.Create(CommerceConstants.UdiEntityType.Country, x.Id),
            });

        private async Task<IEnumerable<uSyncDependency>> GetRegionsAsync(Guid storeId) =>
            (await _CommerceApi.GetRegionsAsync(storeId)).Select(x => new uSyncDependency
            {
                Name = x.Name,
                Order = CommerceConstants.Priorites.Region,
                Udi = Udi.Create(CommerceConstants.UdiEntityType.Region, x.Id),
            });

        private async Task<IEnumerable<uSyncDependency>> GetPaymentMethodsAsync(Guid storeId) =>
            (await _CommerceApi.GetPaymentMethodsAsync(storeId)).Select(x => new uSyncDependency
            {
                Name = x.Name,
                Order = CommerceConstants.Priorites.PaymentMethod,
                Udi = Udi.Create(CommerceConstants.UdiEntityType.PaymentMethod, x.Id),
            });

        private async Task<IEnumerable<uSyncDependency>> GetTaxClassesAsync(Guid storeId) =>
            (await _CommerceApi.GetTaxClassesAsync(storeId)).Select(x => new uSyncDependency
            {
                Name = x.Name,
                Order = CommerceConstants.Priorites.TaxClass,
                Udi = Udi.Create(CommerceConstants.UdiEntityType.TaxClass, x.Id),
            });

        private async Task<IEnumerable<uSyncDependency>> GetEmailTemplatesAsync(Guid storeId) =>
            (await _CommerceApi.GetEmailTemplatesAsync(storeId)).Select(x => new uSyncDependency
            {
                Name = x.Name,
                Order = CommerceConstants.Priorites.EmailTemplate,
                Udi = Udi.Create(CommerceConstants.UdiEntityType.EmailTemplate, x.Id),
            });

        private async Task<IEnumerable<uSyncDependency>> GetExportTemplatesAsync(Guid storeId) =>
            (await _CommerceApi.GetExportTemplatesAsync(storeId)).Select(x => new uSyncDependency
            {
                Name = x.Name,
                Order = CommerceConstants.Priorites.EmailTemplate,
                Udi = Udi.Create(CommerceConstants.UdiEntityType.EmailTemplate, x.Id),
            });

        private async Task<IEnumerable<uSyncDependency>> GetPrintTemplatesAsync(Guid storeId) =>
            (await _CommerceApi.GetPrintTemplatesAsync(storeId)).Select(x => new uSyncDependency
            {
                Name = x.Name,
                Order = CommerceConstants.Priorites.PrintTemplate,
                Udi = Udi.Create(CommerceConstants.UdiEntityType.PrintTemplate, x.Id),
            });
    }
}
