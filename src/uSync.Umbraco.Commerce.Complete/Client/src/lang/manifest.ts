const localizations: UmbExtensionManifest = {
  type: "localization",
  alias: "uSync.Umbraco.Commerce.Complete.Localization.En",
  name: "uSync Commerce Complete English Localization",
  weight: 100,
  meta: {
    culture: "en",
  },
  js: () => import("./en.js"),
};

export const manifests: Array<UmbExtensionManifest> = [localizations];
