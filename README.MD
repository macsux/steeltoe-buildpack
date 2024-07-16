This project offers simplified way of creating buildpacks for Cloud Foundry in .NET. It implements V2 buildpack spec.

## Quick Start

This is a template project that can be used by DotNet CLI to boostrap a new buildpack. Install it into CLI via

```shell
dotnet new -i CloudFoundry.Buildpack.V2
```

Then create a new project from the template:

```
dotnet new buildpack --name MyFancyBuildpack --BuildpackType <supply|final|httpmodule|hostedservice>
```

There are 4 types of buildpacks template supports:

- `supply` - used in multi-buildpack scenarios. Contributes to container creation but doesn't control how app starts. Must be explicitly specified when staging or in manifest. Does not participate in detection. The last buildpack in manifest must be of type "final" such (such as one of the core buildpacks that ship with Cloud Foundry)
- `final` - Last buildpack (or only buildpack) that contributes dependencies and also controls the start command of the app. Must implement detect method and set launch command. Only one final buildpack can be selected during staging
- `httpmodule` - Specialized buildpack for Windows .NET Framework ASP.NET apps that allows installing .NET code that runs inside application process. This is installed as `httpmodule` and can modify the behavior of the running application.
- `hostedservice` - Specialized buildpack for ASP.NET Core apps that allows installing .NET code that runs inside application process. It allows full access to application's `IWebHostBuilder` allowing buildpack to contribute middleware and service container dependencies to ASP.NET Core apps.

#### How to implement

Start with `MyBuilpackBuildpack` class in `SteeltoeBuildpack` project. The stub methods should already be there and simple examples on how to use. 

## How to package

Included build packaging is automated using Nuke.Build project. Either use the included build shell scripts in root folder with name of target you're your trying to invoke. Get full list of useful targets and parameters with `--help` argument for the build script. 

### Usage on Windows

```powershell
.\build.ps1 <TARGET_NAME> (--arg1 VALUE --arg2 VALUE)
```

### Compiling on Linux or Mac

```bash
./build.sh <TARGET_NAME> (--arg1 VALUE --arg2 VALUE)
```

The following targets are provided out of the box:

- `Publish` - compiles the buildpack and places version stamped zip file in `/artifacts` folder. Additionally generates a copy of the buildpack in `/artifacts/latest/<STACK>/buildpack.zip` folder that allows easy reference in tests (filename doesn't change as version increments). By default the buildpack will compile for both Linux and Windows based stacks. You can control it via `stack` argument or hardcoded it to just one inside `Build.cs`.
- `ReleaseGithub` - if remote repo is set, allows compiled buildpack to be published as Github release. This simplifies testing in Cloud Foundry as the URL to the buildpack zip file can just be used directly inside the manifest. You must provided GitHub API Key as `git-hub-token` parameter (or set via environmental variable `GitHubToken`).
- `MakeOffline` - packages a thin buildpack that would normally download additional dependencies during staging into "offline" one that bundles all the dependencies with it. This is useful for installing buildpacks into disconnected environments and speeds up local testing. The buildpack is specified via `BuildpackLocation` argument which can be a local file or uri.

## Testing

The template includes ability to simulate staging and running of the droplet locally inside docker using TestContainers framework. This simplifies testing by allowing validation of buildpack's behavior locally without having to rely on integration tests inside TAS. By combining both the staging piece and the run piece, you can simulate full `cf push` lifecycle and examine the behavior of test applications after buildpacks were applied. The generated tests as part of the template should provide a pattern on how to use. For each test run the test projects `bin` folder will have a unique folder created with droplet output you can examine.

## How to use

* Option 1: Upload to Cloud Foundry via `cf create-buildpack` option and reference in manifest by name
* Option 2: Upload to a public host (like GitHub releases page) and reference in manifest via URL

#### Sample manifest


```yaml
applications:
- name: simpleapp
  stack: windows2016
  buildpacks: 
    - https://github.com/macsux/web-config-transform-buildpack/releases/download/1.0/web-config-transform-buildpack.zip
    - hwc_buildpack
```

## FAQ

Q: Where are test results stored?
A: Each test run is created in tests projects `/bin/Debug/net8.0` folder

Q: I can't delete `tests/<SteeltoeBuildpack>/bin` folder
A: The droplet gets extracted from tarball using Linux permissions and any symlinks, which may prevent normal delete command from working on Windows. To get around this, execute `sudo rm -r ./bin` from WSL
