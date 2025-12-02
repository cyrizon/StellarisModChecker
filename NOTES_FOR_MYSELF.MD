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
├── StellarisModChecker-0.1.0-win-Setup.exe (pour nouveaux utilisateurs)
├── StellarisModChecker-0.1.0-win-full.nupkg
└── RELEASES

GitHub Release v0.1.1
├── StellarisModChecker-0.1.1-win-Setup.exe
├── StellarisModChecker-0.1.1-win-delta.nupkg (seulement les changements)
├── StellarisModChecker-0.1.1-win-full.nupkg
└── RELEASES (mis à jour avec les 2 versions)
```