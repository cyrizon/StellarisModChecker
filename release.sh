#!/bin/bash
set -e

# Vérification du paramètre de version
VERSION=$1
if [ -z "$VERSION" ]; then
    echo "Usage: ./release.sh <VERSION>  (ex: ./release.sh 0.2.1)"
    exit 1
fi

echo "Génération de la release v$VERSION pour Windows & Linux..."

# 1. Dossier de sortie
mkdir -p ./Releases

# 2. Publish (.NET Self-Contained)
echo "Compilation des binaires..."
dotnet publish -c Release -r win-x64 --self-contained
dotnet publish -c Release -r linux-x64 --self-contained

# 3. Velopack Packaging
echo "Packaging Velopack..."

# WINDOWS
echo "--> Packaging Windows (win-x64)..."
vpk [win] pack \
    -u StellarisModChecker \
    -v "$VERSION" \
    -r win-x64 \
    -p ./bin/Release/net9.0/win-x64/publish \
    -e StellarisModChecker.exe \
    -o ./Releases

# LINUX
echo "--> Packaging Linux (linux-x64)..."
vpk pack \
    -u StellarisModChecker \
    -v "$VERSION" \
    -r linux-x64 \
    -p ./bin/Release/net9.0/linux-x64/publish \
    -e StellarisModChecker \
    -o ./Releases

echo "Release v$VERSION générée."