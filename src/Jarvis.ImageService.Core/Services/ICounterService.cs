namespace Jarvis.ImageService.Core.Services
{
    public interface ICounterService
    {
        long GetNext(string serie);
    }
}