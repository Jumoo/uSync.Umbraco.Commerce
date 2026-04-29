using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Umbraco.Commerce.Common;
using Umbraco.Commerce.Core.Api;
using Umbraco.Commerce.Core.Models;
using Umbraco.Commerce.Extensions;
using uSync.Core;
using uSync.Core.Models;
using uSync.Core.Serialization;
using uSync.Umbraco.Commerce.Configuration;
using uSync.Umbraco.Commerce.Extensions;

namespace uSync.Umbraco.Commerce.Serializers;

[SyncSerializer(
    "26892D7B-6C23-4EAA-8902-91730E4C86BB",
    "Location Serializer",
    CommerceConstants.Serialization.Location
)]
public class CommerceLocationSerializer
    : MethodSerializerBase<LocationReadOnly>,
        ISyncSerializer<LocationReadOnly>
{
    public CommerceLocationSerializer(
        ICommerceApi CommerceApi,
        CommerceSyncSettingsAccessor settingsAccessor,
        IUnitOfWorkProvider uowProvider,
        ILogger<MethodSerializerBase<LocationReadOnly>> logger
    )
        : base(CommerceApi, settingsAccessor, uowProvider, logger) { }

    protected override async Task<SyncAttempt<XElement>> SerializeCoreAsync(
        LocationReadOnly item,
        SyncSerializerOptions options
    )
    {
        var node = InitializeBaseNode(item, ItemAlias(item));

        node.Add(new XElement(nameof(item.Name), item.Name));
        node.Add(new XElement(nameof(item.Type), item.Type));
        node.Add(new XElement(nameof(item.SortOrder), item.SortOrder));

        node.Add(new XElement(nameof(item.AddressLine1), item.AddressLine1));
        node.Add(new XElement(nameof(item.AddressLine2), item.AddressLine2));
        node.Add(new XElement(nameof(item.City), item.City));
        node.Add(new XElement(nameof(item.ZipCode), item.ZipCode));
        node.Add(new XElement(nameof(item.CountryIsoCode), item.CountryIsoCode));
        node.Add(new XElement(nameof(item.Region), item.Region));

        var store = await LookupStoreAsync(item.StoreId);
        node.AddStoreId(item.StoreId, store?.Alias);

        return SyncAttemptSucceedIf(node != null, item.Alias, node, Core.ChangeType.Export);
    }

    protected override async Task<SyncAttempt<LocationReadOnly>> DeserializeCoreAsync(
        XElement node,
        SyncSerializerOptions options
    )
    {
        var readonlyItem = await FindItemAsync(node);

        var alias = node.GetAlias();
        var key = node.GetKey();
        var name = node.Element(nameof(readonlyItem.Name)).ValueOrDefault(alias);
        var storeId = node.GetStoreId();

        return await _uowProvider.ExecuteAsync(async uow =>
        {
            Location location;
            if (readonlyItem is null)
            {
                var store = await LookupStoreAsync(node);
                if (store is null)
                    return SyncAttempt<LocationReadOnly>.Fail(alias, ChangeType.Import, $"Store with id {storeId} not found.");

                location = await Location.CreateAsync(uow, key, store.Id, alias, name);
            }
            else
            {
                location = await readonlyItem.AsWritableAsync(uow);
                await location.SetAliasAsync(alias).SetNameAsync(name);
            }

            var address = new Address(
                addressLine1: node.Element(nameof(location.AddressLine1))
                    .ValueOrDefault(location.AddressLine1),
                addressLine2: node.Element(nameof(location.AddressLine2))
                    .ValueOrDefault(location.AddressLine2),
                city: node.Element(nameof(location.City)).ValueOrDefault(location.City),
                region: node.Element(nameof(location.Region)).ValueOrDefault(location.Region),
                countryIsoCode: node.Element(nameof(location.CountryIsoCode))
                    .ValueOrDefault(location.CountryIsoCode),
                zipCode: node.Element(nameof(location.ZipCode)).ValueOrDefault(location.ZipCode)
            );

            await location
                .SetTypeAsync(node.Element(nameof(location.Type)).ValueOrDefault(location.Type))
                .SetSortOrderAsync(
                    node.Element(nameof(location.SortOrder)).ValueOrDefault(location.SortOrder)
                )
                .SetAddressAsync(address);

            await _CommerceApi.SaveLocationAsync(location);

            uow.Complete();

            return SyncAttemptSucceed(name, location.AsReadOnly(), ChangeType.Import);
        });
    }

    public override string GetItemAlias(LocationReadOnly item) => item.Alias;

    public override Task DoDeleteItemAsync(LocationReadOnly item) =>
        _CommerceApi.DeleteLocationAsync(item.Id);

    public override Task<LocationReadOnly> DoFindItemAsync(Guid key) =>
        _CommerceApi.GetLocationAsync(key);

    public override async Task<LocationReadOnly> DoFindItemAsync(string alias, Guid storeId)
        => await _CommerceApi.GetLocationAsync(storeId, alias);

    public override Task DoSaveItemAsync(LocationReadOnly item) =>
        _uowProvider.ExecuteAsync(async uow =>
        {
            var entity = await item.AsWritableAsync(uow);
            await _CommerceApi.SaveLocationAsync(entity);
            uow.Complete();
        });
}
