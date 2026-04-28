using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

using Umbraco.Commerce.Common;
using Umbraco.Commerce.Core.Api;
using Umbraco.Commerce.Core.Models;

using uSync.Core;
using uSync.Core.Models;
using uSync.Core.Serialization;
using uSync.Umbraco.Commerce.Configuration;
using uSync.Umbraco.Commerce.Extensions;

namespace uSync.Umbraco.Commerce.Serializers;

[SyncSerializer("C3E9C810-4B7C-4A12-9A3D-2B4F6E8D1A05", "Discount Serializer", CommerceConstants.Serialization.Discount)]
public class DiscountSerializer : CommerceSerializerBase<DiscountReadOnly>,
    ISyncSerializer<DiscountReadOnly>
{
    public DiscountSerializer(
        ICommerceApi CommerceApi,
        CommerceSyncSettingsAccessor settingsAccessor,
        IUnitOfWorkProvider uowProvider,
        ILogger<CommerceSerializerBase<DiscountReadOnly>> logger)
        : base(CommerceApi, settingsAccessor, uowProvider, logger)
    { }

    public override void DoDeleteItem(DiscountReadOnly item)
        => _CommerceApi.DeleteDiscount(item.Id);

    public override DiscountReadOnly DoFindItem(Guid key)
        => _CommerceApi.GetDiscount(key);

    public override string GetItemAlias(DiscountReadOnly item)
        => item.Alias;

    public override void DoSaveItem(DiscountReadOnly item)
    {
        using (var uow = _uowProvider.Create())
        {
            var discount = item.AsWritable(uow);
            discount.SetName(item.Name);
        }
    }

    public override bool IsValid(XElement node)
        => base.IsValid(node)
        && node.GetStoreId() != Guid.Empty;

    protected override SyncAttempt<XElement> SerializeCore(DiscountReadOnly item, SyncSerializerOptions options)
    {
        var node = InitializeBaseNode(item, ItemAlias(item));

        node.Add(new XElement(nameof(item.Name), item.Name));
        node.Add(new XElement(nameof(item.SortOrder), item.SortOrder));
        node.AddStoreId(item.StoreId);

        node.Add(new XElement(nameof(item.Type), item.Type));
        node.Add(new XElement(nameof(item.IsActive), item.IsActive));
        node.Add(new XElement(nameof(item.StartDate), item.StartDate));
        node.Add(new XElement(nameof(item.ExpiryDate), item.ExpiryDate));
        node.Add(new XElement(nameof(item.BlockFurtherDiscounts), item.BlockFurtherDiscounts));
        node.Add(new XElement(nameof(item.BlockIfPreviousDiscounts), item.BlockIfPreviousDiscounts));

        node.Add(SerializeCodes(item.Codes));
        node.Add(SerializeRuleConfig("Rules", item.Rules));
        node.Add(SerializeRewards(item.Rewards));

        return SyncAttemptSucceedIf(node != null, item.Name, node, ChangeType.Export);
    }

    protected override SyncAttempt<DiscountReadOnly> DeserializeCore(XElement node, SyncSerializerOptions options)
    {
        var readonlyItem = FindItem(node);

        var alias = node.GetAlias();
        var id = node.GetKey();
        var name = node.Element(nameof(DiscountReadOnly.Name)).ValueOrDefault(alias);
        var storeId = node.GetStoreId();

        using (var uow = _uowProvider.Create())
        {
            Discount item;
            if (readonlyItem == null)
            {
                item = Discount.Create(uow, id, storeId, alias, name);
            }
            else
            {
                item = readonlyItem.AsWritable(uow);
                item.SetAlias(alias)
                    .SetName(name);
            }

            item.SetSortOrder(node.Element(nameof(item.SortOrder)).ValueOrDefault(item.SortOrder));
            item.SetType(node.Element(nameof(item.Type)).ValueOrDefault(item.Type));
            item.SetActive(node.Element(nameof(item.IsActive)).ValueOrDefault(item.IsActive));
            item.SetBlockFurtherDiscounts(node.Element(nameof(item.BlockFurtherDiscounts)).ValueOrDefault(item.BlockFurtherDiscounts));
            item.SetBlockIfPreviousDiscounts(node.Element(nameof(item.BlockIfPreviousDiscounts)).ValueOrDefault(item.BlockIfPreviousDiscounts));

            var startDate = node.Element(nameof(item.StartDate)).ValueOrDefault<DateTime?>(null);
            var expiryDate = node.Element(nameof(item.ExpiryDate)).ValueOrDefault<DateTime?>(null);
            item.SetDateRange(startDate, expiryDate);

            item.SetCodes(DeserializeCodes(node), SetBehavior.Replace);
            item.SetRules(DeserializeRuleConfig(node.Element("Rules")));
            item.SetRewards(DeserializeRewards(node));

            _CommerceApi.SaveDiscount(item);
            uow.Complete();

            return SyncAttemptSucceed(name, item.AsReadOnly(), ChangeType.Import);
        }
    }

    private XElement SerializeCodes(IEnumerable<DiscountCode> codes)
    {
        var root = new XElement(nameof(DiscountReadOnly.Codes));
        foreach (var code in codes)
        {
            root.Add(new XElement("Code",
                new XElement(nameof(code.Id), code.Id),
                new XElement(nameof(code.Code), code.Code),
                new XElement(nameof(code.UsageLimit), code.UsageLimit),
                new XElement(nameof(code.IsUnlimited), code.IsUnlimited)));
        }
        return root;
    }

    private IEnumerable<DiscountCode> DeserializeCodes(XElement node)
    {
        var root = node.Element(nameof(DiscountReadOnly.Codes));
        if (root == null || !root.HasElements)
            return Enumerable.Empty<DiscountCode>();

        return root.Elements("Code").Select(el =>
        {
            var codeId = el.Element(nameof(DiscountCode.Id)).ValueOrDefault(Guid.NewGuid());
            var codeVal = el.Element(nameof(DiscountCode.Code)).ValueOrDefault(string.Empty);
            var usageLimit = el.Element(nameof(DiscountCode.UsageLimit)).ValueOrDefault<int?>(null);
            var isUnlimited = el.Element(nameof(DiscountCode.IsUnlimited)).ValueOrDefault(false);
            return new DiscountCode(codeId, codeVal, usageLimit, isUnlimited);
        }).ToList();
    }

    private XElement SerializeRuleConfig(string elementName, DiscountRuleConfig ruleConfig)
    {
        var root = new XElement(elementName);
        if (ruleConfig == null)
            return root;

        root.Add(new XElement(nameof(DiscountRuleConfig.RuleProviderAlias), ruleConfig.RuleProviderAlias));
        root.Add(SerializeSettings(ruleConfig.Settings));

        if (ruleConfig.Children != null && ruleConfig.Children.Any())
        {
            var children = new XElement("Children");
            foreach (var child in ruleConfig.Children)
                children.Add(SerializeRuleConfig("Rule", child));
            root.Add(children);
        }

        return root;
    }

    private DiscountRuleConfig DeserializeRuleConfig(XElement element)
    {
        if (element == null)
            return null;

        var ruleProviderAlias = element.Element(nameof(DiscountRuleConfig.RuleProviderAlias)).ValueOrDefault(string.Empty);
        if (string.IsNullOrEmpty(ruleProviderAlias))
            return null;

        var settings = DeserializeSettings(element);

        var childrenEl = element.Element("Children");
        if (childrenEl != null && childrenEl.HasElements)
        {
            var children = childrenEl.Elements("Rule")
                .Select(DeserializeRuleConfig)
                .Where(r => r != null)
                .ToList();
            return new DiscountRuleConfig(ruleProviderAlias, settings, children);
        }

        return new DiscountRuleConfig(ruleProviderAlias, settings);
    }

    private XElement SerializeRewards(IEnumerable<DiscountRewardConfig> rewards)
    {
        var root = new XElement(nameof(DiscountReadOnly.Rewards));
        foreach (var reward in rewards)
        {
            var rewardEl = new XElement("Reward");
            rewardEl.Add(new XElement(nameof(DiscountRewardConfig.RewardProviderAlias), reward.RewardProviderAlias));
            rewardEl.Add(SerializeSettings(reward.Settings));
            root.Add(rewardEl);
        }
        return root;
    }

    private IEnumerable<DiscountRewardConfig> DeserializeRewards(XElement node)
    {
        var root = node.Element(nameof(DiscountReadOnly.Rewards));
        if (root == null || !root.HasElements)
            return Enumerable.Empty<DiscountRewardConfig>();

        return root.Elements("Reward").Select(el =>
        {
            var alias = el.Element(nameof(DiscountRewardConfig.RewardProviderAlias)).ValueOrDefault(string.Empty);
            var settings = DeserializeSettings(el);
            return new DiscountRewardConfig(alias, settings);
        }).ToList();
    }

    private XElement SerializeSettings(IReadOnlyDictionary<string, string> settings)
    {
        var root = new XElement("Settings");
        if (settings != null)
        {
            foreach (var kvp in settings)
            {
                root.Add(new XElement("Setting",
                    new XElement("Key", kvp.Key),
                    new XElement("Value", kvp.Value)));
            }
        }
        return root;
    }

    private IEnumerable<KeyValuePair<string, string>> DeserializeSettings(XElement element)
    {
        var settingsEl = element?.Element("Settings");
        if (settingsEl == null || !settingsEl.HasElements)
            return Enumerable.Empty<KeyValuePair<string, string>>();

        return settingsEl.Elements("Setting").Select(el =>
            new KeyValuePair<string, string>(
                el.Element("Key").ValueOrDefault(string.Empty),
                el.Element("Value").ValueOrDefault(string.Empty)))
            .ToList();
    }
}
