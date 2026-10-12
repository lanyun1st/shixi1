param([string]$ProjectRoot=(Split-Path -Parent $PSScriptRoot))
$ErrorActionPreference='Stop'
$ProjectRoot=(Resolve-Path -LiteralPath $ProjectRoot).Path
foreach($required in @('Assets/Scenes/Ruins.unity','Assets/Resources/UI/PortfolioCanvas.prefab','Assets/Resources/CombatSettings.asset','Assets/Resources/AttackTimeline.asset','Assets/Resources/Fonts/SourceHanSansCN-Regular.otf','Assets/Resources/Fonts/SourceHanSansCN-Bold.otf','Packages/manifest.json','Packages/packages-lock.json','ProjectSettings/ProjectVersion.txt')){
    if(!(Test-Path -LiteralPath (Join-Path $ProjectRoot $required))){throw "Missing required file: $required"}
}
$files=@();$pending=New-Object 'System.Collections.Generic.Stack[string]'
$pending.Push((Join-Path $ProjectRoot 'Assets'))
while($pending.Count -gt 0){
    foreach($item in Get-ChildItem -LiteralPath $pending.Pop() -Force){
        if($item.Attributes -band [IO.FileAttributes]::ReparsePoint){throw "External/reparse asset is not portable: $($item.FullName)"}
        if($item.PSIsContainer){$pending.Push($item.FullName)}else{$files+=$item}
    }
}
$guids=@{}
foreach($file in $files){
    if($file.Extension -ne '.meta'){
        if(!(Test-Path -LiteralPath ($file.FullName+'.meta'))){throw "Missing .meta: $($file.FullName)"}
        if($file.Extension -eq '.cs'){
            $content=Get-Content -LiteralPath $file.FullName -Raw
            if($content -match '(?<![A-Za-z])[A-Za-z]:[\\/]|/Users/|/home/'){throw "Machine-specific source path: $($file.FullName)"}
        }
    }else{
        $match=[regex]::Match((Get-Content -LiteralPath $file.FullName -Raw),'(?m)^guid: ([a-f0-9]{32})')
        if($match.Success){$id=$match.Groups[1].Value;if($guids.ContainsKey($id)){throw "Duplicate GUID: $($file.FullName) and $($guids[$id])"};$guids[$id]=$file.FullName}
    }
}
$manifest=Get-Content -LiteralPath (Join-Path $ProjectRoot 'Packages/manifest.json') -Raw | ConvertFrom-Json
foreach($dependency in $manifest.dependencies.PSObject.Properties){if([string]$dependency.Value -match '^file:|^[A-Za-z]:'){throw "Local package dependency: $($dependency.Name)"}}
$skills=@(Get-ChildItem -LiteralPath (Join-Path $ProjectRoot 'Assets/Resources/Skills') -Filter '*.asset')
if($skills.Count -ne 18){throw "Expected 18 skill definitions, found $($skills.Count)"}
$scene=Get-Content -LiteralPath (Join-Path $ProjectRoot 'ProjectSettings/EditorBuildSettings.asset') -Raw
if($scene -notmatch 'Assets/Scenes/Ruins.unity'){throw 'Playable scene is not in build settings'}
Write-Output "PORTABILITY_STATIC_OK: $($files.Count) asset files, $($guids.Count) GUIDs, 18 skills; required files and package paths verified."
Write-Output 'Scope: static project checks only. Still requires a licensed matching Unity Editor and target-device validation.'
