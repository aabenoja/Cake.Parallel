using Cake.Core;
using Cake.Core.Annotations;
using Cake.Core.Composition;

[assembly: CakeModule(typeof(Cake.Parallel.Module.ParallelCakeModule))]

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
