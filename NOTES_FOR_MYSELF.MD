# ═══════════════════════════════════════════════════
# FOR EACH NEW VERSION
# ═══════════════════════════════════════════════════

# STEP 1 : INCREMENT VERSION IN .csproj
```
<Version>0.1.2</Version>
```

# STEP 2 : Build
```
dotnet publish -c Release -r win-x64 --self-contained
```

# STEP 3 : CREATE PACKAGE
```
vpk pack -u StellarisModChecker -v 1.0.2 -p .\bin\Release\net9.0\win-x64\publish -e StellarisModChecker.exe -o .\Releases
```

# STEP 4 : CREATE RELEASE ON GITHUB AND UPLOAD FILES
```
GitHub Release v0.1.0
├── releases.win.json (INDISPENSABLE - détection des mises à jour)
├── RELEASES (INDISPENSABLE - format legacy)
├── StellarisModChecker-0.1.0-full.nupkg
└── StellarisModChecker-win-Setup.exe (pour nouveaux utilisateurs)

GitHub Release v0.1.1
├── releases.win.json (INDISPENSABLE - mis à jour avec toutes les versions)
├── RELEASES (INDISPENSABLE - mis à jour)
├── StellarisModChecker-0.1.1-delta.nupkg (mise à jour différentielle - plus léger)
├── StellarisModChecker-0.1.1-full.nupkg (package complet)
└── StellarisModChecker-win-Setup.exe
```