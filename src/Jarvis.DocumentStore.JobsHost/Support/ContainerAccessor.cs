using Castle.Windsor;

namespace Jarvis.DocumentStore.JobsHost.Support
{
    public static class ContainerAccessor
    {
        public static IWindsorContainer Instance { get; set; }
    }
}