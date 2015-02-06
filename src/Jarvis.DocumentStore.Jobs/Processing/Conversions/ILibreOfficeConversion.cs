namespace Jarvis.DocumentStore.Jobs.Processing.Conversions
{
    public interface ILibreOfficeConversion
    {
        string Run(string sourceFile, string outType);

        /// <summary>
        /// Useful to kill already running processes
        /// </summary>
        void Initialize();
    }
}