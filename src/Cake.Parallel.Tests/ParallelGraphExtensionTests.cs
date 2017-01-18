using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
        private readonly StringBuilder _sb;
        private readonly List<CakeTask> _tasks;

        public ParallelGraphExtensionTests()
        {
            _sb = new StringBuilder();
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
            await _graph.Traverse("g", nodeName =>
            {
                var task = (ActionTask)_tasks.First(_ => _.Name == nodeName);
                task.Actions.ForEach(action => action(null));
            });
            _sb.ToString().Length.ShouldBe(7);
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
                    _sb.Append("a");
                });
            yield return taskA;

            var taskB = new ActionTask("b");
            new CakeTaskBuilder<ActionTask>(taskB)
                .IsDependentOn("a")
                .Does(() => _sb.Append("b"));
            yield return taskB;

            var taskC = new ActionTask("c");
            new CakeTaskBuilder<ActionTask>(taskC)
                .IsDependentOn("a")
                .Does(() => _sb.Append("c"));
            yield return taskC;

            var taskD = new ActionTask("d");
            new CakeTaskBuilder<ActionTask>(taskD)
                .Does(() => _sb.Append("d"));
            yield return taskD;

            var taskE = new ActionTask("e");
            new CakeTaskBuilder<ActionTask>(taskE)
                .IsDependentOn("d")
                .Does(() => _sb.Append("e"));
            yield return taskE;

            var taskF = new ActionTask("f");
            new CakeTaskBuilder<ActionTask>(taskF)
                .IsDependentOn("e")
                .Does(() => _sb.Append("f"));
            yield return taskF;

            var taskG = new ActionTask("g");
            new CakeTaskBuilder<ActionTask>(taskG)
                .IsDependentOn("a")
                .IsDependentOn("b")
                .IsDependentOn("c")
                .IsDependentOn("e")
                .IsDependentOn("f")
                .Does(() => _sb.Append("g"));
            yield return taskG;
        }
    }
}
