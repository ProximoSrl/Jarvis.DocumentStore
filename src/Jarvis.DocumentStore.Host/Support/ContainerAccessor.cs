using Castle.Windsor;

namespace Jarvis.DocumentStore.Host.Support
{
    public static class ContainerAccessor
    {
        public static IWindsorContainer Instance { get; set; }
    }
}