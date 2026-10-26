[CmdletBinding()]
param([int]$Port=5188)
$ErrorActionPreference='Stop'
$projectRoot=Split-Path -Parent $PSScriptRoot
$qaDir=Join-Path ([System.IO.Path]::GetTempPath()) ('PetJourney-QA-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $qaDir | Out-Null
$database=Join-Path $qaDir 'qa.db'
Write-Host "Base descartável de QA: $database"
Write-Host "API: http://127.0.0.1:$Port"
dotnet run --project (Join-Path $projectRoot 'api/src/PetJourneyTutorApi/PetJourneyTutorApi.csproj') --no-launch-profile -- --environment Development --urls "http://127.0.0.1:$Port" --Database:Provider Sqlite --Demo:Enabled true --ConnectionStrings:Sqlite "Data Source=$database"
if($LASTEXITCODE -ne 0){exit $LASTEXITCODE}
