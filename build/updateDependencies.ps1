$originalLocation = Get-Location
$projectRoot = "$PSScriptRoot/../"

try {
  Set-Location -Path $projectRoot
  . ./build/buildSettings.ps1

  dotnet tool update --all
  dotnet outdated -u

  if ($LASTEXITCODE -ne 0) {
    Write-Warning "You need to update the Directory.Packages.props file by hand to update the following packages."
    dotnet outdated
  }

} finally {
  Set-Location -Path $originalLocation
}
