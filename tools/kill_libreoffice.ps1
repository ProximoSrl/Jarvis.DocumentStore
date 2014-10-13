$procs = Get-Process soffice
foreach($proc in $procs) 
{
    kill $proc
}