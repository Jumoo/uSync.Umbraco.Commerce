// Umbraco Commerce backoffice entity types that get the uSync.Complete push and pull actions.
//
// These need a matching entry in the item managers in uSync.Umbraco.Commerce
// (CommerceConstants.BackofficeEntityType), which is how the server turns the
// entity type + unique the action sends into a commerce UDI it can sync.
//
// The collection nodes (uc:countries, uc:currencies, ...) are left out on purpose: their
// unique is the store id, so a push from them could only ever push the whole store.
// Push the store itself for that.
const commerceEntityTypes = [
  "uc:store-settings",
  "uc:location",
  "uc:order-status",
  "uc:country",
  "uc:region",
  "uc:currency",
  "uc:payment-method",
  "uc:shipping-method",
  "uc:tax-class",
  "uc:email-template",
  "uc:export-template",
  "uc:print-template",
  "uc:discount",
  "uc:gift-card",
];

const actions: Array<UmbExtensionManifest> = [
  {
    type: "entityAction",
    kind: "publisher-push",
    alias: "usync.commerce.push.action",
    name: "Push Commerce item",
    forEntityTypes: commerceEntityTypes,
  },
  {
    type: "entityAction",
    kind: "publisher-pull",
    alias: "usync.commerce.pull.action",
    name: "Pull Commerce item",
    forEntityTypes: commerceEntityTypes,
  },
];

export const manifests: Array<UmbExtensionManifest> = [...actions];
