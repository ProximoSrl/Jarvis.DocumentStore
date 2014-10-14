rmdir .\publish /s /q

xcopy .\Jarvis.DocumentStore.Host\bin\release .\publish\ /S /Y
del .\publish\*shost.*
del .\publish\*.xml
rmdir .\publish\logs /s /q


xcopy .\Jarvis.DocumentStore.Host\app .\publish\app\ /S /Y

pause