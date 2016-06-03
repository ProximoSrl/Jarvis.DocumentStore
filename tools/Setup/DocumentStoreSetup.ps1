param(
    [string] $deployHostFile,
    [string] $deployJobsFile,
    [string] $deployConfigurationFile,
    [string] $installationRoot,
    [string] $hostPort = "5123",
    [string] $metricsPort = "55558",
    [string] $overwriteConfig = "$true"
)

$overwriteConfigBool = ($overwriteConfig -eq "$true") -or ($overwriteConfig -eq "true")

$hostInstallDir = "$installationRoot\Host"
$jobsInstallDir = "$installationRoot\Jobs"
$configInstallDir = "$installationRoot\ConfigLatest"
$configReleaseInstall = "$installationRoot\Config"

if(-not(Get-Module -name jarvisUtils)) 
{
    Write-Output (Split-Path -Parent -Path $MyInvocation.MyCommand.Definition)
    $runningDirectory = Split-Path -Parent -Path $MyInvocation.MyCommand.Definition
    Write-Output "loading jarvis utils module from $runningDirectory"
    Import-Module -Name "$runningDirectory\\jarvisUtils"
}

Write-Output 'Stopping Service Jarvis - Document Store'

$service = Get-Service -Name "Jarvis - Document Store" -ErrorAction SilentlyContinue 
if ($service -ne $null) 
{
    Write-Output "Stopping Document Store Service"
    Stop-Service "Jarvis - Document Store"
    $service.WaitForStatus("Stopped")
}

$jobsLog4net = "$jobsInstallDir\log4net.config"
$latestJobsLog4net = "$installationRoot\jobs.log4net.config.latest"
if (Test-Path $jobsLog4net) 
{
    Copy-Item -Path $jobsLog4net -Destination $latestJobsLog4net -Force
}

Write-Output "Removing all file in jobs directory"
if (Test-Path -Path $jobsInstallDir) 
{
$allJobsFile = Get-ChildItem -Path $jobsInstallDir -Recurse
$oldDeletedFile = $allJobsFile | where {!$_.FullName.EndsWith(".exe.config") -and !$_.PSIsContainer} | Remove-Item -Force

}

Write-Output "Expanding jobs file $deployJobsFile to $jobsInstallDir"
Expand-WithFramework -zipFile $deployJobsFile -destinationFolder $jobsInstallDir -deleteOld $false
Write-Output "Expanding configuration file $deployConfigurationFile to $configInstallDir"
Expand-WithFramework -zipFile $deployConfigurationFile -destinationFolder $configInstallDir
Write-Output "Expanding host file $deployHostFile to $hostInstallDir"
Expand-WithFramework -zipFile $deployHostFile -destinationFolder $hostInstallDir

if (!(Test-Path $configReleaseInstall) -or $overwriteConfigBool) 
{
    Write-Output "Ovewriting old configuration in $configReleaseInstall"
    Copy-Item $configInstallDir -Destination $configReleaseInstall -Recurse -Force
}
else
{
    $answer = Get-YesNoAnswer -question "Do you want to overwrite latest configuration? [y/N]" -default "n"
    if ($answer -eq 'y')
    {
        Write-Output "Ovewriting old configuration in $configReleaseInstall"
        Remove-Item $configReleaseInstall -Recurse -Force
        Copy-Item $configInstallDir -Destination $configReleaseInstall -Recurse -Force
    }
}

#rename all .orig file if real configuration file does not exists
$allOriginalConfig = Get-ChildItem -Path $jobsInstallDir -Recurse -Filter "*.config.original"
foreach($file in $allOriginalConfig)
{
    $realConfigFile = $file.FullName.Substring(0, $file.FullName.Length - ".original".Length)
    if (!(Test-Path $realConfigFile))
    {
        Write-Output "No config file $realConfigFile exists, renaming .original"
        [System.IO.File]::Move($file.FullName, $realConfigFile)
    }
}


#remove all log4net.config except the one in the top directory
$allLog4Net = Get-ChildItem -Path $jobsInstallDir -Recurse -Filter "log4net.config"
$delete = $allLog4Net | where {$_.Directory.FullName -ne $jobsInstallDir} | Remove-Item

if (Test-Path $latestJobsLog4net) 
{
    Copy-Item -Path $latestJobsLog4net -Destination $jobsLog4net -Force
}

if ($service -eq $null) 
{
    Write-Output "Starting the service in $hostInstallDir\Jarvis.DocumentStore.Host.exe"

    & "$hostInstallDir\Jarvis.DocumentStore.Host.exe" install
} 
 
& netsh http add urlacl url=http://+:$hostPort/ user=Everyone
& netsh http add urlacl url=http://+:$metricsPort/ user=Everyone

 $documentStoreMarkerFile = "$installationRoot\documentstore.application"
 if (!(Test-Path $documentStoreMarkerFile)) 
 {
        Write-Output "Writing marker file $documentStoreMarkerFile"
               [System.IO.File]::WriteAllText($documentStoreMarkerFile, "#jarvis-config
application-name:documentstore
base-server-address:http://localhost:55555")
}

$body = "{""ApplicationName"" : ""DocumentStore"", ""RedirectFolder"" : """ + $configInstallDir.Replace("\", "\\") + """}"
Write-Output "Adding application to configuration manager $body" 
Write-Output $body
Invoke-RestMethod -Uri "http://localhost:55555/api/applications/DocumentStore" `
    -Method "PUT" `
    -Body $body `
    -ContentType "application/json"

$jobsLocation = $jobsInstallDir.Replace("\", "\\") + "\\"
Invoke-RestMethod -Uri "http://localhost:55555/api/defaultparameters/DocumentStore/$env:computername" `
    -Method "PUT" `
    -Body "{""jobs"" : {""location"" : ""$jobsLocation""}}" `
    -ContentType "application/json"


$sampleParameter = [System.IO.File]::ReadAllText("$configInstallDir\paramsample\parameters.documentstore.config.sample")
Write-Output "Updating default configuration to configuration manager" 
Invoke-RestMethod -Uri "http://localhost:55555/api/defaultparameters/DocumentStore/$env:computername" `
    -Method "PUT" `
    -Body $sampleParameter `
    -ContentType "application/json"

$defaultConfigurationContent = [System.IO.File]::ReadAllText("$configInstallDir\defaultParameters.config")
Write-Output "Updating default configuration to configuration manager $configInstallDir\DefaultParameters.config" 
Invoke-RestMethod -Uri "http://localhost:55555/api/defaultparameters/DocumentStore/$env:computername" `
    -Method "PUT" `
    -Body $defaultConfigurationContent `
    -ContentType "application/json"

Write-Output "Jarvis Document Store Installed"

