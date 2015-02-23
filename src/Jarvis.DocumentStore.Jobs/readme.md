##How to write a Document Store poller in .NET

###Samples

Inside Document Store, we have some pre-build plugin that can be used as samples to understand how to write a new poller plugin.

###Host

Project Jarvis.DocumentStore.JobsHost is the host pre-configured to run a poller. To create a new poller you need only to 

1. Create a new project (library), the name of the projection **must contains '.jobs.' in assembly Name** (this is done to reduce the risk of crashing loading an unwanted assembly)
2. Reference projects JobsHost, Core, Client and Shared, also add nuget package Castle.Logging integration
3. Write your plugin inheriting from base class AbstractOutOfProcessPollerFileJob
4. (Optional) Change post-build action of your project to copy everything in a known directory, es: xcopy "$(TargetDir)*.*" "$(SolutionDir)..\artifacts\jobs\email\" /Y /E
5. If you want to test the single poller you can simply configure VS to use the host to debug the project, just add needed executable parameters, es 

	/dsuris:http://localhost:5123 /queue:pdfThumb /handle:manual-execution

###Passwords

Some file are password protected. Tika is enabled to use password. The real password is stored in client running jobs. The setting is in environment variable

	DS_DOCPWDS

The content of this variable is a list of tuple

	regex||password

where regex is a regex that specify whitch file should use the password. you can specify more tuples for different files es:

	\.pdf||passwd1,\.doc||passwd2

Where I specified two password, one for pdf files the other for doc files. If you need to use a comma inside a password, you should escape the password with double comma. ES

	\.pdf||contains,,comma

 