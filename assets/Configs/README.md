##Configuration of Document Store

A section of the configuration is dedicated to roles


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

Api: api controller is started
worker: all worker (jobs) starts
projection: projection engine active
queueManager: new job management with custom queue enabled.
poller: if jobMode is queue and poller is true, it starts job poller 
jobMode: queue or quartz to choose witch job engine to use.