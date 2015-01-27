##Configuration of Document Store

###A section of the configuration is dedicated to roles

```javascript
{
	"roles": {
	    "api": true,
	    "worker": true,
	    "projections": true,
		"queueManager" : true,
		"pollers" : true,
		"jobMode" : "queue"
	}
}
```


- Api: api controller is started.
- worker: all worker (jobs) starts.
- projection: projection engine active.
- queueManager: new job management with custom queue enabled.
- poller: if jobMode is queue and poller is true, it starts job poller. 
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

