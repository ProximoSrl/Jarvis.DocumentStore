$procs = Get-Process soffice -ErrorAction SilentlyContinue
foreach($proc in $procs) 
{
    Write-Output "killing $proc"
    kill $proc -Force 
}

$procs = Get-Process "soffice.bin" -ErrorAction SilentlyContinue
foreach($proc in $procs) 
{
    Write-Output "killing $proc"
    kill $proc -Force 
}