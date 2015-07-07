using Castle.Core.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;

namespace Jarvis.DocumentStore.Core.Support
{
    public static class LoggingExtensionMethods
    {
        public static void ErrorFormat(
            this IExtendedLogger logger,
            IDictionary<String, Object> extraProperties,
            Exception exception,
            String formatString,
            params Object[] args)
        {
            try
            {
                if (extraProperties != null)
                {
                    foreach (var key in extraProperties.Keys)
                    {
                        var obj = extraProperties[key];
                        if (obj == null)
                        {
                            logger.ThreadProperties["reqparam-" + key] = "null";
                        }
                        else
                        {
                            try
                            {
                                logger.ThreadProperties["reqparam-" + key] = obj.ToBsonDocument();
                            }
                            catch (Exception)
                            {
                                //if object is not bsonserializable, simply store. (it could be a string or base type);
                                logger.ThreadProperties["reqparam-" + key] = obj;
                            }
                        }
                    }
                }
                logger.ErrorFormat(exception, formatString, args);
            }
            finally
            {
                if (extraProperties != null)
                {
                    foreach (var key in extraProperties.Keys)
                    {
                        logger.ThreadProperties["reqparam-" + key] = null;
                    }
                }
            }

        }
    }
}
