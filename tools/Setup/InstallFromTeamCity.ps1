param(
    [string] $BranchName = "master",
    [string] $InstallDir = "",
    [string] $teamCityBuildId = "Jarvis_DocumentStore_CI",
    [string] $port = "5123",
    [string] $metricsPort = "55558"
)
#Remove-Module teamCity
#Remove-Module jarvisUtils

Import-Module -Name ".\teamCity"
Import-Module -Name ".\jarvisUtils"


if ($InstallDir -eq '') 
{
    $InstallDir = Split-Path -Parent -Path $MyInvocation.MyCommand.Definition
}


Write-Host "Installing in:$InstallDir"
$user  = Read-Host 'What is your username?' 
$pass = Read-Host 'What is your password?' -AsSecureString
$plainPassword = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($pass))

$lastBuildNumber = Get-LatestBuildNumber -url "demo.prxm.it:8811" -buildId $teamCityBuildId -branch "master" -username $user  -password $plainPassword
$baseBuildUri = Get-LatestBuildUri -url "demo.prxm.it:8811" -buildId $teamCityBuildId -latestBuildId $lastBuildNumber

if ($baseBuildUri -eq $null) 
{
    Write-Host "Error calling team city city server";
    return;
}

Write-Host "Last Build Number is: $lastBuildNumber"

$targetpath = [System.IO.Path]::GetFullPath($InstallDir + "\Jarvis.DocumentStore.Host.build-$lastBuildNumber.zip ") 
$finalInstallDir = [System.IO.Path]::GetFullPath($InstallDir + "\DocumentStoreHost")

$hostUri = "$baseBuildUri/Jarvis.DocumentStore.Host.zip"

if(Test-Path -Path $targetpath)
{
    Write-Host "Target File already downloaded: $targetpath"
}
else
{
    Write-Host "Download Host Url $hostUri"
    Get-Artifact $hostUri $targetpath $user $plainPassword 
}

$targetJobPath = [System.IO.Path]::GetFullPath($InstallDir + "\Jobs.build-$lastBuildNumber.zip ") 
$finalJobInstallDir = [System.IO.Path]::GetFullPath($InstallDir + "\Jobs")

$hostJobUri = "$baseBuildUri/Jobs.zip"

if(Test-Path -Path $targetJobPath)
{
    Write-Host "Target File already downloaded: $targetJobPath"
}
else
{
    Write-Host "Download Host Url $hostJobUri"
    Get-Artifact $hostJobUri $targetJobPath $user $plainPassword 
}

Write-Host 'Stopping Service Jarvis - Document Store'

$service = Get-Service -Name "Jarvis - Document Store" -ErrorAction SilentlyContinue 
if ($service -ne $null) 
{
    Stop-Service "Jarvis - Document Store"
}

Write-Host 'Deleting actual directory'


Write-Host "Unzipping host zip file in $finalInstallDir"

if (Test-Path $finalInstallDir) 
{
    Write-Host "Deleting old folder $finalInstallDir"
    Remove-Item $finalInstallDir -Recurse -Force
}

$shell = new-object -com shell.application
$fullpath = [System.IO.Path]::GetFullPath($targetpath)

New-Item $finalInstallDir -type directory
$zip = $shell.NameSpace($fullpath)
foreach($item in $zip.items())
{
    Write-Host "unzipping " + $item.Name
    $shell.Namespace($finalInstallDir).copyhere($item)
}

Write-Host "Unzipping jobs zip file in $finalJobInstallDir"

if (Test-Path $finalJobInstallDir) 
{
    Write-Host "Deleting old jobs folder $finalJobInstallDir"
    Remove-Item $finalJobInstallDir -Recurse -Force
}

$fullJobsPath = [System.IO.Path]::GetFullPath($targetJobPath)

New-Item $finalJobInstallDir -type directory
$zip = $shell.NameSpace($fullJobsPath)
foreach($item in $zip.items())
{
    Write-Host "unzipping " + $item.Name
    $shell.Namespace($finalJobInstallDir).copyhere($item)
}

if ($service -eq $null) 
{
    Write-Host "Starting the service in $finalInstallDir\Jarvis.DocumentStore.Host.exe"

    & "$finalInstallDir\Jarvis.DocumentStore.Host.exe" install
} 

Write-Host "Changing configuration"

$configFileName = $finalInstallDir + "\Jarvis.DocumentStore.Host.exe.config"
$xml = [xml](Get-Content $configFileName)
 
& netsh http add urlacl url=http://+:$port/ user=Everyone
& netsh http add urlacl url=http://+:$metricsPort/ user=Everyone

Write-Host 'Starting the service'
Start-Service "Jarvis - Document Store"
Write-Host "Jarvis Document Store Installed"

