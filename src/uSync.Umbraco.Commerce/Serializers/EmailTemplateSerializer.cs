using System;
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

namespace uSync.Umbraco.Commerce.Serializers
{
    [SyncSerializer(
        "BAEB7691-9AC2-4F42-92DA-2F8CD42B66DE",
        "Email Template Serializer",
        CommerceConstants.Serialization.EmailTemplate
    )]
    public class EmailTemplateSerializer
        : CommerceSerializerBase<EmailTemplateReadOnly>,
            ISyncSerializer<EmailTemplateReadOnly>
    {
        public EmailTemplateSerializer(
            ICommerceApi CommerceApi,
            CommerceSyncSettingsAccessor settingsAccessor,
            IUnitOfWorkProvider uowProvider,
            ILogger<EmailTemplateSerializer> logger
        )
            : base(CommerceApi, settingsAccessor, uowProvider, logger) { }

        protected override async Task<SyncAttempt<XElement>> SerializeCoreAsync(
            EmailTemplateReadOnly item,
            SyncSerializerOptions options
        )
        {
            var node = InitializeBaseNode(item, ItemAlias(item));

            node.Add(new XElement(nameof(item.Name), item.Name));
            node.Add(new XElement(nameof(item.SortOrder), item.SortOrder));

            var store = await LookupStoreAsync(item.StoreId);
            node.AddStoreId(item.StoreId, store?.Alias);

            node.Add(new XElement(nameof(item.Category), item.Category));

            node.Add(SerailizeList(nameof(item.ToAddresses), "Address", item.ToAddresses));
            node.Add(SerailizeList(nameof(item.BccAddresses), "Address", item.BccAddresses));
            node.Add(SerailizeList(nameof(item.CcAddresses), "Address", item.CcAddresses));

            node.Add(new XElement(nameof(item.SenderAddress), item.SenderAddress));
            node.Add(new XElement(nameof(item.SenderName), item.SenderName));
            node.Add(new XElement(nameof(item.SendToCustomer), item.SendToCustomer));

            node.Add(new XElement(nameof(item.Subject), item.Subject));
            node.Add(new XElement(nameof(item.TemplateView), item.TemplateView));

            // new Umbraco.Commerce
            node.Add(new XElement(nameof(item.ReplyToAddresses), item.ReplyToAddresses));

            return SyncAttemptSucceedIf(node != null, item.Name, node, ChangeType.Export);
            
        }

        public override bool IsValid(XElement node) =>
            base.IsValid(node) && node.GetStoreId() != Guid.Empty;

        protected override async Task<SyncAttempt<EmailTemplateReadOnly>> DeserializeCoreAsync(
            XElement node,
            SyncSerializerOptions options
        )
        {
            var readOnlyItem = await FindItemAsync(node);

            var alias = node.GetAlias();
            var id = node.GetKey();
            var name = node.Element(nameof(readOnlyItem.Name)).ValueOrDefault(alias);
            var storeId = node.GetStoreId();

            return await _uowProvider.ExecuteAsync(async uow =>
            {
                EmailTemplate item;
                if (readOnlyItem == null)
                {
                    var store = await LookupStoreAsync(node);
                    if (store is null)
                        return SyncAttempt<EmailTemplateReadOnly>.Fail(alias, ChangeType.Import, $"Store with id {storeId} not found.");

                    item = await EmailTemplate.CreateAsync(uow, id, store.Id, alias, name);
                }
                else
                {
                    item = await readOnlyItem.AsWritableAsync(uow);
                    await item.SetAliasAsync(alias).SetNameAsync(name);
                }

                await item.SetCategoryAsync(
                        node.Element(nameof(item.Category)).ValueOrDefault(item.Category)
                    )
                    .SetSenderNameAsync(
                        node.Element(nameof(item.SenderName)).ValueOrDefault(item.SenderName)
                    )
                    .SetSenderAddressAsync(
                        node.Element(nameof(item.SenderAddress)).ValueOrDefault(item.SenderAddress)
                    )
                    .SetSendToCustomerAsync(
                        node.Element(nameof(item.SendToCustomer))
                            .ValueOrDefault(item.SendToCustomer)
                    )
                    .SetSortOrderAsync(
                        node.Element(nameof(item.SortOrder)).ValueOrDefault(item.SortOrder)
                    )
                    .SetSubjectAsync(
                        node.Element(nameof(item.Subject)).ValueOrDefault(item.Subject)
                    )
                    .SetTemplateViewAsync(
                        node.Element(nameof(item.TemplateView)).ValueOrDefault(item.TemplateView)
                    )
                    .SetToAddressesAsync(
                        DeserializeList<string>(node, nameof(item.ToAddresses), "Address")
                    )
                    .SetBccAddressesAsync(
                        DeserializeList<string>(node, nameof(item.BccAddresses), "Address")
                    )
                    .SetCcAddressesAsync(
                        DeserializeList<string>(node, nameof(item.CcAddresses), "Address")
                    )
                    .SetReplyToAddressesAsync(
                        DeserializeList<string>(node, nameof(item.ReplyToAddresses), "Addresses")
                    );

                await _CommerceApi.SaveEmailTemplateAsync(item);

                uow.Complete();

                return SyncAttemptSucceed(name, item.AsReadOnly(), ChangeType.Import);
            });
        }

        //

        public override string GetItemAlias(EmailTemplateReadOnly item) => item.Alias;

        public override Task DoDeleteItemAsync(EmailTemplateReadOnly item) =>
            _CommerceApi.DeleteEmailTemplateAsync(item.Id);

        public override Task<EmailTemplateReadOnly> DoFindItemAsync(Guid key) =>
            _CommerceApi.GetEmailTemplateAsync(key);

        public override async Task<EmailTemplateReadOnly> DoFindItemAsync(string alias, Guid storeId)
            => await _CommerceApi.GetEmailTemplateAsync(storeId, alias);

        public override Task DoSaveItemAsync(EmailTemplateReadOnly item) =>
            _uowProvider.ExecuteAsync(async uow =>
            {
                var entity = await item.AsWritableAsync(uow);
                await _CommerceApi.SaveEmailTemplateAsync(entity);
                uow.Complete();
            });
    }
}
