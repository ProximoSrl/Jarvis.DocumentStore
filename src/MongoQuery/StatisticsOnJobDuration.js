db.getCollection('queue.imgResize').aggregate(
[
    {$project : {
        "ExecutionEndTime" : 1 , 
        "ExecutionStartTime" : 1,
        "Handle" : 1,
        "Duration" : {$subtract : ["$ExecutionEndTime", "$ExecutionStartTime"]}}} ,
    {$sort : {"Duration" : -1}} 
])