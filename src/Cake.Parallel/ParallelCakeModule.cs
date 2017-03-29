using Cake.Core;
using Cake.Core.Composition;

namespace Cake.Parallel.Module
{
    public class ParallelCakeModule : ICakeModule
    {
        public void Register(ICakeContainerRegistrar registrar)
        {
            registrar.RegisterType<ParallelCakeEngine>().As<ICakeEngine>().Singleton();
        }
    }
}
