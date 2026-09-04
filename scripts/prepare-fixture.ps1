param([Parameter(Mandatory)][ValidatePattern('^[a-zA-Z0-9_-]+$')][string]$Name)
$repo = Split-Path $PSScriptRoot -Parent
$target = Join-Path $repo ".cteam/$Name"
if (Test-Path -LiteralPath $target) { throw "Fixture already exists; choose another name: $target" }
New-Item -ItemType Directory -Path $target | Out-Null
Copy-Item -Path (Join-Path $repo 'fixtures/telemetry/*') -Destination $target
New-Item -ItemType Directory -Path (Join-Path $target '.codex/agents') -Force | Out-Null
Copy-Item -Path (Join-Path $repo '.codex/agents/*.toml') -Destination (Join-Path $target '.codex/agents')
Copy-Item -LiteralPath (Join-Path $repo '.codex/config.toml') -Destination (Join-Path $target '.codex/config.toml')
git -C $target init --quiet
git -C $target add .
git -C $target -c user.name='C-Team Fixture' -c user.email='fixture@example.invalid' commit --quiet -m 'Initialize disposable telemetry fixture'
if ($LASTEXITCODE -ne 0) { throw 'Fixture initialization failed' }
Write-Output $target
