using Jarvis.DocumentStore.Core.Processing.Conversions;

namespace Jarvis.DocumentStore.Core.Jobs
{
    public class ExtractTextWithTikaNetJob : AbstractTikaJob
    {
        protected override ITikaAnalyzer BuildAnalyzer()
        {
            return new TikaNetAnalyzer();
        }
    }
}