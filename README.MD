This buildpack automatically injects Steeltoe components into compatible apps. Currently only actuators are being injected

## How to use

Push your app (.net 8 only supported for now) with manifest


```yaml
applications:
- name: simpleapp
  buildpacks: 
    - https://github.com/macsux/steeltoe-buildpack/releases/download/v0.0.1/SteeltoeBuildpack-linux-x64-0.0.1.zip
    - dotnet_core_buildpack
```

