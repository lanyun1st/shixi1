param(
    [ValidateSet('Rules','Gameplay')][string]$Suite='Rules',
    [string]$UnityPath=$env:UNITY_EDITOR_PATH,
    [switch]$ValidateOnly
)
$ErrorActionPreference='Stop'
if ([string]::IsNullOrWhiteSpace($UnityPath) -or !(Test-Path -LiteralPath $UnityPath -PathType Leaf)) { throw 'Set -UnityPath to your Unity executable, or set UNITY_EDITOR_PATH. Required editor version is in ProjectSettings/ProjectVersion.txt.' }
$method=if($Suite -eq 'Rules'){'PortfolioRuleTests.Batch'}else{'PortfolioSmokeTest.Run'}
$log=Join-Path $PSScriptRoot ('test-'+$Suite+'-'+[guid]::NewGuid().ToString('N')+'.log')
Write-Host 'Close this project in Unity before batch tests; other projects can remain open.'
# UGUI raycasts require a graphics device; pure rule tests remain headless.
$graphics=if($Suite -eq 'Rules'){' -nographics'}else{''}
$arguments='-batchmode'+$graphics+' -projectPath "'+$PSScriptRoot+'" -executeMethod '+$method+' -logFile "'+$log+'"'
if($ValidateOnly){Write-Host "Validated command: $UnityPath $arguments";return}
$process=Start-Process -FilePath $UnityPath -ArgumentList $arguments -WorkingDirectory $PSScriptRoot -WindowStyle Hidden -Wait -PassThru
$marker=if($Suite -eq 'Rules'){'RULE_TESTS_RESULT: passed=True, demonstration_detected=True'}else{'PORTFOLIO_SMOKE_OK:'}
$success=(Test-Path -LiteralPath $log) -and (Select-String -LiteralPath $log -SimpleMatch $marker -Quiet)
if($process.ExitCode -ne 0 -or !$success){Write-Error "Tests failed or did not finish. Read $log";exit 1}
Write-Host "PASS: $Suite. Log: $log"
