using Jarvis.DocumentStore.Core.Processing.Conversions;

namespace Jarvis.DocumentStore.Core.Jobs
{
    public class ExtractTextWithTikaJob : AbstractTikaJob
    {

        protected override ITikaAnalyzer BuildAnalyzer()
        {
            return new TikaAnalyzer(ConfigService)
            {
                Logger = this.Logger
            };            
        }
    }
}
