# AGENTS.md

## Branches

- Commit changes to the Rhino version branch they belong to: `rhino-x.x` (e.g. `rhino-9.x`).
- For the current version (Rhino 9), also merge `rhino-9.x` into `ein/local-dev`:

```
git switch ein/local-dev
git merge --no-edit rhino-9.x
git switch rhino-9.x
```

## Running tests

Run from the repository root:

```
dotnet test src/RhinoCodeTests.sln /tl:off -c Debug -v:normal -f net9.0-windows --filter "Name!~TestClient"
```
