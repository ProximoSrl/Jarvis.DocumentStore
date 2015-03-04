db.Commits.find({$or:[{"Events.Payload.Body.AggregateId":{$exists:true}},{"Events.Payload.Body._id":{$exists:true}}]).forEach(function(commit){
    var arr = commit.Events;
    var length = arr.length;
    for(var i = 0; i < length; i++){
        print(arr[i].Payload.Body);
        delete arr[i].Payload.Body["_id"];
        delete arr[i].Payload.Body["AggregateId"];
    }
    db.Commits.save(commit);
});