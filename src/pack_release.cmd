rmdir .\publish /s /q

xcopy .\Jarvis.DocumentStore.Host\bin\release .\publish\Host\ /S /Y
del .\publish\Host\*shost.*
del .\publish\Host\*.xml
rmdir .\publish\logs /s /q

xcopy .\Jarvis.DocumentStore.Host\app .\publish\Host\app\ /S /Y

echo Copying jobs

xcopy .\Jarvis.DocumentStore.Jobs\Jarvis.DocumentStore.Jobs.Attachments\bin\release .\publish\jobs\attachments\ /S /Y
xcopy .\Jarvis.DocumentStore.Jobs\Jarvis.DocumentStore.Jobs.Email\bin\release .\publish\jobs\email\ /S /Y
xcopy .\Jarvis.DocumentStore.Jobs\Jarvis.DocumentStore.Jobs.HtmlZipOld\bin\release .\publish\jobs\htmlzip\ /S /Y
xcopy .\Jarvis.DocumentStore.Jobs\Jarvis.DocumentStore.Jobs.HtmlZip\bin\release .\publish\jobs\htmlzipNew\ /S /Y
xcopy .\Jarvis.DocumentStore.Jobs\Jarvis.DocumentStore.Jobs.ImageResizer\bin\release .\publish\jobs\imageresizer\ /S /Y
xcopy .\Jarvis.DocumentStore.Jobs\Jarvis.DocumentStore.Jobs.Office\bin\release .\publish\jobs\libreoffice\ /S /Y
xcopy .\Jarvis.DocumentStore.Jobs\Jarvis.DocumentStore.Jobs.PdfThumbnail\bin\release .\publish\jobs\pdfthumb\ /S /Y
xcopy .\Jarvis.DocumentStore.Jobs\Jarvis.DocumentStore.Jobs.Tika\bin\release .\publish\jobs\tika\ /S /Y

pause