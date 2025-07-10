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
        "D0D7176C-2EDD-453E-9795-D71F1D29B44A",
        "Print Template Serializer",
        CommerceConstants.Serialization.PrintTemplate
    )]
    public class PrintTemplateSerializer
        : CommerceSerializerBase<PrintTemplateReadOnly>,
            ISyncSerializer<PrintTemplateReadOnly>
    {
        public PrintTemplateSerializer(
            ICommerceApi CommerceApi,
            CommerceSyncSettingsAccessor settingsAccessor,
            IUnitOfWorkProvider uowProvider,
            ILogger<PrintTemplateSerializer> logger
        )
            : base(CommerceApi, settingsAccessor, uowProvider, logger) { }

        protected override Task<SyncAttempt<XElement>> SerializeCoreAsync(
            PrintTemplateReadOnly item,
            SyncSerializerOptions options
        )
        {
            var node = InitializeBaseNode(item, ItemAlias(item));

            node.Add(new XElement(nameof(item.Name), item.Name));
            node.Add(new XElement(nameof(item.SortOrder), item.SortOrder));
            node.AddStoreId(item.StoreId);

            node.Add(new XElement(nameof(item.Category), item.Category));
            node.Add(new XElement(nameof(item.TemplateView), item.TemplateView));

            return Task.FromResult(
                SyncAttemptSucceedIf(node != null, item.Name, node, ChangeType.Export)
            );
        }

        public override bool IsValid(XElement node) =>
            base.IsValid(node) && node.GetStoreId() != Guid.Empty;

        protected override async Task<SyncAttempt<PrintTemplateReadOnly>> DeserializeCoreAsync(
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
                PrintTemplate item;
                if (readOnlyItem == null)
                {
                    item = await PrintTemplate.CreateAsync(uow, id, storeId, alias, name);
                }
                else
                {
                    item = await readOnlyItem.AsWritableAsync(uow);
                    await item.SetAliasAsync(alias).SetNameAsync(name);
                }

                await item.SetCategoryAsync(
                        node.Element(nameof(item.Category)).ValueOrDefault(item.Category)
                    )
                    .SetTemplateViewAsync(
                        node.Element(nameof(item.TemplateView)).ValueOrDefault(item.TemplateView)
                    );

                await _CommerceApi.SavePrintTemplateAsync(item);

                uow.Complete();

                return SyncAttemptSucceed(name, item.AsReadOnly(), ChangeType.Import);
            });
        }

        //

        public override string GetItemAlias(PrintTemplateReadOnly item) => item.Alias;

        public override Task DoDeleteItemAsync(PrintTemplateReadOnly item) =>
            _CommerceApi.DeletePrintTemplateAsync(item.Id);

        public override Task<PrintTemplateReadOnly> DoFindItemAsync(Guid key) =>
            _CommerceApi.GetPrintTemplateAsync(key);

        public override Task<PrintTemplateReadOnly> DoFindItemAsync(string alias) => null;

        public override Task DoSaveItemAsync(PrintTemplateReadOnly item) =>
            _uowProvider.ExecuteAsync(async uow =>
            {
                var entity = await item.AsWritableAsync(uow);
                await _CommerceApi.SavePrintTemplateAsync(entity);
                uow.Complete();
            });
    }
}
