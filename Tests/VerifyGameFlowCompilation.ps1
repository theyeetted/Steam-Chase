param(
    [Parameter(Mandatory = $true)][string]$ReferenceProject
)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$referenceRoot = (Resolve-Path -LiteralPath $ReferenceProject).Path
$response = Get-ChildItem -LiteralPath (Join-Path $referenceRoot 'Library/Bee/artifacts') -Filter 'Assembly-CSharp.rsp' -Recurse |
    Sort-Object LastWriteTime -Descending | Select-Object -First 1
if (-not $response) { throw 'The reference project must have an existing Unity compilation response file.' }
$dotnetRoot = Split-Path (Get-Command dotnet).Source -Parent
$sdk = Get-ChildItem (Join-Path $dotnetRoot 'sdk') -Directory | Sort-Object { [version]$_.Name } | Select-Object -Last 1
$compiler = Join-Path $sdk.FullName 'Roslyn/bincore/csc.dll'
$output = Join-Path $projectRoot 'Temp/GameFlowVerification'
New-Item -ItemType Directory -Path $output -Force | Out-Null

# Reuse only reference paths from Unity's last compilation; do not launch the Editor
# or execute its analyzers. Compile this worktree's current gameplay source.
$references = foreach ($line in Get-Content -LiteralPath $response.FullName) {
    if ($line -match '^-r:"(.+)"$') {
        $reference = $Matches[1]
        if (-not [System.IO.Path]::IsPathRooted($reference)) { $reference = Join-Path $referenceRoot $reference }
        if (-not (Test-Path -LiteralPath $reference)) { throw "Missing reference: $reference" }
        if ([System.IO.Path]::GetFileName($reference) -eq 'UnityEngine.CoreModule.dll') {
            $engineDirectory = Split-Path $reference -Parent
        }
        '-r:"' + $reference + '"'
    }
}
$sources = Get-ChildItem -LiteralPath (Join-Path $projectRoot 'Assets/Script') -Filter '*.cs' -Recurse |
    ForEach-Object { '"' + $_.FullName + '"' }
foreach ($configuration in @('EditorNewInput', 'PlayerLegacyInput')) {
    $defines = if ($configuration -eq 'EditorNewInput') { 'UNITY_EDITOR;ENABLE_INPUT_SYSTEM' } else { 'ENABLE_LEGACY_INPUT_MANAGER' }
    $arguments = @('-nologo', '-target:library', '-nostdlib+', '-langversion:9', '-nowarn:0649',
        ('-define:' + $defines), ('-out:"' + (Join-Path $output "$configuration.dll") + '"')) + $references + $sources
    $responsePath = Join-Path $output "$configuration.rsp"
    $arguments | Set-Content -LiteralPath $responsePath
    & dotnet $compiler "@$responsePath"
    if ($LASTEXITCODE -ne 0) { throw "$configuration source compilation failed." }
    Write-Output "PASS: $configuration gameplay source compiled against existing Unity references."
}

# Run the production pause enumerator as managed code, without constructing engine
# objects or starting the Editor. The harness models nested coroutine scheduling.
$pack = Get-ChildItem (Join-Path $dotnetRoot 'packs/Microsoft.NETCore.App.Ref') -Directory |
    Sort-Object { [version]$_.Name } | Select-Object -Last 1
$tfm = 'net' + ([version]$pack.Name).Major + '.0'
$netReferences = Get-ChildItem (Join-Path $pack.FullName "ref/$tfm") -Filter '*.dll' |
    ForEach-Object { '-r:"' + $_.FullName + '"' }
$checksResponse = Join-Path $output 'pause-checks.rsp'
@('-nologo', '-target:exe', '-nostdlib+', ('-out:"' + $output + '/PauseFlowChecks.dll"'),
    ('"' + (Join-Path $PSScriptRoot 'PauseFlowChecks.cs') + '"')) + $netReferences |
    Set-Content -LiteralPath $checksResponse
& dotnet $compiler "@$checksResponse"
if ($LASTEXITCODE -ne 0) { throw 'Pause checks compilation failed.' }
@{ runtimeOptions = @{ tfm = $tfm; framework = @{ name = 'Microsoft.NETCore.App'; version = $pack.Name }; rollForward = 'LatestPatch' } } |
    ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $output 'PauseFlowChecks.runtimeconfig.json')
& dotnet (Join-Path $output 'PauseFlowChecks.dll') (Join-Path $output 'EditorNewInput.dll') `
    $engineDirectory (Join-Path $referenceRoot 'Library/ScriptAssemblies')
if ($LASTEXITCODE -ne 0) { throw 'Pause/resume enumerator checks failed.' }
