param(
    [string]$DestinationFolder = '..\..\ServicesDev\Jarvis.DocumentStore.Host'
)

Import-Module -Name ".\Invoke-MsBuild.psm1"
Write-Host 'Cleaning and compiling solution'
$compileResult = Invoke-MsBuild -Path '..\src\Jarvis.DocumentStore.sln' -MsBuildParameters "/target:Clean;Build /p:Configuration=Release"

if ($compileResult -eq $false) 
{
    Write-Error 'Compile failed'
    Exit 1
}

Write-Host 'Stopping Service DocumentStore'

$service = Get-Service -Name "Jarvis - Document Store" 
if ($service -ne $null) 
{
    Stop-Service "Jarvis - Document Store"
}

Write-Host 'Deleting actual directory'

Remove-Item -Recurse $DestinationFolder

Write-Host 'Copy new deploy to destination folder'


$rco = robocopy '..\src\Jarvis.DocumentStore.Host\bin\Release' $DestinationFolder /e 
$rco = robocopy '..\src\Jarvis.DocumentStore.Host\app' "$DestinationFolder\app" /e 
echo $null > "$DestinationFolder\DocumentStore.application"
if ($service -eq $null) 
{
    Write-Host 'Installing the service in $DestinationFolder\Jarvis.DocumentStore.Host.exe'
    $ps = Start-Process "$DestinationFolder\Jarvis.DocumentStore.Host.exe" -ArgumentList 'install' -Wait -NoNewWindow
    Write-Host 'installing service exited with:' $ps.ExitCode
} 

Write-Host 'Starting the service'
Start-Service "Jarvis - Document Store"