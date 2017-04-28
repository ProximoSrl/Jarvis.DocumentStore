Param
(
    [String] $Configuration,
    [String] $DestinationDir = "",
    [String] $DeleteOriginalAfterZip = "$true",
    [String] $StandardZipFormat = "$false"
)

Write-Output "Configuration = $Configuration, DestinationDir = $DestinationDir, DeleteOriginalAfterZip = $DeleteOriginalAfterZip, StandardZipFormat = $StandardZipFormat"

$DeleteOriginalAfterZipBool = ($DeleteOriginalAfterZip -eq "$true") -or ($DeleteOriginalAfterZip -eq "true")
$StandardZipFormatBool =  ($StandardZipFormat -eq "$true") -or ($StandardZipFormat -eq "true")

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
$DestinationDirConfigs = $DestinationDir + "\DocumentStoreConfig"

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

Copy-Item ".\src\Jarvis.DocumentStore.Host\bin\$configuration\net461\*.*" `
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
Copy-Item ".\artifacts\jobs\*" `
    $DestinationDirJobs `
    -Force -Recurse

Write-Host "Cleaning up $DestinationDirJobs"
Get-ChildItem $DestinationDirJobs -Recurse -Include *.xml | foreach ($_) {remove-item $_.fullname}

Write-Host "Compressing everything"
$sevenZipExe = "c:\Program Files\7-Zip\7z.exe"
if (-not (test-path $sevenZipExe)) 
{
    $sevenZipExe =  "C:\Program Files (x86)\7-Zip\7z.exe"
    if (-not (test-path $sevenZipExe)) 
    {
        throw "$env:ProgramFiles\7-Zip\7z.exe needed"
        Exit 
    }

} 

Write-Host "Copying base configuration file"
Copy-Item ".\assets\configs\default" `
    "$DestinationDirConfigs\default" `
    -Force -Recurse

Write-Host "Copying configurations paramters sample"
New-Item -Path "$DestinationDirConfigs\paramsample\" -ItemType Directory
Copy-Item ".\assets\configs\parameters.documentstore.config.sample" `
    "$DestinationDirConfigs\paramsample\parameters.documentstore.config.sample" `
    -Force -Recurse

Write-Host "Copying default configuration file"
Copy-Item ".\assets\configs\defaultParameters.config" `
    "$DestinationDirConfigs\defaultParameters.config" `
    -Force -Recurse

set-alias sz $sevenZipExe 

$extension = ".7z"
if ($StandardZipFormatBool -eq $true) 
{
    Write-Output "Choose standard ZIP format instead of 7Z"
    $extension = ".zip"
}
 

$Source = $DestinationDirHost + "\*"
$Target = $DestinationDir + "\Jarvis.DocumentStore.Host" + $extension

#sz a -m0=lzma2 -mx=9 -aoa $Target $Source
sz a -mx=5 $Target $Source

$Source = $DestinationDirJobs + "\*"
$Target = $DestinationDir + "\Jarvis.DocumentStore.Jobs" + $extension

#sz a -m0=lzma2 -mx=9 -aoa $Target $Source
sz a -mx=5 $Target $Source

$Source = $DestinationDirConfigs + "\*"
$Target = $DestinationDir + "\DocumentStoreConfig" + $extension

#sz a -m0=lzma2 -mx=9 -aoa $Target $Source
sz a -mx=5 $Target $Source

if ($DeleteOriginalAfterZipBool -eq $true) 
{
    Remove-Item $DestinationDirHost  -Recurse -Force
	Remove-Item $DestinationDirJobs  -Recurse -Force
    Remove-Item $DestinationDirConfigs  -Recurse -Force
}

