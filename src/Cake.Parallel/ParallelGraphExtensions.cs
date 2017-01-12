using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cake.Core;

namespace Cake.Parallel.Module
{
    public static class ParallelGraphExtensions
    {
        public static Task Traverse(this CakeGraph graph, string target, Action<string> executeTask)
        {
            if (!graph.Exist(target)) return Task.CompletedTask;
            if (graph.hasCircularReferences(target)) throw new CakeException("Graph contains circular references.");

            return graph.traverse(target, executeTask);
        }

        private static Task traverse(this CakeGraph graph, string nodeName, Action<string> executeTask)
        {
            var dependentTasks = graph.Edges
                .Where(_ => _.End.Equals(nodeName, StringComparison.OrdinalIgnoreCase))
                .Select(_ => graph.traverse(_.Start, executeTask))
                .ToArray();

            if (!dependentTasks.Any()) return Task.Factory.StartNew(() => executeTask(nodeName));

            return Task.Factory.ContinueWhenAll(dependentTasks, _ => executeTask(nodeName));
        }

        private static bool hasCircularReferences(this CakeGraph graph, string nodeName, HashSet<string> visited = null)
        {
            visited = visited ?? new HashSet<string>();

            if (visited.Contains(nodeName)) return true;

            visited.Add(nodeName);
            var dependencies = graph.Edges
                .Where(_ => _.End.Equals(nodeName, StringComparison.OrdinalIgnoreCase))
                .Select(_ => _.Start);

            return dependencies.Any(dependency => graph.hasCircularReferences(dependency, visited));
        }
    }
}
