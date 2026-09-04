Add-Type -TypeDefinition (Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot 'Calculator.cs'))
if ([Calculator]::Add(2, 3) -ne 5 -or [Calculator]::Add(-1, 1) -ne 0) { throw 'Add returned an incorrect sum' }
Write-Output 'Fixture check passed (positive and mixed-sign cases)'
