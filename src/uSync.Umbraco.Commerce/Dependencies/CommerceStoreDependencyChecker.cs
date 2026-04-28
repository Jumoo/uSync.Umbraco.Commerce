using Dazinator.Extensions.FileProviders.InMemory.Directory;
using System;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Commerce.Core.Api;
using Umbraco.Commerce.Core.Models;
using Umbraco.Commerce.Core.Services;
using uSync.Core.Dependency;

namespace uSync.Umbraco.Commerce.Dependencies
{
    public class CommerceStoreDependencyChecker : ISyncDependencyChecker<StoreReadOnly>
    {
        private readonly ICommerceApi _CommerceApi;
        private readonly IGiftCardService _giftCardService;

        public CommerceStoreDependencyChecker(ICommerceApi CommerceApi, IGiftCardService giftCardService)
        {
            _CommerceApi = CommerceApi;
            _giftCardService = giftCardService;
        }

        public UmbracoObjectTypes ObjectType => UmbracoObjectTypes.Unknown;

        public IEnumerable<uSyncDependency> GetDependencies(StoreReadOnly item, DependencyFlags flags)
        {
            if (item == null) return Enumerable.Empty<uSyncDependency>();

            var items = new List<uSyncDependency>();


            items.Add(new uSyncDependency
            {
                Name = item.Name,
                Order = CommerceConstants.Priorites.Stores,
                Udi = Udi.Create(CommerceConstants.UdiEntityType.Store, item.Id),
                Flags = DependencyFlags.None
            });

            // a store has a lot of dependencies,
            items.AddRange(GetLocations(item.Id));
            items.AddRange(GetOrderStatuses(item.Id));
            items.AddRange(GetShippingMethods(item.Id));
            items.AddRange(GetPaymentMethods(item.Id));
            items.AddRange(GetCountries(item.Id));
            items.AddRange(GetRegions(item.Id));
            items.AddRange(GetCurrencies(item.Id));
            items.AddRange(GetTaxClasses(item.Id));
            items.AddRange(GetEmailTemplates(item.Id));
            items.AddRange(GetExportTemplates(item.Id));
            items.AddRange(GetPrintTemplates(item.Id));
            // items.AddRange(GetGiftCards(item.Id));

            return items;
        }

        public IEnumerable<uSyncDependency> GetOrderStatuses(Guid storeId)
            => _CommerceApi.GetOrderStatuses(storeId)
                    .Select(x => new uSyncDependency
                    {
                        Name = x.Name,
                        Order = CommerceConstants.Priorites.OrderStatus,
                        Udi = Udi.Create(CommerceConstants.UdiEntityType.OrderStatus, x.Id)
                    });

        private IEnumerable<uSyncDependency> GetCurrencies(Guid storeId)
            => _CommerceApi.GetCurrencies(storeId)
                    .Select(x => new uSyncDependency
                    {
                        Name = x.Name,
                        Order = CommerceConstants.Priorites.Currency,
                        Udi = Udi.Create(CommerceConstants.UdiEntityType.Currency, x.Id)
                    });

        private IEnumerable<uSyncDependency> GetShippingMethods(Guid storeId)
            => _CommerceApi.GetShippingMethods(storeId)
                .Select(x => new uSyncDependency
                {
                    Name = x.Name,
                    Order = CommerceConstants.Priorites.ShippingMethod,
                    Udi = Udi.Create(CommerceConstants.UdiEntityType.ShippingMethod)
                });

        private IEnumerable<uSyncDependency> GetCountries(Guid storeId)
            => _CommerceApi.GetCountries(storeId)
                .Select(x => new uSyncDependency
                {
                    Name = x.Name,
                    Order = CommerceConstants.Priorites.Country,
                    Udi = Udi.Create(CommerceConstants.UdiEntityType.Country, x.Id)
                });

        private IEnumerable<uSyncDependency> GetRegions(Guid storeId)
            => _CommerceApi.GetRegions(storeId)
                .Select(x => new uSyncDependency
                {
                    Name = x.Name,
                    Order = CommerceConstants.Priorites.Region,
                    Udi = Udi.Create(CommerceConstants.UdiEntityType.Region, x.Id)
                });


        private IEnumerable<uSyncDependency> GetPaymentMethods(Guid storeId)
            => _CommerceApi.GetPaymentMethods(storeId)
                .Select(x => new uSyncDependency
                {
                    Name = x.Name,
                    Order = CommerceConstants.Priorites.PaymentMethod,
                    Udi = Udi.Create(CommerceConstants.UdiEntityType.PaymentMethod, x.Id)
                });

        private IEnumerable<uSyncDependency> GetTaxClasses(Guid storeId)
            => _CommerceApi.GetTaxClasses(storeId)
                .Select(x => new uSyncDependency
                {
                    Name = x.Name,
                    Order = CommerceConstants.Priorites.TaxClass,
                    Udi = Udi.Create(CommerceConstants.UdiEntityType.TaxClass, x.Id)
                });

        private IEnumerable<uSyncDependency> GetEmailTemplates(Guid storeId)
            => _CommerceApi.GetEmailTemplates(storeId)
                .Select(x => new uSyncDependency
                {
                    Name = x.Name,
                    Order = CommerceConstants.Priorites.EmailTemplate,
                    Udi = Udi.Create(CommerceConstants.UdiEntityType.EmailTemplate, x.Id)
                });

        private IEnumerable<uSyncDependency> GetExportTemplates(Guid storeId)
            => _CommerceApi.GetExportTemplates(storeId)
                .Select(x => new uSyncDependency
                {
                    Name = x.Name,
                    Order = CommerceConstants.Priorites.EmailTemplate,
                    Udi = Udi.Create(CommerceConstants.UdiEntityType.EmailTemplate, x.Id)
                });

        private IEnumerable<uSyncDependency> GetPrintTemplates(Guid storeId)
            => _CommerceApi.GetPrintTemplates(storeId)
                .Select(x => new uSyncDependency
                {
                    Name = x.Name,
                    Order = CommerceConstants.Priorites.PrintTemplate,
                    Udi = Udi.Create(CommerceConstants.UdiEntityType.PrintTemplate, x.Id)
                });

        private IEnumerable<uSyncDependency> GetLocations(Guid storeId)
            => _CommerceApi.GetLocations(storeId)
                .Select(x => new uSyncDependency
                {
                    Name = x.Name,
                    Order = CommerceConstants.Priorites.Location,
                    Udi = Udi.Create(CommerceConstants.UdiEntityType.Location, x.Id)
                });

        private IEnumerable<uSyncDependency> GetGiftCards(Guid storeId)
            => _giftCardService.GetGiftCards(storeId)
                .Select(x => new uSyncDependency
                {
                    Name = x.Code,
                    Order = CommerceConstants.Priorites.GiftCard,
                    Udi = Udi.Create(CommerceConstants.UdiEntityType.GiftCard, x.Id)
                });

    }
}
