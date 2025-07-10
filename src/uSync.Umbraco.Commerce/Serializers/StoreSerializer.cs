using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Services;
using Umbraco.Commerce.Common;
using Umbraco.Commerce.Core.Api;
using Umbraco.Commerce.Core.Models;
using Umbraco.Commerce.Extensions;
using Umbraco.Extensions;
using uSync.Core;
using uSync.Core.Models;
using uSync.Core.Serialization;
using uSync.Umbraco.Commerce.Configuration;

namespace uSync.Umbraco.Commerce.Serializers
{
    [SyncSerializer(
        "d4d2593e-04ad-4a32-9ca7-e2a5b2ff2725",
        "Store Serializer",
        CommerceConstants.Serialization.Store,
        IsTwoPass = true
    )]
    public class StoreSerializer
        : CommerceSerializerBase<StoreReadOnly>,
            ISyncSerializer<StoreReadOnly>
    {
        private IUserService _userService;
        private IUserGroupService _userGroupService;

        public StoreSerializer(
            IUserService userService,
            ICommerceApi CommerceApi,
            CommerceSyncSettingsAccessor settingsAccessor,
            IUnitOfWorkProvider uowProvider,
            ILogger<StoreSerializer> logger,
            IUserGroupService userGroupService
        )
            : base(CommerceApi, settingsAccessor, uowProvider, logger)
        {
            _userService = userService;
            _userGroupService = userGroupService;
        }

        protected override async Task<SyncAttempt<XElement>> SerializeCoreAsync(
            StoreReadOnly item,
            SyncSerializerOptions options
        )
        {
            // makes the basic xml,
            var node = InitializeBaseNode(item, item.Alias);

            node.Add(new XElement("Name", item.Name));
            node.Add(new XElement(nameof(item.SortOrder), item.SortOrder));

            node.Add(new XElement(nameof(item.PricesIncludeTax), item.PricesIncludeTax));
            node.Add(new XElement(nameof(item.CookieTimeout), item.CookieTimeout));
            node.Add(new XElement(nameof(item.CartNumberTemplate), item.CartNumberTemplate));

            // product
            node.Add(
                new XElement(
                    nameof(item.ProductPropertyAliases),
                    string.Join(",", item.ProductPropertyAliases)
                )
            );
            node.Add(
                new XElement(
                    nameof(item.ProductUniquenessPropertyAliases),
                    item.ProductUniquenessPropertyAliases
                )
            );

            // gift card
            node.Add(new XElement(nameof(item.GiftCardCodeLength), item.GiftCardCodeLength));
            node.Add(new XElement(nameof(item.GiftCardDaysValid), item.GiftCardDaysValid));
            node.Add(new XElement(nameof(item.GiftCardCodeTemplate), item.GiftCardCodeTemplate));
            node.Add(
                new XElement(
                    nameof(item.GiftCardPropertyAliases),
                    string.Join(",", item.GiftCardPropertyAliases)
                )
            );
            node.Add(
                new XElement(
                    nameof(item.GiftCardActivationMethod),
                    (int)item.GiftCardActivationMethod
                )
            );

            // order
            // TODO: Where is OrderEditorConfig? node.Add(new XElement(nameof(item.OrderEditorConfig), item.OrderEditorConfig));
            node.Add(new XElement(nameof(item.OrderNumberTemplate), item.OrderNumberTemplate));

            node.Add(AddNullableGuid(nameof(item.BaseCurrencyId), item.BaseCurrencyId));
            node.Add(AddNullableGuid(nameof(item.DefaultCountryId), item.DefaultCountryId));
            node.Add(AddNullableGuid(nameof(item.DefaultTaxClassId), item.DefaultTaxClassId));
            node.Add(AddNullableGuid(nameof(item.DefaultOrderStatusId), item.DefaultOrderStatusId));
            node.Add(AddNullableGuid(nameof(item.ErrorOrderStatusId), item.ErrorOrderStatusId));
            node.Add(AddNullableGuid(nameof(item.DefaultLocationId), item.DefaultLocationId));

            node.Add(new XElement(nameof(item.MeasurementSystem), item.MeasurementSystem));

            node.Add(
                AddNullableGuid(
                    nameof(item.GiftCardActivationOrderStatusId),
                    item.GiftCardActivationOrderStatusId
                )
            );
            node.Add(
                AddNullableGuid(
                    nameof(item.DefaultGiftCardEmailTemplateId),
                    item.DefaultGiftCardEmailTemplateId
                )
            );

            node.Add(
                AddNullableGuid(
                    nameof(item.ConfirmationEmailTemplateId),
                    item.ConfirmationEmailTemplateId
                )
            );
            node.Add(AddNullableGuid(nameof(item.ErrorEmailTemplateId), item.ErrorEmailTemplateId));

            node.Add(AddNullableGuid(nameof(item.ErrorOrderStatusId), item.ErrorOrderStatusId));
            node.Add(
                AddNullableGuid(nameof(item.ShareStockFromStoreId), item.ShareStockFromStoreId)
            );

            // new to Umbraco.Commerce ?

            // order rounding method
            node.Add(new XElement(nameof(item.OrderRoundingMethod), item.OrderRoundingMethod));

            SerializeAllowedUsers(node, item);

            await SerializeUserRolesAsync(node, item);

            return SyncAttemptSucceedIf(node != null, item.Name, node, ChangeType.Export);
        }

        private void SerializeAllowedUsers(XElement node, StoreReadOnly item)
        {
            var allowedUsers = new XElement(nameof(item.AllowedUsers));

            if (item.AllowedUsers.Count > 0)
            {
                foreach (var id in item.AllowedUsers)
                {
                    var user = _userService.GetByProviderKey(id.UserId);
                    if (user != null)
                    {
                        allowedUsers.Add(new XElement("User", user.Username));
                    }
                }
            }

            node.Add(allowedUsers);
        }

        private async Task SerializeUserRolesAsync(XElement node, StoreReadOnly item)
        {
            var allowedRoles = new XElement(nameof(item.AllowedUserRoles));
            if (item.AllowedUserRoles.Count > 0)
            {
                foreach (var role in item.AllowedUserRoles)
                {
                    var group = await _userGroupService.GetAsync(role.Role);
                    if (group != null)
                    {
                        allowedRoles.Add(new XElement("Role", group.Alias));
                    }
                }
            }

            node.Add(allowedRoles);
        }

        public override bool IsValid(XElement node) =>
            base.IsValid(node)
            && !string.IsNullOrWhiteSpace(node.Element("Name").ValueOrDefault(string.Empty));

        protected override async Task<SyncAttempt<StoreReadOnly>> DeserializeCoreAsync(
            XElement node,
            SyncSerializerOptions options
        )
        {
            var readOnlyStore = await FindItemAsync(node);

            var alias = node.GetAlias();
            var id = node.GetKey();
            var name = node.Element("Name").ValueOrDefault(alias);

            return await _uowProvider.ExecuteAsync(async uow =>
            {
                Store store;
                if (readOnlyStore == null)
                {
                    store = await Store.CreateAsync(uow, id, alias, name, false);
                }
                else
                {
                    store = await readOnlyStore.AsWritableAsync(uow);
                }

                // here we have found or created the store item.

                await store
                    .SetNameAsync(name)
                    .SetSortOrderAsync(
                        node.Element(nameof(store.SortOrder)).ValueOrDefault(store.SortOrder)
                    )
                    .SetPriceTaxInclusivityAsync(
                        node.Element(nameof(store.PricesIncludeTax)).ValueOrDefault(false)
                    )
                    .SetCartNumberTemplateAsync(
                        node.Element(nameof(store.CartNumberTemplate)).ValueOrDefault(string.Empty)
                    )
                    .SetProductPropertyAliasesAsync(
                        node.Element(nameof(store.ProductPropertyAliases))
                            .ValueOrDefault(string.Empty)
                            .ToDelimitedList()
                    )
                    .SetProductUniquenessPropertyAliasesAsync(
                        node.Element(nameof(store.ProductUniquenessPropertyAliases))
                            .ValueOrDefault(string.Empty)
                            .ToDelimitedList()
                    )
                    .SetGiftCardCodeLengthAsync(
                        node.Element(nameof(store.GiftCardCodeLength))
                            .ValueOrDefault(store.GiftCardCodeLength)
                    )
                    .SetGiftCardValidityTimeframeAsync(
                        node.Element(nameof(store.GiftCardDaysValid))
                            .ValueOrDefault(store.GiftCardDaysValid)
                    )
                    .SetGiftCardCodeTemplateAsync(
                        node.Element(nameof(store.GiftCardCodeTemplate))
                            .ValueOrDefault(store.GiftCardCodeTemplate)
                    )
                    .SetGiftCardActivationMethodAsync(
                        node.Element(nameof(store.GiftCardActivationMethod))
                            .ValueOrDefault(store.GiftCardActivationMethod)
                    );

                var measurementSystem = (MeasurementSystem)
                    node.Element(nameof(store.MeasurementSystem))
                        .ValueOrDefault((int)store.MeasurementSystem);
                await store.SetMeasurementSystemAsync(measurementSystem);

                var giftCardPropertyAliasList = node.Element(nameof(store.GiftCardPropertyAliases))
                    .ValueOrDefault(string.Empty)
                    .ToDelimitedList();
                if (giftCardPropertyAliasList != null && giftCardPropertyAliasList.Count > 0)
                {
                    await store.SetGiftCardPropertyAliasesAsync(giftCardPropertyAliasList);
                }
                else
                {
                    await store.ClearGiftCardPropertyAliasesAsync();
                }

                // base currency
                Guid? currencyId = await GetCurrencyIdAsync(node, nameof(store.BaseCurrencyId));
                await store.SetBaseCurrencyAsync(currencyId);

                // country
                Guid? countryId = await GetCountryIdAsync(node, nameof(store.DefaultCountryId));
                await store.SetDefaultCountryAsync(countryId);

                // tax class
                Guid? taxClassId = await GetTaxClassIdAsync(node, nameof(store.DefaultTaxClassId));
                await store.SetDefaultTaxClassAsync(taxClassId);

                Guid? defaultLocationId = await GetLocationIdAsync(
                    node,
                    nameof(store.DefaultLocationId)
                );
                await store.SetDefaultLocationAsync(defaultLocationId);

                // DefaultOrderStatus
                Guid? defaultOrderStatusId = await GetOrderStatusIdAsync(
                    node,
                    nameof(store.DefaultOrderStatusId)
                );
                await store.SetDefaultOrderStatusAsync(defaultOrderStatusId);

                // error order status
                Guid? errorOrderStatusId = await GetOrderStatusIdAsync(
                    node,
                    nameof(store.ErrorOrderStatusId)
                );
                await store.SetErrorOrderStatusAsync(errorOrderStatusId);

                // gift card template
                var defaultGiftCardEmailTemplateId = await GetEmailTemplateIdAsync(
                    node,
                    nameof(store.DefaultGiftCardEmailTemplateId)
                );
                await store.SetDefaultGiftCardEmailTemplateAsync(defaultGiftCardEmailTemplateId);

                // confimation email template
                var confirmationEmailTemplateId = await GetEmailTemplateIdAsync(
                    node,
                    nameof(store.ConfirmationEmailTemplateId)
                );
                await store.SetConfirmationEmailTemplateAsync(confirmationEmailTemplateId);

                // error email template
                var errorEmailTemplateId = await GetEmailTemplateIdAsync(
                    node,
                    nameof(store.ErrorEmailTemplateId)
                );
                await store.SetErrorEmailTemplateAsync(errorEmailTemplateId);

                // new for Umbraco.Commerce

                // order rounding method
                await store.SetOrderRoundingMethodAsync(
                    node.Element(nameof(store.OrderRoundingMethod))
                        .ValueOrDefault(store.OrderRoundingMethod)
                );

                await DeserializeAllowedUsersAsync(node, store);

                await DeserializeAllowedRoles(node, store);

                await _CommerceApi.SaveStoreAsync(store);

                uow.Complete();

                return SyncAttemptSucceed(name, store.AsReadOnly(), ChangeType.Import, true);
            });
        }

        private async Task DeserializeAllowedRoles(XElement node, Store store)
        {
            var roleIds = new List<string>();

            var collection = node.Element(nameof(store.AllowedUserRoles));
            if (collection != null && collection.HasElements)
            {
                foreach (var item in collection.Elements("Role"))
                {
                    var alias = item.Value;

                    if (!string.IsNullOrEmpty(alias))
                    {
                        var role = await _userGroupService.GetAsync(alias);
                        if (role != null)
                        {
                            roleIds.Add(role.Alias);
                        }
                    }
                }
            }

            await store.SetAllowedUserRolesAsync(roleIds, SetBehavior.Replace);
        }

        private async Task DeserializeAllowedUsersAsync(XElement node, Store store)
        {
            var userIds = new List<string>();

            var collection = node.Element(nameof(store.AllowedUsers));
            if (collection != null && collection.HasElements)
            {
                foreach (var item in collection.Elements("User"))
                {
                    var username = item.Value;

                    if (!string.IsNullOrEmpty(username))
                    {
                        var user = _userService.GetByUsername(username);
                        if (user != null)
                        {
                            userIds.Add(user.Id.ToString());
                        }
                    }
                }
            }

            await store.SetAllowedUsersAsync(userIds, SetBehavior.Replace);
        }

        /// <summary>
        ///  called as part of the serialization, after all stores are serialized.
        /// </summary>
        /// <remarks>
        ///  The second pass happens once all store items have gone through their first pass - as such you only need to put things
        ///  here that rely on other stores being setup.
        ///  </remarks>
        public override async Task<SyncAttempt<StoreReadOnly>> DeserializeSecondPassAsync(
            StoreReadOnly item,
            XElement node,
            SyncSerializerOptions options
        )
        {
            if (item == null)
                return SyncAttempt<StoreReadOnly>.Fail(
                    node.GetAlias(),
                    ChangeType.ImportFail,
                    "Store Item not set for second pass"
                );

            // currency
            return await _uowProvider.ExecuteAsync(async uow =>
            {
                var store = await item.AsWritableAsync(uow);

                // StockSharingStore
                var stockSharingStore = await GetStoreIdAsync(
                    node,
                    nameof(store.ShareStockFromStoreId)
                );
                if (stockSharingStore.HasValue)
                {
                    await store.ShareStockFromAsync(stockSharingStore.Value);
                }
                else
                {
                    await store.StopSharingStockAsync();
                }

                await _CommerceApi.SaveStoreAsync(store);
                uow.Complete();

                return SyncAttemptSucceed(store.Name, store.AsReadOnly(), ChangeType.Import, true);
            });
        }

        /// <summary>
        ///  gets a guid value from the xml, and if set checks with the passed Commerce method that it exsits
        /// </summary>
        /// <param name="node">XElement containing values</param>
        /// <param name="name">name of the node in the xml containing the guid</param>
        /// <param name="action">method (that returns a EntityBase) to check value</param>
        private async Task<Guid?> GetCommerceIdFromXml(
            XElement node,
            string name,
            Func<Guid, Task<EntityBase>> action
        )
        {
            var value = node.Element(name).ValueOrDefault(Guid.Empty);
            if (value != Guid.Empty)
            {
                return (await action(value))?.Id;
            }

            return null;
        }

        /// <summary>
        ///  Get the CurrencyId from the xml and confirm it exist in Commerce.
        /// </summary>
        private Task<Guid?> GetCurrencyIdAsync(XElement node, string name) =>
            GetCommerceIdFromXml(node, name, async (id) => await _CommerceApi.GetCurrencyAsync(id));

        /// <summary>
        ///  Get the CountryId from the xml and confirm it exist in Commerce.
        /// </summary>
        private Task<Guid?> GetCountryIdAsync(XElement node, string name) =>
            GetCommerceIdFromXml(node, name, async (id) => await _CommerceApi.GetCountryAsync(id));

        /// <summary>
        ///  Get the TaxClassId from the xml and confirm it exist in Commerce.
        /// </summary>
        private Task<Guid?> GetTaxClassIdAsync(XElement node, string name) =>
            GetCommerceIdFromXml(node, name, async (id) => await _CommerceApi.GetTaxClassAsync(id));

        /// <summary>
        ///  Get the OrderId from the xml and confirm it exist in Commerce.
        /// </summary>
        private Task<Guid?> GetOrderStatusIdAsync(XElement node, string name) =>
            GetCommerceIdFromXml(
                node,
                name,
                async (id) => await _CommerceApi.GetOrderStatusAsync(id)
            );

        /// <summary>
        ///  Get the EmailTemplateId from the xml and confirm it exist in Commerce.
        /// </summary>
        private Task<Guid?> GetEmailTemplateIdAsync(XElement node, string name) =>
            GetCommerceIdFromXml(
                node,
                name,
                async (id) => await _CommerceApi.GetEmailTemplateAsync(id)
            );

        private Task<Guid?> GetLocationIdAsync(XElement node, string name) =>
            GetCommerceIdFromXml(node, name, async (id) => await _CommerceApi.GetLocationAsync(id));

        /// <summary>
        ///  Get the StoreId from the xml and confirm it exist in Commerce.
        /// </summary>
        private Task<Guid?> GetStoreIdAsync(XElement node, string name) =>
            GetCommerceIdFromXml(node, name, async (id) => await _CommerceApi.GetStoreAsync(id));

        private XElement AddNullableGuid(string alias, Guid? value) =>
            new XElement(alias, value.HasValue ? value : Guid.Empty);

        // overloads to let base functions do the bulk of the work.

        public override string GetItemAlias(StoreReadOnly item) => item.Alias;

        public override Task<StoreReadOnly> DoFindItemAsync(Guid key) =>
            _CommerceApi.GetStoreAsync(key);

        public override Task<StoreReadOnly> DoFindItemAsync(string alias) =>
            _CommerceApi.GetStoreAsync(alias);

        public override Task DoSaveItemAsync(StoreReadOnly item) =>
            _uowProvider.ExecuteAsync(async uow =>
            {
                var entity = await item.AsWritableAsync(uow);
                await _CommerceApi.SaveStoreAsync(entity);
                uow.Complete();
            });

        public override Task DoDeleteItemAsync(StoreReadOnly item) =>
            _CommerceApi.DeleteStoreAsync(item.Id);
    }
}
