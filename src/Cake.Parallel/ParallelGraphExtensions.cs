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

            var visitedNodes = new Dictionary<string, Task>();
            return graph.traverse(target, executeTask, visitedNodes);
        }

        private static Task traverse(this CakeGraph graph, string nodeName, Action<string> executeTask, IDictionary<string, Task> visitedNodes)
        {
            if (visitedNodes.ContainsKey(nodeName)) return visitedNodes[nodeName];

            var dependentTasks = graph.Edges
                .Where(_ => _.End.Equals(nodeName, StringComparison.OrdinalIgnoreCase))
                .Select(_ =>
                {
                    var task = graph.traverse(_.Start, executeTask, visitedNodes);
                    visitedNodes[_.Start] = task;
                    return task;
                })
                .ToArray();

            if (!dependentTasks.Any()) return Task.Factory.StartNew(() => executeTask(nodeName));

            return Task.Factory.ContinueWhenAll(dependentTasks, _ => executeTask(nodeName));
        }

        private static bool hasCircularReferences(this CakeGraph graph, string nodeName, HashSet<string> visited = null)
        {
            visited = visited ?? new HashSet<string>();

            if (visited.Contains(nodeName) && graph.hasDependency(nodeName)) return true;

            visited.Add(nodeName);
            var dependencies = graph.Edges
                .Where(_ => _.End.Equals(nodeName, StringComparison.OrdinalIgnoreCase))
                .Select(_ => _.Start);

            return dependencies.Any(dependency => graph.hasCircularReferences(dependency, visited));
        }

        private static bool hasDependency(this CakeGraph graph, string nodeName)
        {
            return graph.Edges
                .Any(_ => _.End.Equals(nodeName, StringComparison.OrdinalIgnoreCase));
        }
    }
}
