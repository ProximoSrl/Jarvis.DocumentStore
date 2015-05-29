#Installation with scripts

Simply copy all these scripts in the folder you want to install ConfigurationService, then from a PowerShell console, with administration rights (you need rights to install services) you can simply launch

	.\TcSetupDocumentStore.ps1

You will be prompted for credentials for the build server, then it will download the latest build from selected branch (default is master) and install everything.

