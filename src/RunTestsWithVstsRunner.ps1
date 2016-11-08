Param
(
    [String] $testRunnerLocation = "C:\Program Files (x86)\Microsoft Visual Studio 14.0\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe",
    [String] $testAssembly = ".\Jarvis.DocumentStore.Tests\bin\Debug\Jarvis.DocumentStore.Tests.dll"
)
Remove-Item ".\TestResults" -Recurse -ErrorAction SilentlyContinue
$finfo =  ([System.IO.FileInfo]$testAssembly)
Remove-Item -Path "$($finfo.Directory.FullName)\TestResults" -Recurse -ErrorAction SilentlyContinue
& $testRunnerLocation $testAssembly /Logger:trx /Enablecodecoverage /UseVsixExtensions:true