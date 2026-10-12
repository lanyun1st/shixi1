param([Parameter(Mandatory=$true)][string]$Destination,[switch]$Zip)
$ErrorActionPreference='Stop'
$source=(Resolve-Path -LiteralPath (Split-Path -Parent $PSScriptRoot)).Path
$target=[IO.Path]::GetFullPath($Destination)
if($target.TrimEnd('\','/') -eq $source -or $target.StartsWith($source+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'Choose a new destination outside this project.'}
if(Test-Path -LiteralPath $target){throw 'Destination already exists. Choose a fresh path; nothing is overwritten.'}
if($Zip -and (Test-Path -LiteralPath ($target+'.zip'))){throw 'ZIP already exists. Choose a fresh path.'}
& (Join-Path $PSScriptRoot 'Test-Portability.ps1') -ProjectRoot $source
New-Item -ItemType Directory -Path $target | Out-Null
foreach($name in @('Assets','Packages','ProjectSettings','Docs','Tools','Run-Tests.ps1','PORTABLE-START.txt','ArchitectureNotes.txt')){
    $path=Join-Path $source $name
    if(Test-Path -LiteralPath $path){Copy-Item -LiteralPath $path -Destination (Join-Path $target $name) -Recurse}
}
$manifest=@()
foreach($file in Get-ChildItem -LiteralPath $target -File -Recurse){
    $manifest+=[pscustomobject]@{Path=$file.FullName.Substring($target.Length+1).Replace('\','/');SHA256=(Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash;Bytes=$file.Length}
}
$manifest | Export-Csv -LiteralPath (Join-Path $target 'TransferManifest.csv') -NoTypeInformation -Encoding UTF8
& (Join-Path $target 'Tools/Test-Portability.ps1') -ProjectRoot $target
if($Zip){Compress-Archive -LiteralPath $target -DestinationPath ($target+'.zip') -CompressionLevel Optimal;Write-Output ('ZIP: '+$target+'.zip')}
Write-Output ('PORTABLE_PROJECT_READY: '+$target)
