param([string]$EditorData)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
if (-not $EditorData) {
    $versionLine = Get-Content (Join-Path $projectRoot 'ProjectSettings/ProjectVersion.txt') | Select-Object -First 1
    $version = $versionLine.Split(':')[1].Trim()
    $EditorData = Join-Path $env:ProgramFiles "Unity/Hub/Editor/$version/Editor/Data"
}
$dotnetRoot = Split-Path (Get-Command dotnet).Source -Parent
$sdk = Get-ChildItem (Join-Path $dotnetRoot 'sdk') -Directory | Sort-Object { [version]$_.Name } | Select-Object -Last 1
$compiler = Join-Path $sdk.FullName 'Roslyn/bincore/csc.dll'
$pack = Get-ChildItem (Join-Path $dotnetRoot 'packs/Microsoft.NETCore.App.Ref') -Directory | Sort-Object { [version]$_.Name } | Select-Object -Last 1
$tfm = 'net' + ([version]$pack.Name).Major + '.0'
$output = Join-Path $projectRoot 'Temp/CombatManagerVerification'
New-Item -ItemType Directory -Path $output -Force | Out-Null
$source = Join-Path $projectRoot 'Assets/Script/CombatSystem/CombatManager.cs'
$unitSources = @('CombatUnit.cs', 'PlayerUnit.cs', 'EnemyUnit.cs') | ForEach-Object {
    '"' + (Join-Path $projectRoot "Assets/Script/CombatSystem/$_") + '"'
}
$unityReferences = @(
    (Join-Path $EditorData 'Managed/UnityEngine/UnityEngine.CoreModule.dll'),
    (Join-Path $EditorData 'Managed/UnityEngine/UnityEngine.UIModule.dll'),
    (Join-Path $EditorData 'Managed/UnityEngine/UnityEngine.Physics2DModule.dll'),
    (Join-Path $projectRoot 'Library/ScriptAssemblies/Unity.InputSystem.dll'),
    (Join-Path $projectRoot 'Library/ScriptAssemblies/UnityEngine.UI.dll'),
    (Join-Path $projectRoot 'Library/ScriptAssemblies/Unity.TextMeshPro.dll')
)
foreach ($reference in $unityReferences) {
    if (-not (Test-Path -LiteralPath $reference)) { throw "Missing Unity reference: $reference" }
}
$engineArgs = $unityReferences | ForEach-Object { '-r:"' + $_ + '"' }
$unityProfile = Get-ChildItem (Join-Path $EditorData 'NetStandard/ref') -Filter '*.dll' -Recurse | ForEach-Object { '-r:"' + $_.FullName + '"' }
$compileResponse = Join-Path $output 'compile.rsp'
@('-nologo', '-target:library', '-nostdlib+', ('-out:"' + $output + '/CombatManager.dll"')) + $unityProfile + $engineArgs + ('"' + $source + '"') + $unitSources | Set-Content $compileResponse
& dotnet $compiler "@$compileResponse"
if ($LASTEXITCODE -ne 0) { throw 'Unity API compilation failed.' }
Write-Output 'PASS: Compiled against Unity reference assemblies.'

$netReferences = Get-ChildItem (Join-Path $pack.FullName "ref/$tfm") -Filter '*.dll' | ForEach-Object { '-r:"' + $_.FullName + '"' }
@('-nologo', '-target:exe', '-nostdlib+', ('-out:"' + $output + '/CombatManagerChecks.dll"')) + $netReferences + $engineArgs + ('"' + $source + '"') + ('"' + (Join-Path $PSScriptRoot 'CombatManagerChecks.cs') + '"') | Set-Content $compileResponse
& dotnet $compiler "@$compileResponse"
if ($LASTEXITCODE -ne 0) { throw 'Rule check compilation failed.' }
foreach ($reference in $unityReferences) { Copy-Item -LiteralPath $reference -Destination $output -Force }
@{ runtimeOptions = @{ tfm = $tfm; framework = @{ name = 'Microsoft.NETCore.App'; version = $pack.Name }; rollForward = 'LatestPatch' } } |
    ConvertTo-Json -Depth 5 | Set-Content (Join-Path $output 'CombatManagerChecks.runtimeconfig.json')
& dotnet (Join-Path $output 'CombatManagerChecks.dll')
if ($LASTEXITCODE -ne 0) { throw 'Combat rule checks failed.' }
