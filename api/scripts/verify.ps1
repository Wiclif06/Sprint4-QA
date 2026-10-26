param([ValidateSet('Debug','Release')][string]$Configuration = 'Release')
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
Push-Location $projectRoot
try {
    dotnet restore PetJourney.sln --locked-mode --configfile NuGet.Config
    if ($LASTEXITCODE -ne 0) { throw 'Falha na restauração ou auditoria NuGet.' }
    dotnet build PetJourney.sln --no-restore --configuration $Configuration -warnaserror
    if ($LASTEXITCODE -ne 0) { throw 'Falha na compilação.' }
    dotnet test PetJourney.sln --no-build --configuration $Configuration --settings coverage.runsettings --logger trx --collect 'XPlat Code Coverage' --results-directory TestResults
    if ($LASTEXITCODE -ne 0) { throw 'Falha nos testes.' }
    Write-Host 'Verificação concluída. Confira testes externos ignorados no TRX.'
}
finally { Pop-Location }
