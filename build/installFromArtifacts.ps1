$repoRoot = Split-Path -Parent $PSScriptRoot

# Builds from branches other than main are pre-release versions, so always allow pre-release.
dotnet tool install -g vt-loadtest --add-source "$repoRoot\artifacts\dist\" --prerelease
