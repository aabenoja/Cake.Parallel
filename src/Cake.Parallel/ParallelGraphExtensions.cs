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

        private static bool hasCircularReferences(this CakeGraph graph, string nodeName, Stack<string> visited = null)
        {
            visited = visited ?? new Stack<string>();

            if (visited.Contains(nodeName)) return true;

            visited.Push(nodeName);
            var hasCircularReference = graph.Edges
                .Where(_ => _.End.Equals(nodeName, StringComparison.OrdinalIgnoreCase))
                .Any(_ => graph.hasCircularReferences(_.Start, visited));
            visited.Pop();
            return hasCircularReference;
        }
    }
}
