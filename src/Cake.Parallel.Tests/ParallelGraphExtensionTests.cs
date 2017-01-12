using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cake.Core;
using Cake.Parallel.Module;
using Shouldly;
using Xunit;

namespace Cake.Parallel.Tests
{
    public class ParallelGraphExtensionTests
    {
        private readonly CakeGraph _graph;
        private readonly List<CakeTask> _tasks;
        private readonly List<string> _taskResults;

        public ParallelGraphExtensionTests()
        {
            _taskResults = new List<string>();
            _tasks = new List<CakeTask>(defineTasks());
            _graph = ParallelGraphBuilder.Build(_tasks);
        }

        [Fact]
        public void Throws_On_Circular_References()
        {
            Should.Throw<CakeException>(() =>
            {
                _graph.Traverse("circ-c", _ => {});
            });
        }

        [Fact]
        public async Task Should_Execute_Dependencies_Asynchronously()
        {
            await _graph.Traverse("e", nodeName =>
            {
                var task = (ActionTask)_tasks.First(_ => _.Name == nodeName);
                task.Actions.ForEach(action => action(null));
            });
            _taskResults.ShouldNotBeEmpty();
        }

        private IEnumerable<CakeTask> defineTasks()
        {
            var circularTaskA = new ActionTask("circ-a");
            new CakeTaskBuilder<ActionTask>(circularTaskA)
                .IsDependentOn("circ-b");
            yield return circularTaskA;

            var circularTaskB = new ActionTask("circ-b");
            new CakeTaskBuilder<ActionTask>(circularTaskB)
                .IsDependentOn("circ-c");
            yield return circularTaskB;

            var circularTaskC = new ActionTask("circ-c");
            new CakeTaskBuilder<ActionTask>(circularTaskC)
                .IsDependentOn("circ-a");
            yield return circularTaskC;

            var taskA = new ActionTask("a");
            new CakeTaskBuilder<ActionTask>(taskA)
                .Does(() =>
                {
                    Thread.Sleep(1000);
                    _taskResults.Add("a");
                });
            yield return taskA;

            var taskB = new ActionTask("b");
            new CakeTaskBuilder<ActionTask>(taskB)
                .IsDependentOn("a")
                .Does(() => _taskResults.Add("b"));
            yield return taskB;

            var taskC = new ActionTask("c");
            new CakeTaskBuilder<ActionTask>(taskC)
                .IsDependentOn("a")
                .Does(() => _taskResults.Add("c"));
            yield return taskC;

            var taskD = new ActionTask("d");
            new CakeTaskBuilder<ActionTask>(taskD)
                .Does(() => _taskResults.Add("d"));
            yield return taskD;

            var taskE = new ActionTask("e");
            new CakeTaskBuilder<ActionTask>(taskE)
                .IsDependentOn("b")
                .IsDependentOn("c")
                .IsDependentOn("d")
                .Does(() => _taskResults.Add("e"));
            yield return taskE;
        }
    }
}
