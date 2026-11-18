// uSync.Publisher labels the items in a push/pull report as `usyncpublish_{type name}`,
// where the type name is the Commerce model the handler works with.
export default {
  usyncpublish: {
    StoreReadOnly: "Store",
    LocationReadOnly: "Location",
    OrderStatusReadOnly: "Order Status",
    CountryReadOnly: "Country",
    RegionReadOnly: "Region",
    CurrencyReadOnly: "Currency",
    TaxClassReadOnly: "Tax Class",
    PaymentMethodReadOnly: "Payment Method",
    ShippingMethodReadOnly: "Shipping Method",
    EmailTemplateReadOnly: "Email Template",
    PrintTemplateReadOnly: "Print Template",
    ExportTemplateReadOnly: "Export Template",
    DiscountReadOnly: "Discount",
    GiftCardReadOnly: "Gift Card",
  },
};
