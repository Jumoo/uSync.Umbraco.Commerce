// src/menus/manifest.ts
var e = [
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
	"uc:gift-card"
], t = [{
	type: "entityAction",
	kind: "publisher-push",
	alias: "usync.commerce.push.action",
	name: "Push Commerce item",
	forEntityTypes: e
}, {
	type: "entityAction",
	kind: "publisher-pull",
	alias: "usync.commerce.pull.action",
	name: "Pull Commerce item",
	forEntityTypes: e
}], n = [{
	type: "localization",
	alias: "uSync.Umbraco.Commerce.Complete.Localization.En",
	name: "uSync Commerce Complete English Localization",
	weight: 100,
	meta: { culture: "en" },
	js: () => import("./en-GHTwno-2.js")
}], r = [...t, ...n];
export { r as manifests };

//# sourceMappingURL=usync-commerce-complete.js.map