##Backup with Mongodump

You should backup original and artifacts database first, then backup main database with commits and system. Backup the system while the system is running can lead to inconsistencies that should be fixed manually.

###Error in the id

You start having this error after a restore

```
	MongoDB.Driver.MongoDuplicateKeyException: 
		WriteConcern detected an error 'E11000 duplicate key error collection: ds-docs-ori-fs.original.files index: _id_ dup key: { : "original.124706" }'. 
		(Response was { "ok" : 1, "code" : 11000, "err" : "E11000 duplicate key error collection: ds-docs-ori-fs.original.files index: _id_ dup key: { : \"original.124706\" }", "n" : NumberLong(0) }).
``` 

This happens because after the backup of **sysCounters** collection the system continued to update GridFs. The error occurred because the sysCounters for original blob contains value X, but the original collection on gridfs already contains value greater than X.

This can be fixed manually for each gridfs, first of all grab the highest generated blob id for each collection, lets start with the original.

	db.getCollection('original.files').find({}).sort({uploadDate : -1})

Now look at the very first records, they have id like **original.12534**, scan the first records to find the highest number (Since you are ordering with uploadDate that does not guarantee that the first record is really has the highest id). 

Now check in the **sysCounters** collection the value for the original, and probably the value is lower. Simply edit the sysCounters document, storing in the original counter the value 12534. 

Repeat this process for all artifacts.