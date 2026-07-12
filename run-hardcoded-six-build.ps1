$ErrorActionPreference = "Stop"

$unity = "C:\Program Files\Unity\6000.3.14f1\Editor\Unity.exe"
$project = (Get-Location).Path
$apk = Join-Path $project "Builds\Android\AguinaldoShrineARTour.apk"
$log = Join-Path $project "build-play-video-hardcoded-six-items.log"

if (Test-Path $log) {
    Remove-Item -LiteralPath $log -Force
}

$arguments = "-quit -projectPath `"$project`" -executeMethod CommandLineAndroidBuild.BuildAndroidApk -customBuildPath `"$apk`" -logFile `"$log`""
$process = Start-Process -FilePath $unity -ArgumentList $arguments -WindowStyle Hidden -PassThru -Wait

[pscustomobject]@{
    Id = $process.Id
    HasExited = $process.HasExited
    ExitCode = $process.ExitCode
    LogExists = Test-Path $log
    Log = $log
    Apk = $apk
}
