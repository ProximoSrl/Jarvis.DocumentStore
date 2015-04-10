using NEventStore.Persistence.MongoDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Host.Support
{
    public class MongoPersistenceOptionWithLog : MongoPersistenceOptions
    {
        public override string GetInsertCommitScript()
        {
            return
@"
    function (commit) {
    var result;
    while (1) {
        var cursor = db.Commits.find( {}, { _id: 1 } ).sort( { _id: -1 } ).limit(1);

        var seq = cursor.hasNext() ? cursor.next()._id + 1 : 1;

        commit._id = NumberLong(seq);

        db.Commits.insert(commit);
        
        var err = db.getLastErrorObj();

        if( err && err.code ) {
            
            if( err.code == 11000 /* dup key */  && 
                err.err.indexOf('$_id_') != -1  /* dup checkpoint id */){

                continue;
            }
            else{
                err.commit = commit;
                db.NEventStoreLog.insert(err);
                result = err ;
                break;
            }
        }

        result = {id: commit._id};
        break;
    }
    return result;
}
";
        }
    }
}
