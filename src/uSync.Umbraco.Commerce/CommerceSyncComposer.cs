using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using System.Collections.Generic;
using System.Linq;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Commerce.Cms;
using Umbraco.Commerce.Common.Events;
using Umbraco.Commerce.Core;
using Umbraco.Commerce.Core.Events.Notification;
using Umbraco.Commerce.Extensions;

using uSync.BackOffice;
using uSync.BackOffice.Configuration;
using uSync.Umbraco.Commerce.Configuration;
using uSync.Umbraco.Commerce.Handlers;

namespace uSync.Umbraco.Commerce;

[ComposeBefore(typeof(uSyncBackOfficeComposer))]
[ComposeAfter(typeof(UmbracoCommerceComposer))]
public class CommerceSyncComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddOptions<CommerceSyncSettings>()
            .Bind(builder.Config.GetSection("Commerce.uSync"));

        builder.Services.AddSingleton<CommerceSyncSettingsAccessor>();

        // Serializers are auto discovered, but the Commerce event handlers have to be
        // registered on the Commerce builder.
        //
        // AddUmbracoCommerce only runs on its first call - any later call returns early
        // without invoking its config callback. So we can't pass our handlers in as a
        // callback: if a site calls AddUmbracoCommerce in Program.cs (e.g. to AddSQLite)
        // or Commerce's own composer has already run, the handlers would be silently
        // dropped and nothing would export on save.
        //
        // Instead we make sure Commerce is set up (a no-op if it already is) and then
        // register against whichever Commerce builder was created first.
        builder.AddUmbracoCommerce();
        builder.WithUmbracoCommerceBuilder().AdduSyncEventHandlers();

        UdiParser.RegisterUdiType(CommerceConstants.UdiEntityType.Store, UdiType.GuidUdi);
        UdiParser.RegisterUdiType(CommerceConstants.UdiEntityType.OrderStatus, UdiType.GuidUdi);
        UdiParser.RegisterUdiType(CommerceConstants.UdiEntityType.ShippingMethod, UdiType.GuidUdi);
        UdiParser.RegisterUdiType(CommerceConstants.UdiEntityType.Country, UdiType.GuidUdi);
        UdiParser.RegisterUdiType(CommerceConstants.UdiEntityType.Currency, UdiType.GuidUdi);
        UdiParser.RegisterUdiType(CommerceConstants.UdiEntityType.PaymentMethod, UdiType.GuidUdi);
        UdiParser.RegisterUdiType(CommerceConstants.UdiEntityType.TaxClass, UdiType.GuidUdi);
        UdiParser.RegisterUdiType(CommerceConstants.UdiEntityType.EmailTemplate, UdiType.GuidUdi);
        UdiParser.RegisterUdiType(CommerceConstants.UdiEntityType.Store, UdiType.GuidUdi);
        UdiParser.RegisterUdiType(CommerceConstants.UdiEntityType.PrintTemplate, UdiType.GuidUdi);
        UdiParser.RegisterUdiType(CommerceConstants.UdiEntityType.Location, UdiType.GuidUdi);
        UdiParser.RegisterUdiType(CommerceConstants.UdiEntityType.Region, UdiType.GuidUdi);
        UdiParser.RegisterUdiType(CommerceConstants.UdiEntityType.Discount, UdiType.GuidUdi);
        UdiParser.RegisterUdiType(CommerceConstants.UdiEntityType.GiftCard, UdiType.GuidUdi);

        // by default we disable the giftcard and discounts in the default sync - as this is a breaking change
        builder.Services.PostConfigure<uSyncHandlerSetSettings>("Default", options =>
        {
            List<string> disabledHandlers = new();
            
            bool EnableDiscounts = builder.Config.GetValue(CommerceConstants.Settings.SyncDiscounts, false);
            if (EnableDiscounts is false) disabledHandlers.Add("CommerceDiscountHandler");

            bool EnableGifCards = builder.Config.GetValue(CommerceConstants.Settings.SyncGiftCards, false);
            if (EnableGifCards is false) disabledHandlers.Add("CommerceGiftCardHandler");

            options.DisabledHandlers = [.. options.DisabledHandlers.Concat(disabledHandlers).Distinct()];
        });
    }
}

internal static class uSyncCommerceEventHandlerExtensions
{
    public static IUmbracoCommerceBuilder AdduSyncEventHandlers(this IUmbracoCommerceBuilder builder)
    {
        builder
            .AddCommerceNotificationEvent<StoreSavedNotification, StoreHandler>()
            .AddCommerceNotificationEvent<StoreDeletedNotification, StoreHandler>()
            .AddCommerceNotificationEvent<CountrySavedNotification, CountryHandler>()
            .AddCommerceNotificationEvent<CountryDeletedNotification, CountryHandler>()
            .AddCommerceNotificationEvent<CurrencySavedNotification, CurrencyHandler>()
            .AddCommerceNotificationEvent<CurrencyDeletedNotification, CurrencyHandler>()
            .AddCommerceNotificationEvent<OrderStatusSavedNotification, OrderStatusHandler>()
            .AddCommerceNotificationEvent<OrderStatusDeletedNotification, OrderStatusHandler>()
            .AddCommerceNotificationEvent<ShippingMethodSavedNotification, ShippingMethodHandler>()
            .AddCommerceNotificationEvent<ShippingMethodDeletedNotification, ShippingMethodHandler>()
            .AddCommerceNotificationEvent<PaymentMethodSavedNotification, PaymentMethodHandler>()
            .AddCommerceNotificationEvent<PaymentMethodDeletedNotification, PaymentMethodHandler>()
            .AddCommerceNotificationEvent<TaxClassSavedNotification, TaxClassHandler>()
            .AddCommerceNotificationEvent<TaxClassDeletedNotification, TaxClassHandler>()
            .AddCommerceNotificationEvent<EmailTemplateSavedNotification, EmailTemplateHandler>()
            .AddCommerceNotificationEvent<EmailTemplateDeletedNotification, EmailTemplateHandler>()
            .AddCommerceNotificationEvent<PrintTemplateSavedNotification, PrintTemplateHandler>()
            .AddCommerceNotificationEvent<PrintTemplateDeletedNotification, PrintTemplateHandler>()
            .AddCommerceNotificationEvent<LocationSavedNotification, CommerceLocationHandler>()
            .AddCommerceNotificationEvent<LocationDeletedNotification, CommerceLocationHandler>()
            .AddCommerceNotificationEvent<RegionSavedNotification, RegionHandler>()
            .AddCommerceNotificationEvent<RegionDeletedNotification, RegionHandler>()
            ;

        return builder;
    }

    internal static IUmbracoCommerceBuilder AddCommerceNotificationEvent<TNotification, THandler>(this IUmbracoCommerceBuilder builder)
        where TNotification : INotificationEvent
        where THandler : class, ISyncCommerceEventHandler<TNotification>
    {
        builder.WithNotificationEvent<TNotification>()
            .RegisterHandler<THandler>();
        
        return builder;
    }

}
