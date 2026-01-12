using Zenject;

namespace MPGame3d
{
    public static class ZenjectAccessor
    {
        public static DiContainer Container { get; private set; }

        public static void SetContainer(DiContainer container)
        {
            Container = container;
        }
    }
}