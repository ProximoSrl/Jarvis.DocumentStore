namespace Jarvis.DocumentStore.Jobs.LibreOffice
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