$ErrorActionPreference='Stop'
Push-Location (Join-Path $PSScriptRoot '..')
try { node scripts/executar.cjs; $result=$LASTEXITCODE } finally { Pop-Location }
exit $result
