# Run Compute

- Clone https://github.com/mcneel/compute.rhino3d.git
- Open `src\compute.sln`
- Change `src\compute.geometry\Program.cs` and add your Rhino build folder

```csharp
#if DEBUG
            string rhinoSystemDir = @"C:\your\rhino\build\path";
            if (System.IO.File.Exists(rhinoSystemDir + "\\Rhino.exe"))
                RhinoInside.Resolver.RhinoSystemDirectory = rhinoSystemDir;
#endif

```
- Build solution
- Open Powershell
- Add your rhino build system path to PATH

```shell
$env:PATH = "C:\your\rhino\build\path;" + $env:PATH
```

- Run `bin\Debug\compute.geometry\compute.geometry.exe`

```shell
CG  [09:31:00 INF] Child process started at 22/2/2024 09:31:00
CG  [09:31:00 DBG] Hosting starting
CG  [09:31:02 INF] (1/3) Loading grasshopper
CG  [09:31:05 INF] (2/3) Loading compute plug-ins
CG  [09:31:05 INF] (3/3) Loading rhino scripting plugin
CG  [09:31:05 DBG] Found module compute.geometry.FixedEndPointsModule
CG  [09:31:05 DBG] Found module compute.geometry.RhinoGetModule
CG  [09:31:05 DBG] Found module compute.geometry.RhinoPostModule
CG  [09:31:05 DBG] Found module compute.geometry.ResthopperEndpointsModule
CG  [09:31:05 INF] Now listening on: http://localhost:5000
CG  [09:31:05 INF] Application started. Press Ctrl+C to shut down.
CG  [09:31:05 INF] Hosting environment: Production
CG  [09:31:05 DBG] Hosting started
```

- Open an instance of Rhino
- Open `test_hops_main.ghx` to run all the tests
