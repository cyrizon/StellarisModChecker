# ═══════════════════════════════════════════════════
# FOR EACH NEW VERSION
# ═══════════════════════════════════════════════════

# STEP 1 : INCREMENT VERSION IN .csproj
```
<Version>0.1.3</Version>
<AssemblyVersion>0.1.3</AssemblyVersion>
<FileVersion>0.1.3</FileVersion>
```

# STEP 2 : Build

## WINDOWS
```
dotnet publish -c Release -r win-x64 --self-contained
```

## LINUX
```
dotnet publish -c Release -r linux-x64 --self-contained
```

## MACOS (Apple Silicon / M1-M3)
```
dotnet publish -c Release -r osx-arm64 --self-contained
```

# STEP 3 : CREATE PACKAGE

## WINDOWS

```
vpk [win] pack -u StellarisModChecker -v 0.1.2 -r win-x64 -p ./bin/Release/net9.0/win-x64/publish -e StellarisModChecker.exe -o ./Releases
```

## LINUX

```
vpk pack -u StellarisModChecker -v 0.1.3 -p ./bin/Release/net9.0/linux-x64/publish -e StellarisModChecker -o ./Releases
```

## MACOS

```
vpk [osx] pack -u StellarisModChecker -v 0.1.3 -r osx-arm64 -p ./bin/Release/net9.0/osx-arm64/publish -e StellarisModChecker -o ./Releases
```

# STEP 4 : CREATE RELEASE ON GITHUB AND UPLOAD FILES
```
GitHub Release v0.1.0
├── releases.win.json (INDISPENSABLE - détection des mises à jour)
├── RELEASES (INDISPENSABLE - format legacy)
├── StellarisModChecker-0.1.0-full.nupkg
└── StellarisModChecker-win-Setup.exe (pour nouveaux utilisateurs)

GitHub Release v0.1.3
├── releases.win.json                  (INDISPENSABLE - Auto-update Windows)
├── releases.linux.json                (INDISPENSABLE - Auto-update Linux)
├── releases.osx.json                  (INDISPENSABLE - Auto-update macOS)
├── RELEASES (INDISPENSABLE - mis à jour)
├── StellarisModChecker-0.1.3-delta.nupkg (si version >= 0.1.1)
├── StellarisModChecker-0.1.3-full.nupkg
├── StellarisModChecker-win-Setup.exe   (Installateur Windows)
├── StellarisModChecker-linux-x64.AppImage (Executable Linux)
└── StellarisModChecker-osx-arm64.dmg   (Installateur macOS)
```