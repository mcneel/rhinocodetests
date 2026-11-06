# AGENTS.md

## Running tests

Run from the repository root:

```
dotnet test src/RhinoCodeTests.sln /tl:off -c Debug -v:normal -f net9.0-windows --filter "Name!~TestClient"
```
