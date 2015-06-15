#Prerequisites

##Configuration Manager

You should install [Configuration Service](https://github.com/ProximoSrl/Jarvis.ConfigurationService) on your machine to run DocumentStore. You can download latest release and install with Powershell, or you can compile from source and do a manually install. 


After configuration service is installed, you usually put a documentstore.redirect file that point to the Assets\config directory. A typical content of documentstore.redirect file is 

	C:\develop\proximo\Jarvis\Assets\Configs

At the same level (root of configuration service) where you put this redirect file, you usually have another file called *parameters.documenstore.config* containing parameters for documentstore. You can find a sample of this file in the assets\configs folder in source. 

##External software

Various jobs of documenstore depends on external tools and libraries. 

###Tika

Download from https://tika.apache.org/ latest tested tika version (1.6). Direct link is  [http://archive.apache.org/dist/tika/tika-app-1.6.jar](http://archive.apache.org/dist/tika/tika-app-1.6.jar) 

If you want to manually test tika with command line you can use the following command.

  	java -jar tika-app-1.4.jar nomefile

You need also to create an environment variable TIKA_HOME that contains the full name of the tika jar file you previsously downloaded.

### Ghostscript
Download and install 32 bits version of Ghostscript http://downloads.ghostscript.com/public/gs907w32.exe

***

### Magick.NET
To use Magick.NET you need Visual C++ Redistributable 2012 x86 version. The version is really important because if you only install x64 it will not work.

Il prerequisito si scarica qui [http://download.microsoft.com/download/1/6/B/16B06F60-3B20-4FF2-B699-5E9B7962F9AE/VSU_4/vcredist_x86.exe](http://download.microsoft.com/download/1/6/B/16B06F60-3B20-4FF2-B699-5E9B7962F9AE/VSU_4/vcredist_x86.exe)
***

### Libreoffice
Download libreoffice and configure an environment variable called LIBREOFFICE_PATH that contains the full path of *soffice.exe* executable file. 