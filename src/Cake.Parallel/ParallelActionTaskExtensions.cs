using Cake.Core;

namespace Cake.Parallel.Module
{
    public static class ParallelActionTaskExtensions
    {
        public static CakeTaskBuilder<ActionTask> IgnoreCancellation(this CakeTaskBuilder<ActionTask> task)
        {
            return task;
        }
    }
}
