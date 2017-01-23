using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cake.Core;

namespace Cake.Parallel.Module
{
    public static class ParallelGraphExtensions
    {
        public static Task Traverse(this CakeGraph graph, string target, Action<string, CancellationTokenSource> executeTask)
        {
            if (!graph.Exist(target)) return Task.CompletedTask;
            if (graph.hasCircularReferences(target)) throw new CakeException("Graph contains circular references.");

            var cancellationTokenSource = new CancellationTokenSource();
            var visitedNodes = new Dictionary<string, Task>();
            return graph.traverse(target, executeTask, cancellationTokenSource, visitedNodes);
        }

        private static async Task traverse(
            this CakeGraph graph, string nodeName,
            Action<string, CancellationTokenSource> executeTask,
            CancellationTokenSource cancellationTokenSource,
            IDictionary<string, Task> visitedNodes)
        {
            if (visitedNodes.ContainsKey(nodeName))
            {
                await visitedNodes[nodeName].ConfigureAwait(false);
                return;
            }

            var token = cancellationTokenSource.Token;
            var dependentTasks = graph.Edges
                .Where(_ => _.End.Equals(nodeName, StringComparison.OrdinalIgnoreCase))
                .Select(_ =>
                {
                    var task = graph.traverse(_.Start, executeTask, cancellationTokenSource, visitedNodes);
                    visitedNodes[_.Start] = task;
                    return task;
                })
                .ToArray();

            if (dependentTasks.Any())
            {
                TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
                token.Register(() => tcs.TrySetCanceled(), false);
                await Task.WhenAny(Task.WhenAll(dependentTasks), tcs.Task).ConfigureAwait(false);
            }

            await Task.Factory.StartNew(() => executeTask(nodeName, cancellationTokenSource), token).ConfigureAwait(false);
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
