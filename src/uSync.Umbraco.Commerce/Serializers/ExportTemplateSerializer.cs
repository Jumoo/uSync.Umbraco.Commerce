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
        "6D4C64D0-B840-47F7-AF92-61A1C86D892E",
        "Export Template Serializer",
        CommerceConstants.Serialization.ExportTemplate
    )]
    public class ExportTemplateSerializer
        : CommerceSerializerBase<ExportTemplateReadOnly>,
            ISyncSerializer<ExportTemplateReadOnly>
    {
        public ExportTemplateSerializer(
            ICommerceApi CommerceApi,
            CommerceSyncSettingsAccessor settingsAccessor,
            IUnitOfWorkProvider uowProvider,
            ILogger<ExportTemplateSerializer> logger
        )
            : base(CommerceApi, settingsAccessor, uowProvider, logger) { }

        protected override Task<SyncAttempt<XElement>> SerializeCoreAsync(
            ExportTemplateReadOnly item,
            SyncSerializerOptions options
        )
        {
            var node = InitializeBaseNode(item, ItemAlias(item));

            node.Add(new XElement(nameof(item.Name), item.Name));
            node.Add(new XElement(nameof(item.SortOrder), item.SortOrder));
            node.AddStoreId(item.StoreId);

            node.Add(new XElement(nameof(item.Category), item.Category));
            node.Add(new XElement(nameof(item.FileMimeType), item.FileMimeType));
            node.Add(new XElement(nameof(item.FileExtension), item.FileExtension));
            node.Add(new XElement(nameof(item.ExportStrategy), item.ExportStrategy));
            node.Add(new XElement(nameof(item.TemplateView), item.TemplateView));

            return Task.FromResult(
                SyncAttemptSucceedIf(node != null, item.Name, node, ChangeType.Export)
            );
        }

        public override bool IsValid(XElement node) =>
            base.IsValid(node) && node.GetStoreId() != Guid.Empty;

        protected override async Task<SyncAttempt<ExportTemplateReadOnly>> DeserializeCoreAsync(
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
                ExportTemplate item;
                if (readOnlyItem == null)
                {
                    item = await ExportTemplate.CreateAsync(uow, id, storeId, alias, name);
                }
                else
                {
                    item = await readOnlyItem.AsWritableAsync(uow);
                    await item.SetAliasAsync(alias).SetNameAsync(name);
                }

                await item.SetCategoryAsync(
                        node.Element(nameof(item.Category)).ValueOrDefault(item.Category)
                    )
                    .SetFileMimeTypeAsync(
                        node.Element(nameof(item.FileMimeType)).ValueOrDefault(item.FileMimeType)
                    )
                    .SetFileExtensionAsync(
                        node.Element(nameof(item.FileExtension)).ValueOrDefault(item.FileExtension)
                    )
                    .SetExportStrategyAsync(
                        node.Element(nameof(item.ExportStrategy))
                            .ValueOrDefault(item.ExportStrategy)
                    )
                    .SetTemplateViewAsync(
                        node.Element(nameof(item.TemplateView)).ValueOrDefault(item.TemplateView)
                    );

                await _CommerceApi.SaveExportTemplateAsync(item);

                uow.Complete();

                return SyncAttemptSucceed(name, item.AsReadOnly(), ChangeType.Import);
            });
        }

        //

        public override string GetItemAlias(ExportTemplateReadOnly item) => item.Alias;

        public override Task DoDeleteItemAsync(ExportTemplateReadOnly item) =>
            _CommerceApi.DeleteExportTemplateAsync(item.Id);

        public override Task<ExportTemplateReadOnly> DoFindItemAsync(Guid key) =>
            _CommerceApi.GetExportTemplateAsync(key);

        public override async Task<ExportTemplateReadOnly> DoFindItemAsync(string alias, Guid storeId)
            => await _CommerceApi.GetExportTemplateAsync(storeId, alias);

        public override Task DoSaveItemAsync(ExportTemplateReadOnly item) =>
            _uowProvider.ExecuteAsync(async uow =>
            {
                var entity = await item.AsWritableAsync(uow);
                await _CommerceApi.SaveExportTemplateAsync(entity);
                uow.Complete();
            });
    }
}
