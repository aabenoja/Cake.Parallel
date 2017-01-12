using Cake.Core;
using Cake.Core.Composition;

namespace Cake.Parallel.Module
{
    public class ParallelCakeModule : ICakeModule
    {
        public void Register(ICakeContainerRegistry registry)
        {
            registry.RegisterType<ParallelCakeEngine>().As<ICakeEngine>().Singleton();
        }
    }
}
