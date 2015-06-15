$procs = Get-Process soffice -ErrorAction SilentlyContinue
foreach($proc in $procs) 
{
    kill $proc
}