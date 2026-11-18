# Changelog

Notable changes to `uSync.Umbraco.Commerce`. Changes before this file existed are only in the
commit history. Like uSync itself, this package ships one release line per Umbraco major, so this
file covers the 19.x line, with 18.x history carried forward from `v18/main`; the 18.x line lives on
[`v18/main`](https://github.com/Jumoo/uSync.Umbraco.Commerce/blob/v18/main/CHANGELOG.md).

## 19.0.0-rc1 - 2026-11-12

Release candidate for Umbraco 19. Includes everything in 18.0.1, and the fixes below that are
still unreleased on the 18.x line.

### Changed

- Targets Umbraco 19 and .NET 11. Built against Umbraco 19.0.0-rc1, Umbraco Commerce 19.0.0-rc.1
  and uSync 19.0.0-rc1. No code changes were needed.

### Added

- `uSync.Umbraco.Commerce.Complete`, a new package that adds uSync.Complete's Push and Pull
  actions to Umbraco Commerce store settings: on a store, and on its locations, order statuses,
  countries, regions, currencies, payment and shipping methods, tax classes, email/print/export
  templates, discounts and gift cards ([#33](https://github.com/Jumoo/uSync.Umbraco.Commerce/issues/33)).
  It includes English names for each Commerce item type in uSync.Publisher's push and pull
  reports, which otherwise show raw keys such as `usyncpublish_CountryReadOnly`.

### Fixed

- The uSync reports showed raw keys for regions, discounts and gift cards, which were missing
  from the package's English localization.
- uSync.Complete couldn't find Umbraco Commerce items by the entity types the Umbraco 17
  backoffice uses (`uc:country` and so on), only by the older `commerce-*` types. Regions and
  locations had no lookup at all, and the discount and gift card lookups never found a single item.
- Countries, regions, currencies, locations, payment and shipping methods, tax classes and
  email/print/export templates had no dependency checker, so uSync.Complete found nothing to
  export when one of them was pushed on its own ("There is nothing to export").
- A store's dependencies listed its shipping methods without their ids, its export templates as
  email templates, and left its locations out altogether, so a store push couldn't include any of
  them correctly.
- The export template UDI type was never registered (the store type was registered twice
  instead), so an export template UDI couldn't be parsed.
- Creating a store only exported one of each of its default settings (one order status, one email
  template and so on), because Umbraco Commerce raises a single saved notification per type when it
  creates them. Saving a store now also exports the settings that belong to it, so a new store gets
  a file for every order status, email, print and export template.
- Saving or deleting an export template didn't update its file: the handler was never registered
  for Umbraco Commerce's export template notifications.
- Export and print templates didn't import their sort order, so on another server they could end
  up in a different order, and every report afterwards showed them as changed.

## 18.0.2 - 2026-11-12

### Fixed

- Shipping method prices were never synced: the export didn't write them, a price change didn't
  change the file, and an import created the method with no prices. Prices are now exported and
  imported, including dynamic and real-time calculation configs. A file exported before this fix
  leaves the existing prices alone, and a file whose calculation mode differs from the method's is
  skipped with a warning, because Umbraco Commerce only sets the mode when the method is created.
  Every shipping method file changes on its first export after upgrading ([#23](https://github.com/Jumoo/uSync.Umbraco.Commerce/issues/23)).
- Nothing was exported on save when the site called `AddUmbracoCommerce` itself in `Program.cs`
  (as SQLite needs, for `AddSQLite()`). `AddUmbracoCommerce` only runs its setup callback on the
  first call, so the package's save and delete handlers were dropped. They are now registered
  against the existing Commerce builder, whichever order the calls run in ([#25](https://github.com/Jumoo/uSync.Umbraco.Commerce/pull/25)).
- An item deleted in Umbraco Commerce couldn't be added back by importing its file: the import
  reported no change and the item stayed hidden. Umbraco Commerce keeps deleted items in the
  database, and uSync treated them as still there. Deleted items are now ignored, and the import
  creates the item again. Because the deleted item keeps its key, the new one gets a new key and is
  matched by alias from then on. A deleted store can't be recreated this way, because other items
  find their store by key, so its import fails with a message saying why ([#24](https://github.com/Jumoo/uSync.Umbraco.Commerce/issues/24)).

## 18.0.1 - 2026-10-14

### Fixed

- The backoffice localization file 404'd on case-sensitive filesystems (Linux/macOS), so the
  Settings tree showed raw keys like `#uSync_section`: `umbraco-package.json` referenced
  `Localization/en.js` but the file ships as `localization/en.js` ([#15](https://github.com/Jumoo/uSync.Umbraco.Commerce/issues/15)).
- Store permissions (allowed users and user roles) were never exported, and an import then cleared
  them on the target. Umbraco Commerce stores these by user / user group key, but the serializer
  looked them up by int id and alias. Export now resolves the keys to usernames and group aliases,
  and import maps them back to keys ([#14](https://github.com/Jumoo/uSync.Umbraco.Commerce/issues/14)).
- A location whose name contained `/` (or another character not allowed in file names) was
  exported into an extra sub-folder rather than as a single file. Those characters are now replaced
  with `-` in the file name ([#9](https://github.com/Jumoo/uSync.Umbraco.Commerce/issues/9)).

## 18.0.0 - 2026-10-13

### Added

- Umbraco / Umbraco Commerce 18 support — `Umbraco.Commerce`, `uSync.Core` and `uSync.BackOffice`
  bumped to 18.1.0. No code changes were needed; the APIs this package uses were unchanged
  between the 17.x and 18.x lines.
- Repository standards: `LICENSE.md`, `README.md`, `CHANGELOG.md`, `SECURITY.md`,
  `CODE_OF_CONDUCT.md`, `.editorconfig`, `.gitattributes`, `global.json`, `Directory.Build.props`,
  `GitVersion.yml`, dependabot, and issue and PR templates.
- CI workflows — PR build, package build, and release. CodeQL is present but disabled by default
  (see the workflow for why).
- Releases publish to NuGet from a `v{version}` tag pushed on a release branch, gated behind the
  `nuget` GitHub environment and authenticated with trusted publishing (OIDC) rather than a
  stored API key.
- `packages.lock.json`, so a transitive dependency update can't change what a build restores
  without a commit.

### Fixed

- Tax class country/region tax rates were never restored on import: the serializer wrote them
  under a `<TaxClasses>` element but read them back looking for `<TaxRates>`, so import always saw
  an empty list and cleared every existing override instead of restoring it ([#10](https://github.com/Jumoo/uSync.Umbraco.Commerce/issues/10)).
- The exported country/region tax rate value itself round-tripped as `0`, because it was serialized
  via `TaxRate.ToString()` (`"20.00%"`) instead of the underlying decimal value, which then failed
  to parse back on import. Tax codes are now round-tripped too.
- `ShippingMethodSerializer` never removed a country/region shipping allowance that had been
  removed from the XML, due to a collection-diff comparing the live list against itself instead of
  against the incoming XML values.
