namespace Jarvis.DocumentStore.Core.Processing.Conversions
{
    public interface ILibreOfficeConversion
    {
        string Run(string sourceFile, string outType);
    }
}