param(
    [switch]$Release
)

$repoRoot = Split-Path -Parent $PSScriptRoot

if ($Release) {
    dotnet tool install -g vt-loadtest --add-source "$repoRoot\artifacts\dist\release\"
} else {
    dotnet tool install -g vt-loadtest --add-source "$repoRoot\artifacts\dist\pre-release\" --prerelease
}
