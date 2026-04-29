using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using System.Collections.Generic;
using System.Linq;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Commerce.Extensions;

using uSync.BackOffice;
using uSync.BackOffice.Configuration;
using uSync.Umbraco.Commerce.Configuration;

namespace uSync.Umbraco.Commerce
{
    [ComposeBefore(typeof(uSyncBackOfficeComposer))]
    public class CommerceSyncComposer : IComposer
    {
        public void Compose(IUmbracoBuilder builder)
        {
            builder.Services.AddOptions<CommerceSyncSettings>()
                .Bind(builder.Config.GetSection("Commerce.uSync"));
            builder.Services.AddSingleton<CommerceSyncSettingsAccessor>();

            // No need to register serializers in v9 as they
            // are auto discovered however we do need to ensure
            // that Commerce has been initialized so we'll call AddCommerce
            // which should auto escape if it's already been added
            builder.AddUmbracoCommerce();
    
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
}
