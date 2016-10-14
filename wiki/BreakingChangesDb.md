##version 2.0

##Rename of Queue property

Property DocumentId of queues was renamed during release of Version 2 to DocumentDescriptorId, to update older database you should run this set of queries agains queue database

	db.getCollection('queue.attachments').update({}, {$rename : {"DocumentId" : "DocumentDescriptorId"}}, {multi : true})
	db.getCollection('queue.email').update({}, {$rename : {"DocumentId" : "DocumentDescriptorId"}}, {multi : true})
	db.getCollection('queue.htmlzip').update({}, {$rename : {"DocumentId" : "DocumentDescriptorId"}}, {multi : true})
	db.getCollection('queue.imgResize').update({}, {$rename : {"DocumentId" : "DocumentDescriptorId"}}, {multi : true})
	db.getCollection('queue.office').update({}, {$rename : {"DocumentId" : "DocumentDescriptorId"}}, {multi : true})
	db.getCollection('queue.pdfThumb').update({}, {$rename : {"DocumentId" : "DocumentDescriptorId"}}, {multi : true})
	db.getCollection('queue.tika').update({}, {$rename : {"DocumentId" : "DocumentDescriptorId"}}, {multi : true})
	db.getCollection('queue.videoThumb').update({}, {$rename : {"DocumentId" : "DocumentDescriptorId"}}, {multi : true})

##Rename of readmodels properties

This query should be run against all tenants db to rename DocumentDb property of readmodel to DocumentDescriptorId

	db.getCollection('rm.Stream').update({}, {$rename : {"DocumentId" : "DocumentDescriptorId"}}, {multi : true})