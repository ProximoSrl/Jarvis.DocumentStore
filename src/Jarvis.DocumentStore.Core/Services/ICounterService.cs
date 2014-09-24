namespace Jarvis.DocumentStore.Core.Services
{
    public interface ICounterService
    {
        long GetNext(string serie);
    }
}