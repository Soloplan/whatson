# MSIX packaging and updates

`WhatsON.wapproj` is a Windows Application Packaging Project.
It packages whatsON and its built-in plugins for x64, including the .NET 10
runtime. Users do not need to install the Desktop Runtime separately.

## Build

Use Visual Studio Developer PowerShell with the MSIX Packaging Tools workload,
Windows SDK 10.0.26100, and the .NET 10 SDK. From the repository root:

```powershell
msbuild src/Soloplan.WhatsON.Package/WhatsON.wapproj -restore -p:Configuration=Release -p:Platform=x64 -p:RuntimeIdentifier=win-x64 -p:GenerateAppxPackageOnBuild=true
```

The runtime argument is needed during restore as well as publish. The packaging
project is listed as a build file in the main solution so ordinary `dotnet build`
and `dotnet test` commands do not require Visual Studio's packaging targets.
Open the `.wapproj` separately to use Visual Studio's package designer.

The build stamps a copy of `Package.appxmanifest`, leaving the source unchanged.
The MSIX version is the numeric SDK version with four components; for example,
`1.2610.42-alpha` becomes `1.2610.42.0`. A prerelease suffix alone cannot
distinguish MSIX updates: every update must increase the numeric version.

## Channels

Jenkins sets `WhatsONChannel` from `TAG_NAME` and `BRANCH_NAME`: develop maps to
alpha, master to beta, and release tags to stable. A local build or feature
branch defaults to alpha. Use `-p:WhatsONChannel=stable`, `beta`, or `alpha`
to override it for local packaging.

| Build | Package identity | Update feed |
| --- | --- | --- |
| Release tag | `Soloplan.WhatsON` | `https://github.com/Soloplan/whatson/releases/latest/download/whatsON.appinstaller` |
| master (beta) | `Soloplan.WhatsON.beta` | `https://github.com/Soloplan/whatson/releases/download/beta/whatsON.appinstaller` |
| develop (alpha) | `Soloplan.WhatsON.alpha` | `https://github.com/Soloplan/whatson/releases/download/alpha/whatsON.appinstaller` |

These are separate Windows applications and can be installed side by side.
Each feed checks for updates on application launch. `AppInstallerUri` can
override the feed's base URL, and `WhatsONPackageUri` the package's base URL;
both must be HTTPS URLs ending in `/`.

## Signing in Jenkins

The Package stage builds the MSIX using the Windows SDK's package validation.
Builds for master, develop, and release tags use AzureSignTool 6.0.1 with these
existing Jenkins credentials:

- `SOLOPLAN_EV_CODE_SIGNING_CERTIFICATE_NAME`
- `AZURE_SERVICE_PRINCIPAL_SECRET`

The manifest publisher matches the company signing certificate. If that
certificate's subject changes, review the publisher identity before shipping;
Windows update identity must remain compatible. Signing failures fail the build.
Local and feature-branch packages are unsigned and cannot be installed normally
without signing them with a certificate trusted on the target machine.

Jenkins archives the `.msix` and `whatsON.appinstaller` as build artifacts.
It does not upload anything to GitHub. The existing portable publish output
and its artifact publishing remain available.

## Publish manually on GitHub

1. Download the signed `.msix` and `whatsON.appinstaller` from a successful
   Jenkins build. Keep both filenames unchanged.
2. For stable, create a GitHub release for the same version tag that Jenkins
   built, upload both files as flat release assets, and mark it latest only
   when both uploads are complete. Its feed points at that exact tag's MSIX.
3. For beta or alpha, maintain a prerelease whose tag is exactly `beta`
   or `alpha`. Upload the new versioned MSIX first, then replace that release's
   `whatsON.appinstaller`. Keep older MSIX assets for clients finishing an update.
   Exclude these two hosting tags from Jenkins tag discovery; they are channel
   containers, not version tags and are not valid SDK version strings.
4. Upload assets using these content types. The GitHub release asset API accepts
   the type through its `Content-Type` header. Check the final download responses
   include the expected type and `Content-Length`, including after redirects.

   | Asset | Content-Type |
   | --- | --- |
   | `.msix` | `application/msix` |
   | `.appinstaller` | `application/appinstaller` |

5. Users download and open `whatsON.appinstaller` to install and register the
   update feed. Installing the bare `.msix` does not register the update feed.
6. Test installing one signed version and updating to the next on Windows before
   advertising a channel. A running tray application may need to exit before
   Windows can apply its update.

All download URLs must be accessible without GitHub authentication. Public
certificate metadata is in the manifest; private signing keys stay in Azure.
Packaged installation folders are read-only, so external plugins should use the
portable distribution. Verify Windows startup behavior on an installed package;
the current application setting uses the registry Run key, which MSIX isolates.

References: [App Installer files](https://learn.microsoft.com/en-us/windows/msix/app-installer/how-to-create-appinstaller-file),
[hosting requirements](https://learn.microsoft.com/en-us/windows/msix/app-installer/troubleshoot-appinstaller-issues),
[GitHub release asset uploads](https://docs.github.com/en/rest/releases/assets#upload-a-release-asset).
