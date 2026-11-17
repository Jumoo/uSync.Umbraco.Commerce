# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

`uSync.Umbraco.Commerce` bridges [uSync](https://github.com/KevinJump/uSync) with
[Umbraco Commerce](https://umbraco.com/products/add-ons/commerce/). It serializes store settings
(stores, currencies, countries, regions, tax classes, shipping/payment methods, order statuses,
email/print/export templates, and optionally discounts and gift cards) to disk as uSync config
files, and imports them back in — the same config-as-files model uSync uses for content types.
It is a plugin — it does nothing without Umbraco Commerce, whose types come in via NuGet
(`Umbraco.Commerce`) and are not in this repo.

**Branch per Umbraco major.** This is `v17/main`, the Umbraco 17 / `net10.0` line. The 18.x line
is on `v18/main`. Every past major bump (v13, v16, v17, v18) was a pure package-version bump with
no code changes — this package tracks the upstream uSync/Umbraco/Commerce APIs closely and only
needs porting work when something upstream actually breaks. Check whether the newer branch
already has a fix before writing it here.

## Commands

Two packages: `uSync.Umbraco.Commerce` (the main one) and `uSync.Umbraco.Commerce.Complete`
(uSync.Complete push/pull menus, modelled on `uSync.Forms.Complete`). The main package has no
client build step — its assets under `src/uSync.Umbraco.Commerce/wwwroot/App_Plugins` are static
files. The Complete project's `Client` folder **is** built: its csproj runs `npm ci` and
`npm run build` during `dotnet build`, so Node has to be on PATH. The built bundle in its
`wwwroot` is committed too.

The Complete project references the main one, so building it builds both:

```bash
dotnet build src/uSync.Umbraco.Commerce.Complete/uSync.Umbraco.Commerce.Complete.csproj -c Release
```

```bash
dotnet pack src/uSync.Umbraco.Commerce/uSync.Umbraco.Commerce.csproj -c Release
```

```bash
dotnet pack src/uSync.Umbraco.Commerce.Complete/uSync.Umbraco.Commerce.Complete.csproj -c Release
```

There are no automated tests. Verification is: a clean `dotnet build`, and for anything
behavioural, running the package against an Umbraco + Umbraco Commerce site and syncing a store.

## Repository shape

**`uSync.Umbraco.Commerce.slnx` references `test/Commerce.Site`, which is not in the repository.**
`.gitignore` excludes `test/`. It exists locally as a test setup. Build and pack the **projects**,
never the solution — CI does the same, and pointing tooling at the solution fails on a clean
checkout.

Adding `Directory.Build.props` here stops `D:\Source\directory.build.props` applying, which is
why `NuGetAuditMode` is repeated in it.

Shared package metadata (`VersionPrefix`, `Authors`, `Copyright`, license, repository URL) lives
in `Directory.Build.props` rather than the csproj, so it can't drift if a second project is ever
added to this repo. Don't hardcode a version in the csproj — CI stamps it via
`dotnet pack /p:version=`.

## Import ordering — the thing to get right

Handlers are marked with priorities in `CommerceConstants.Priorites`, all reserved in the
`1150–1199` range so stores build before content syncs at `1200`. The ordering encodes real
dependencies: `Stores` first, then things that only need a store (email/print/export templates,
order statuses), then `Country`/`Region`/`Currency`/`TaxClass`/`Location`, then
`PaymentMethod`/`ShippingMethod`, then `Discount`/`GiftCard` last.

Some of these are circular in practice (a country can reference a payment method and vice versa),
so `StoreHandler`, and the other handlers implementing `ISyncPostImportHandler`, get run a second
time by uSync at the end of the import to pick up references that weren't resolvable on the first
pass. If you add a new handler with cross-references, decide whether it needs
`ISyncPostImportHandler` too rather than just picking a priority number and hoping.

## Things that will catch you out

**Discounts and gift cards are off by default.** `CommerceSyncComposer` disables
`CommerceDiscountHandler` and `CommerceGiftCardHandler` in the `Default` handler set unless
`uSync:Commerce:SyncDiscounts` / `uSync:Commerce:SyncGiftCards` are set to `true` — this was a
deliberate breaking-change guard when the two were added, not an oversight.

**`GiftCardSerializer._giftCardService` is never assigned** (`Serializers/GiftCardSerializer.cs`)
— it's a field with no constructor parameter to fill it, so any code path that hits it (delete,
find-by-key, save) throws a `NullReferenceException`. Known issue, not fixed as part of the repo
tidy-up; look here first if gift card sync misbehaves.

**Store lookups fall back from key to alias.** `CommerceSerializerBase.LookupStoreAsync(XElement)`
tries the store id from the file first, then falls back to `storeAlias` if the id doesn't
resolve — this is what lets synced files survive a store being recreated with a new key on
another environment, as long as the alias matches.

**Push/pull needs both entity type spellings in the item managers.** The Umbraco 17 backoffice
names Commerce items `uc:country`, `uc:store-settings` and so on; uSync's UDIs use
`commerce-country` etc. (a `:` can't go in a UDI). uSync.Complete picks an `ISyncItemManager`
by whichever type it holds — the `uc:` one when a menu is clicked, the `commerce-` one once it
has the UDI — so `OrderSyncManager` / `CommerceStoreManager` list both. Adding a type to the
Complete project's `Client/src/menus/manifest.ts` without adding it to
`CommerceConstants.BackofficeEntityType` and a manager's `EntityTypes` gives a menu that does
nothing.

**Every pushable type needs a dependency checker.** uSync.Complete exports what a handler's
`ISyncDependencyChecker<T>` returns, so a type with no checker fails a push with "There is
nothing to export". Complete also caches those results on disk
(`umbraco/Data/TEMP/uSync/cache/publisher.json`) and only clears them on content/media changes —
delete that file when testing a checker change, or you'll keep seeing the old result.

**uSync.Complete's push condition rejects `uc:` entity types.** It builds
`umb://{entityType}/{unique}` and asks the server for available servers; a `:` makes that an
invalid URI, so the server returns none and the Push/Pull actions never show. That's fixed (or
not) in uSync.Complete, not here.

**`OneWay` / `CreateOnly` settings are handled once, generically**, in
`CommerceSyncHandlerBase.ShouldImportAsync` — individual handlers don't need to implement this
themselves.

**Regenerating `packages.lock.json` needs `--source https://api.nuget.org/v3/index.json`.** This
machine has a `D:\Source\LocalGit` folder feed carrying locally built copies of some packages at
the same version numbers as the released ones. The lock file records a content hash per package,
and nuget.org repository-signs what it serves, so a hash taken from a local build never validates
against nuget.org — CI fails the `--locked-mode` restore with NU1403. If a plain `dotnet restore`
rewrites those hashes, that is what happened; don't commit it.
