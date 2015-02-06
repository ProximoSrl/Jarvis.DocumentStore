using Castle.Windsor;

namespace Jarvis.DocumentStore.Jobs.Support
{
    public static class ContainerAccessor
    {
        public static IWindsorContainer Instance { get; set; }
    }
}