Param
(
    [String] $Configuration,
    [String] $DestinationDir = "",
    [Bool] $DeleteOriginalAfterZip = $true
)

$runningDirectory = Split-Path -Parent -Path $MyInvocation.MyCommand.Definition

if ($DestinationDir -eq "") 
{
    $DestinationDir = $runningDirectory + "\release"
}
elseif ($DestinationDir.StartsWith(".")) 
{
     $DestinationDir = $runningDirectory + "\" + $DestinationDir.Substring(1)
}

$DestinationDir = [System.IO.Path]::GetFullPath($DestinationDir)
$DestinationDirHost = $DestinationDir + "\Jarvis.DocumentStore.Host"
$DestinationDirJobs = $DestinationDir + "\Jarvis.DocumentStore.Jobs"

Write-Host "Destination dir is $DestinationDir"

if(Get-Module -name jarvisUtils) 
{
    Remove-Module -Name "jarvisUtils"
}

if(-not(Get-Module -name jarvisUtils)) 
{
    Import-Module -Name ".\jarvisUtils"
}

if(Test-Path -Path $DestinationDir )
{
    Remove-Item $DestinationDir -Recurse
}

$DestinationDir = $DestinationDir.TrimEnd('/', '\')

New-Item -ItemType Directory -Path $DestinationDir
New-Item -ItemType Directory -Path $DestinationDirHost
New-Item -ItemType Directory -Path $DestinationDirJobs

Copy-Item ".\src\Jarvis.DocumentStore.Host\bin\$configuration\*.*" `
    $DestinationDirHost `
    -Force -Recurse

$appDir = $DestinationDirHost.ToString() + "\app"
New-Item -Force -ItemType directory -Path $appDir

Copy-Item ".\src\Jarvis.DocumentStore.Host\app" `
    $DestinationDirHost `
    -Force -Recurse

Write-Host "Destination dir is  $DestinationDirHost"
$configFileName = $DestinationDirHost + "\Jarvis.DocumentStore.Host.exe.config"
#Write-Host "Changing configuration file $configFileName"
#$xml = [xml](Get-Content $configFileName)
#Edit-XmlNodes $xml -xpath "/configuration/appSettings/add[@key='uri']/@value" -value "http://localhost:55555"
#Edit-XmlNodes $xml -xpath "/configuration/appSettings/add[@key='baseConfigDirectory']/@value" -value "..\ConfigurationStore"

#$xml.save($configFileName)

Write-Host "Cleaning up $DestinationDirHost"
Get-ChildItem $DestinationDirHost -Recurse -Include *.xml | foreach ($_) {remove-item $_.fullname}


Write-Host "Copying file for jobs"
Copy-Item ".\artifacts\jobs\" `
    $DestinationDirJobs `
    -Force -Recurse

Write-Host "Cleaning up $DestinationDirJobs"
Get-ChildItem $DestinationDirJobs -Recurse -Include *.xml | foreach ($_) {remove-item $_.fullname}

Write-Host "Compressing everything with 7z"
if (-not (test-path "$env:ProgramFiles\7-Zip\7z.exe")) {throw "$env:ProgramFiles\7-Zip\7z.exe needed"} 
set-alias sz "$env:ProgramFiles\7-Zip\7z.exe"  

$Source = $DestinationDirHost + "\*"
$Target = $DestinationDir + "\Jarvis.DocumentStore.Host.7z"

#sz a -m0=lzma2 -mx=9 -aoa $Target $Source
sz a -mx=5 $Target $Source

$Source = $DestinationDirJobs + "\*"
$Target = $DestinationDir + "\Jarvis.DocumentStore.Jobs.7z"

#sz a -m0=lzma2 -mx=9 -aoa $Target $Source
sz a -mx=5 $Target $Source

if ($DeleteOriginalAfterZip -eq $true) 
{
    Remove-Item $DestinationDirHost  -Recurse -Force
	Remove-Item $DestinationDirJobs  -Recurse -Force
}

