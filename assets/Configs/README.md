##Configuration of Document Store

###Parameters

This folder contains a file called parameters.documentstore.config.sample that contains parameters sample file. You should  place this file inside your configuration manager configuration folder, at the same level of the DocumentStore.redirect file.

This permits to change specific parameters to your dev machine or production environment and using the standard default configuration that you download with source control.

###Permissions

DocumentStore needs to open two HTTP port, the first one is used by the main service, the other one is used by metrics.net.

Remember that, in order to run with no-administrative privileges, you need to explicitly add permission to open that port for the user used to run the service.

	netsh http add urlacl url=http://127.0.0.1:5123/ user=Everyone

This is the configuration for the main port of the application, you can use machine_name and this will be substituted by the name of the machine.

```javascript
"api-bindings": [
    "http://machine_name:5123",
    "http://localhost:5123",
    "http://127.0.0.1:5123",
],
```

**If document store started, but navigating with chrome returns a "Service Unavailable", it probably indicates that netsh was badly configured**. In this situation, you should check all permission on selected port running

	netsh http show urlacl 

and then looking at each entry with 5123 as port. You should usually delete every registration for every ip, Es.

	netsh http delete urlacl url=http://127.0.0.1:5123/ 
	netsh http delete urlacl url=http://MACHINENAME:5123/ 
	netsh http delete urlacl url=http://+:5123/ 
	netsh http delete urlacl url=http://localhost:5123/ 

Then re-add all needed registration. Remember that you should add an urlacl for every specific address. Meters is usually installed with this configuration to listen on all IP

```javascript
"meters": {
    "http-endpoint": "http://+:55558/"
},
```

With this configuration you will need to setup acl (netsh http add urlacl url=http://+:55558/ user=Everyone)



###A section of the configuration is dedicated to roles

```javascript
{
	"roles": {
	    "api": true,
	    "worker": true,
	    "projections": true,
		"queueManager" : true,
		"jobMode" : "queue"
	}
}
```


- Api: api controller is started.
- worker: all worker (jobs) starts (if jobMode is poller it starts out of process pollers)
- projection: projection engine active.
- queueManager: new job management with custom queue enabled.
- jobMode: queue or quartz to choose witch job engine to use.

###Another section is dedicated to queue configuration

```javascript
"queues" : {
		"stream-poll-interval-ms" : 1000,
		"jobs-poll-interval-ms" : 1000,
	    "list" : [
			{
			    //Tika wants every format except if it comes from office pipeline
				"name" : "tika",
				//"pipeline" : "^(?!office$).*"
				"pipeline" : "original|htmlzip" //htmlZip pipeline generates pdf from zip.
			},
			{
			    //pdfThumbnail pipeline accepts every format of extension pdf
				"name" : "pdfThumb",
				"extension" : "pdf",
				"parameters" : {
					"thumb_format" : "png"
				}
			}
```

- stream-poll-interval-ms: interval used by Queue Manager to poll the stream read model to generate jobs
- jobs-poll-interval-ms interval of polling used by jobs to read the relative job queue
- list: a list of queue definition object **each queue object is a distinct job queue for a specific worker**

In the above example I defined a queue named tika, that accepts every extension and try to extract text from two pipeline, original is the original file, while the htmlzip is the pipeline that generates pdf from zipped email. This permits to extract text from pdf if tika was not able to extract text directly from email.

The other queue is the one dedicated to generation of thumbnail from pdf, it accepts only pdf file extension, from any pipeline and has some custom parameters like thumb_format to specify the format of the thumbnail.

