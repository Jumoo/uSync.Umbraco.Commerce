using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Commerce.Core.Models;

using uSync.Core.Dependency;

namespace uSync.Umbraco.Commerce.Dependencies;

/// <summary>
///  dependency checker that returns just the item itself.
/// </summary>
/// <remarks>
///  uSync.Complete exports what a handler's dependency checkers return - a type with no
///  checker exports nothing, so pushing a single country (etc) fails with "nothing to export".
///  Pushing a store pulls all of these in through <see cref="CommerceStoreDependencyChecker"/>.
/// </remarks>
public abstract class CommerceSingleItemDependencyChecker<TObject> : ISyncDependencyChecker<TObject>
    where TObject : class
{
    public UmbracoObjectTypes ObjectType => UmbracoObjectTypes.Unknown;

    protected abstract string EntityType { get; }

    protected abstract int Order { get; }

    protected abstract Guid GetId(TObject item);

    protected abstract string GetName(TObject item);

    public Task<IEnumerable<uSyncDependency>> GetDependenciesAsync(TObject item, DependencyFlags flags)
    {
        if (item is null) return Task.FromResult<IEnumerable<uSyncDependency>>([]);

        return Task.FromResult<IEnumerable<uSyncDependency>>(
        [
            new uSyncDependency
            {
                Name = GetName(item),
                Order = Order,
                Udi = Udi.Create(EntityType, GetId(item)),
            }
        ]);
    }
}

public class CommerceCountryDependencyChecker : CommerceSingleItemDependencyChecker<CountryReadOnly>
{
    protected override string EntityType => CommerceConstants.UdiEntityType.Country;
    protected override int Order => CommerceConstants.Priorites.Country;
    protected override Guid GetId(CountryReadOnly item) => item.Id;
    protected override string GetName(CountryReadOnly item) => item.Name;
}

public class CommerceRegionDependencyChecker : CommerceSingleItemDependencyChecker<RegionReadOnly>
{
    protected override string EntityType => CommerceConstants.UdiEntityType.Region;
    protected override int Order => CommerceConstants.Priorites.Region;
    protected override Guid GetId(RegionReadOnly item) => item.Id;
    protected override string GetName(RegionReadOnly item) => item.Name;
}

public class CommerceCurrencyDependencyChecker : CommerceSingleItemDependencyChecker<CurrencyReadOnly>
{
    protected override string EntityType => CommerceConstants.UdiEntityType.Currency;
    protected override int Order => CommerceConstants.Priorites.Currency;
    protected override Guid GetId(CurrencyReadOnly item) => item.Id;
    protected override string GetName(CurrencyReadOnly item) => item.Name;
}

public class CommerceLocationDependencyChecker : CommerceSingleItemDependencyChecker<LocationReadOnly>
{
    protected override string EntityType => CommerceConstants.UdiEntityType.Location;
    protected override int Order => CommerceConstants.Priorites.Location;
    protected override Guid GetId(LocationReadOnly item) => item.Id;
    protected override string GetName(LocationReadOnly item) => item.Name;
}

public class CommercePaymentMethodDependencyChecker : CommerceSingleItemDependencyChecker<PaymentMethodReadOnly>
{
    protected override string EntityType => CommerceConstants.UdiEntityType.PaymentMethod;
    protected override int Order => CommerceConstants.Priorites.PaymentMethod;
    protected override Guid GetId(PaymentMethodReadOnly item) => item.Id;
    protected override string GetName(PaymentMethodReadOnly item) => item.Name;
}

public class CommerceShippingMethodDependencyChecker : CommerceSingleItemDependencyChecker<ShippingMethodReadOnly>
{
    protected override string EntityType => CommerceConstants.UdiEntityType.ShippingMethod;
    protected override int Order => CommerceConstants.Priorites.ShippingMethod;
    protected override Guid GetId(ShippingMethodReadOnly item) => item.Id;
    protected override string GetName(ShippingMethodReadOnly item) => item.Name;
}

public class CommerceTaxClassDependencyChecker : CommerceSingleItemDependencyChecker<TaxClassReadOnly>
{
    protected override string EntityType => CommerceConstants.UdiEntityType.TaxClass;
    protected override int Order => CommerceConstants.Priorites.TaxClass;
    protected override Guid GetId(TaxClassReadOnly item) => item.Id;
    protected override string GetName(TaxClassReadOnly item) => item.Name;
}

public class CommerceEmailTemplateDependencyChecker : CommerceSingleItemDependencyChecker<EmailTemplateReadOnly>
{
    protected override string EntityType => CommerceConstants.UdiEntityType.EmailTemplate;
    protected override int Order => CommerceConstants.Priorites.EmailTemplate;
    protected override Guid GetId(EmailTemplateReadOnly item) => item.Id;
    protected override string GetName(EmailTemplateReadOnly item) => item.Name;
}

public class CommercePrintTemplateDependencyChecker : CommerceSingleItemDependencyChecker<PrintTemplateReadOnly>
{
    protected override string EntityType => CommerceConstants.UdiEntityType.PrintTemplate;
    protected override int Order => CommerceConstants.Priorites.PrintTemplate;
    protected override Guid GetId(PrintTemplateReadOnly item) => item.Id;
    protected override string GetName(PrintTemplateReadOnly item) => item.Name;
}

public class CommerceExportTemplateDependencyChecker : CommerceSingleItemDependencyChecker<ExportTemplateReadOnly>
{
    protected override string EntityType => CommerceConstants.UdiEntityType.ExportTemplate;
    protected override int Order => CommerceConstants.Priorites.ExportTemplate;
    protected override Guid GetId(ExportTemplateReadOnly item) => item.Id;
    protected override string GetName(ExportTemplateReadOnly item) => item.Name;
}
