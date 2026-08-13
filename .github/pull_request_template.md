## What does this change?

<!-- A sentence or two on the change and why it is needed. -->

## Notes for the reviewer

<!-- Anything non-obvious: behaviour changes, things you decided against, areas you want a
     second opinion on. Delete if there is nothing to say. -->

## Checklist

- [ ] `dotnet build src/uSync.Umbraco.Commerce/uSync.Umbraco.Commerce.csproj -c Release` is clean
- [ ] A new handler's import priority in `CommerceConstants.Priorites` reflects its real
      dependencies, and it implements `ISyncPostImportHandler` if it can reference something
      that isn't guaranteed to exist on the first import pass
- [ ] `CHANGELOG.md` updated under **Unreleased**
- [ ] If a dependency changed, `dotnet restore --force-evaluate --source https://api.nuget.org/v3/index.json`
      was run and the updated `packages.lock.json` is committed
- [ ] Behavioural changes were tested against a running Umbraco + Umbraco Commerce site, not
      just built
