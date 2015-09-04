db.getCollection('Commits').find({}).sort({_id:1}).forEach(function(commit){
    var evts = commit["Events"];
    evts.forEach(function(evt){
        evt["Payload"]["Body"]["AggregateId"] = commit["StreamId"];
    });
    db.getCollection('Commits').save(commit);    
});