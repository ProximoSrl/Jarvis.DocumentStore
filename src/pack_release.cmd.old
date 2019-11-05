rmdir .\publish /s /q

xcopy .\Jarvis.DocumentStore.Host\bin\release\net461 .\publish\Host\ /S /Y
del .\publish\Host\*shost.*
del .\publish\Host\*.xml
rmdir .\publish\logs /s /q

xcopy .\Jarvis.DocumentStore.Host\app .\publish\Host\app\ /S /Y

echo Copying jobs

xcopy .\Jarvis.DocumentStore.Jobs\Jarvis.DocumentStore.Jobs.Attachments\bin\release\net461 .\publish\jobs\attachments\ /S /Y
xcopy .\Jarvis.DocumentStore.Jobs\Jarvis.DocumentStore.Jobs.Email\bin\release\net461 .\publish\jobs\email\ /S /Y
xcopy .\Jarvis.DocumentStore.Jobs\Jarvis.DocumentStore.Jobs.HtmlZipOld\bin\release\net461 .\publish\jobs\htmlzipOld\ /S /Y
xcopy .\Jarvis.DocumentStore.Jobs\Jarvis.DocumentStore.Jobs.HtmlZip\bin\release\net461 .\publish\jobs\htmlzip\ /S /Y
xcopy .\Jarvis.DocumentStore.Jobs\Jarvis.DocumentStore.Jobs.ImageResizer\bin\release\net461 .\publish\jobs\imageresizer\ /S /Y
xcopy .\Jarvis.DocumentStore.Jobs\Jarvis.DocumentStore.Jobs.LibreOffice\bin\release\net461 .\publish\jobs\libreoffice\ /S /Y
xcopy .\Jarvis.DocumentStore.Jobs\Jarvis.DocumentStore.Jobs.PdfThumbnails\bin\release\net461 .\publish\jobs\pdfthumbnails\ /S /Y
xcopy .\Jarvis.DocumentStore.Jobs\Jarvis.DocumentStore.Jobs.Tika\bin\release\net461 .\publish\jobs\tika\ /S /Y
xcopy .\Jarvis.DocumentStore.Jobs\Jarvis.DocumentStore.Jobs.VideoThumbnails\bin\release\net461 .\publish\jobs\videothumbnails\ /S /Y

pause