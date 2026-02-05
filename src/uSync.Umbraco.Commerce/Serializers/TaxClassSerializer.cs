using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Umbraco.Commerce.Common;
using Umbraco.Commerce.Core.Api;
using Umbraco.Commerce.Core.Models;
using Umbraco.Commerce.Extensions;
using Umbraco.Extensions;
using uSync.Core;
using uSync.Core.Models;
using uSync.Core.Serialization;
using uSync.Umbraco.Commerce.Configuration;
using uSync.Umbraco.Commerce.Extensions;
using uSync.Umbraco.Commerce.SyncModels;

namespace uSync.Umbraco.Commerce.Serializers
{
    [SyncSerializer(
        "22F98052-DD59-4A0C-AA13-52398B794ED5",
        "TaxClass Serializer",
        CommerceConstants.Serialization.TaxClass
    )]
    public class TaxClassSerializer
        : CommerceSerializerBase<TaxClassReadOnly>,
            ISyncSerializer<TaxClassReadOnly>
    {
        public TaxClassSerializer(
            ICommerceApi CommerceApi,
            CommerceSyncSettingsAccessor settingsAccessor,
            IUnitOfWorkProvider uowProvider,
            ILogger<TaxClassSerializer> logger
        )
            : base(CommerceApi, settingsAccessor, uowProvider, logger) { }

        protected override Task<SyncAttempt<XElement>> SerializeCoreAsync(
            TaxClassReadOnly item,
            SyncSerializerOptions options
        )
        {
            var node = InitializeBaseNode(item, ItemAlias(item));

            node.Add(new XElement(nameof(item.Name), item.Name));
            node.Add(new XElement(nameof(item.SortOrder), item.SortOrder));
            node.AddStoreId(item.StoreId);

            node.Add(new XElement(nameof(item.DefaultTaxRate), item.DefaultTaxRate.Value));

            node.Add(SerializeTaxRates(item));

            return Task.FromResult(
                SyncAttemptSucceedIf(node != null, item.Name, node, ChangeType.Export)
            );
        }

        private XElement SerializeTaxRates(TaxClassReadOnly item)
        {
            var root = new XElement("TaxClasses");

            foreach (var rate in item.CountryRegionTaxClasses)
            {
                root.Add(
                    new XElement(
                        "Rate",
                        new XElement("CountryId", rate.CountryId),
                        new XElement("RegionId", rate.RegionId),
                        new XElement("TaxCode", rate.TaxCode),
                        new XElement("TaxRate", rate.TaxRate)
                    )
                );
            }

            return root;
        }

        public override bool IsValid(XElement node) =>
            base.IsValid(node) && node.GetStoreId() != Guid.Empty;

        protected override async Task<SyncAttempt<TaxClassReadOnly>> DeserializeCoreAsync(
            XElement node,
            SyncSerializerOptions options
        )
        {
            var readonlyItem = await FindItemAsync(node);

            var alias = node.GetAlias();
            var id = node.GetKey();
            var name = node.Element(nameof(readonlyItem.Name)).ValueOrDefault(alias);
            var storeId = node.GetStoreId();
            var defaultTaxRate = node.Element(nameof(readonlyItem.DefaultTaxRate))
                .ValueOrDefault((decimal)0);

            return await _uowProvider.ExecuteAsync(async uow =>
            {
                TaxClass item;
                if (readonlyItem == null)
                {
                    item = await TaxClass.CreateAsync(
                        uow,
                        id,
                        storeId,
                        alias,
                        name,
                        defaultTaxRate
                    );
                }
                else
                {
                    item = await readonlyItem.AsWritableAsync(uow);
                    await item.SetAliasAsync(alias)
                        .SetNameAsync(name)
                        .SetDefaultTaxRateAsync(defaultTaxRate);
                }

                await item.SetSortOrderAsync(
                    node.Element(nameof(item.SortOrder)).ValueOrDefault(item.SortOrder)
                );

                await DeserializeTaxRatesAsync(node, item);

                await _CommerceApi.SaveTaxClassAsync(item);

                uow.Complete();

                return SyncAttemptSucceed(name, item.AsReadOnly(), ChangeType.Import);
            });
        }

        protected List<SyncTaxRateModel> GetTaxRates(XElement node)
        {
            var taxRates = new List<SyncTaxRateModel>();

            // load the regions from the xml.
            var root = node.Element("TaxRates");
            if (root != null && root.HasElements)
            {
                foreach (var value in root.Elements("Rate"))
                {
                    taxRates.Add(
                        new SyncTaxRateModel
                        {
                            CountryId = value.GetGuidValue("CountryId"),
                            RegionId = value.GetGuidValue("RegionId"),
                            Rate = value.Element("TaxRate").ValueOrDefault((decimal)0),
                        }
                    );
                }
            }

            return taxRates;
        }

        protected async Task DeserializeTaxRatesAsync(XElement node, TaxClass item)
        {
            var rates = GetTaxRates(node);

            var ratesToRemove = item
                .CountryRegionTaxClasses.Where(x =>
                    rates == null
                    || !rates.Any(y => y.CountryId == x.CountryId && y.RegionId == x.RegionId)
                )
                .ToList();

            foreach (var rate in rates)
            {
                if (rate.RegionId == null)
                {
                    await item.SetCountryTaxClassAsync(
                        rate.CountryId.Value,
                        rate.Rate,
                        rate.TaxCode
                    );
                }
                else
                {
                    await item.SetRegionTaxClassAsync(
                        rate.CountryId.Value,
                        rate.RegionId.Value,
                        rate.Rate,
                        rate.TaxCode
                    );
                }
            }

            foreach (var rate in ratesToRemove)
            {
                if (rate.RegionId == null)
                {
                    await item.ClearCountryTaxClassAsync(rate.CountryId);
                }
                else
                {
                    await item.ClearRegionTaxClassAsync(rate.CountryId, rate.RegionId.Value);
                }
            }
        }

        public override string GetItemAlias(TaxClassReadOnly item) => item.Alias;

        public override Task DoDeleteItemAsync(TaxClassReadOnly item) =>
            _CommerceApi.DeleteTaxClassAsync(item.Id);

        public override Task<TaxClassReadOnly> DoFindItemAsync(Guid key) =>
            _CommerceApi.GetTaxClassAsync(key);

        public override Task<TaxClassReadOnly> DoFindItemAsync(string alias, Guid storeId)
            => _CommerceApi.GetTaxClassAsync(storeId, alias);

        public override Task DoSaveItemAsync(TaxClassReadOnly item) =>
            _uowProvider.ExecuteAsync(async uow =>
            {
                var entity = await item.AsWritableAsync(uow);
                await _CommerceApi.SaveTaxClassAsync(entity);
                uow.Complete();
            });
    }
}
