// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// originally copied from https://github.com/cake-build/cake/blob/b5de9ae6219510330f6926b9e779307be88ff669/src/Cake.Core/CakeEngine.cs

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Cake.Core;
using Cake.Core.Diagnostics;

namespace Cake.Parallel.Module
{
    public class ParallelCakeEngine : ICakeEngine
    {
        private readonly List<CakeTask> _tasks;
        private readonly ICakeLog _logger;
        private Action<ICakeContext> _setupAction;
        private Action<ITeardownContext> _teardownAction;
        private Action<ITaskSetupContext> _taskSetupAction;
        private Action<ITaskTeardownContext> _taskTeardownAction;

        public ParallelCakeEngine(ICakeLog logger)
        {
            logger.Warning("PARALLELIZER IS A WORK IN PROGRESS! YOU HAVE BEEN WARNED");

            _logger = logger;
            _tasks = new List<CakeTask>();
        }

        public CakeTaskBuilder<ActionTask> RegisterTask(string name)
        {
            if (_tasks.Any(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                const string format = "Another task with the name '{0}' has already been added.";
                throw new CakeException(string.Format(CultureInfo.InvariantCulture, format, name));
            }

            var task = new ActionTask(name);
            _tasks.Add(task);

            return new CakeTaskBuilder<ActionTask>(task);
        }

        public void RegisterSetupAction(Action<ICakeContext> action)
        {
            _setupAction = action;
        }

        public void RegisterTeardownAction(Action<ITeardownContext> action)
        {
            _teardownAction = action;
        }

        public CakeReport RunTarget(ICakeContext context, IExecutionStrategy strategy, string target)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }
            if (strategy == null)
            {
                throw new ArgumentNullException(nameof(strategy));
            }

            var graph = ParallelGraphBuilder.Build(_tasks);

            // Make sure target exist.
            if (!graph.Exist(target))
            {
                const string format = "The target '{0}' was not found.";
                throw new CakeException(string.Format(CultureInfo.InvariantCulture, format, target));
            }

            // This isn't pretty, but we need to keep track of exceptions thrown
            // while running a setup action, or a task. We do this since we don't
            // want to throw teardown exceptions if an exception was thrown previously.
            var exceptionWasThrown = false;
            Exception thrownException = null;

            try
            {
                performSetup(strategy, context);

                var report = new CakeReport();


                var targetTask = graph.Traverse(target, (taskName, cts) =>
                {
                    if (cts.IsCancellationRequested) return;

                    var task = _tasks.FirstOrDefault(_ => _.Name.Equals(taskName, StringComparison.OrdinalIgnoreCase));
                    Debug.Assert(task != null, "Node should not be null");

                    var isTarget = task.Name.Equals(target, StringComparison.OrdinalIgnoreCase);

                    if (shouldExecuteTask(context, task, isTarget))
                    {
                        executeTask(context, strategy, cts, task, report);
                    }
                    else
                    {
                        skipTask(context, strategy, task, report);
                    }
                });

                var stopwatch = Stopwatch.StartNew();
                targetTask.Wait();
                _logger.Information($"All tasks completed in {stopwatch.Elapsed}");

                return report;
            }
            catch (TaskCanceledException)
            {
                exceptionWasThrown = true;
                throw;
            }
            catch(AggregateException ex)
            {
                exceptionWasThrown = true;
                thrownException = ex.InnerException;
                throw;
            }
            catch (Exception ex)
            {
                exceptionWasThrown = true;
                thrownException = ex;
                throw;
            }
            finally
            {
                performTeardown(strategy, context, exceptionWasThrown, thrownException);
            }
        }

        public void RegisterTaskSetupAction(Action<ITaskSetupContext> action)
        {
            _taskSetupAction = action;
        }

        public void RegisterTaskTeardownAction(Action<ITaskTeardownContext> action)
        {
            _taskTeardownAction = action;
        }

        private void performSetup(IExecutionStrategy strategy, ICakeContext context)
        {
            publishEvent(Setup, new SetupEventArgs(context));
            if (_setupAction != null)
            {
                strategy.PerformSetup(_setupAction, context);
            }
        }

        private void performTeardown(IExecutionStrategy strategy, ICakeContext context, bool exceptionWasThrown, Exception thrownException)
        {
            var teardownContext = new TeardownContext(context, thrownException);
            publishEvent(Teardown, new TeardownEventArgs(teardownContext));
            if (_teardownAction != null)
            {
                try
                {
                    strategy.PerformTeardown(_teardownAction, teardownContext);
                }
                catch (Exception ex)
                {
                    _logger.Error("An error occurred in the custom teardown action.");
                    if (!exceptionWasThrown)
                    {
                        // If no other exception was thrown, we throw this one.
                        throw;
                    }
                    _logger.Error("Teardown error: {0}", ex.ToString());
                }
            }
        }

        private void publishEvent<T>(EventHandler<T> eventHandler, T eventArgs) where T : EventArgs
        {
            if (eventHandler != null)
            {
                foreach (var @delegate in eventHandler.GetInvocationList())
                {
                    var handler = (EventHandler<T>) @delegate;
                    try
                    {
                        handler(this, eventArgs);
                    }
                    catch (Exception e)
                    {
                        _logger.Error($"An error occurred in the event handler {handler.GetMethodInfo().Name}: {e.Message}");
                        throw;
                    }
                }
            }
        }

        private bool shouldExecuteTask(ICakeContext context, CakeTask task, bool isTarget)
        {
            foreach (var criteria in task.Criterias)
            {
                if (!criteria(context))
                {
                    if (!isTarget) return false;

                    throw new CakeException($"Could not reach target '{task.Name}' since it was skipped due to a criteria");
                }
            }
            return true;
        }

        private void executeTask(ICakeContext context, IExecutionStrategy strategy, CancellationTokenSource cts, CakeTask task, CakeReport report)
        {
            _logger.Verbose($"Starting task {task.Name}");
            var stopwatch = Stopwatch.StartNew();

            performTaskSetup(context, strategy, task, false);

            var execptionWasThrown = false;
            try
            {
                strategy.Execute(task, context);
            }
            catch (TaskCanceledException)
            {
                execptionWasThrown = true;
                throw;
            }
            catch (Exception exception)
            {
                execptionWasThrown = true;
                _logger.Error($"An error occurred when executing task '{task.Name}'.");

                if (task.ErrorReporter != null)
                {
                    reportErrors(strategy, task.ErrorReporter, exception);
                }

                if (task.ErrorHandler != null)
                {
                    handleErrors(strategy, task.ErrorHandler, exception);
                }
                else
                {
                    cts.Cancel();
                    throw;
                }
            }
            finally
            {
                strategy.InvokeFinally(task.FinallyHandler);

                performTaskTeardown(context, strategy, task, stopwatch.Elapsed, false, execptionWasThrown);
            }

            if (isDelegatedTask(task))
            {
                report.AddDelegated(task.Name, stopwatch.Elapsed);
            }
            else
            {
                report.Add(task.Name, stopwatch.Elapsed);
            }
        }

        private void skipTask(ICakeContext context, IExecutionStrategy strategy, CakeTask task, CakeReport report)
        {
            performTaskSetup(context, strategy, task, true);
            strategy.Skip(task);
            performTaskTeardown(context, strategy, task, TimeSpan.Zero, true, false);

            report.AddSkipped(task.Name);
        }

        private bool isDelegatedTask(CakeTask task)
        {
            var actionTask = task as ActionTask;

            return actionTask != null && !actionTask.Actions.Any();
        }

        private void reportErrors(IExecutionStrategy strategy, Action<Exception> errorReporter, Exception taskException)
        {
            try
            {
                strategy.ReportErrors(errorReporter, taskException);
            }
            catch { }
        }

        private void handleErrors(IExecutionStrategy strategy, Action<Exception> errorHandler, Exception exception)
        {
            try
            {
                strategy.HandleErrors(errorHandler, exception);
            }
            catch (Exception errorHandlerException)
            {
                if (errorHandlerException != exception)
                {
                    _logger.Error($"Error: {exception.Message}");
                }
                throw;
            }
        }

        public void performTaskSetup(ICakeContext context, IExecutionStrategy strategy, CakeTask task, bool skipped)
        {
            var taskSetupContext = new TaskSetupContext(context, task);
            publishEvent(TaskSetup, new TaskSetupEventArgs(taskSetupContext));

            if (_taskSetupAction != null)
            {
                try
                {
                    strategy.PerformTaskSetup(_taskSetupAction, taskSetupContext);
                }
                catch
                {
                    performTaskTeardown(context, strategy, task, TimeSpan.Zero, skipped, true);
                    throw;
                }
            }
        }

        public void performTaskTeardown(ICakeContext context, IExecutionStrategy strategy, CakeTask task, TimeSpan duration, bool skipped, bool exceptionWasThrown)
        {
            var taskTeardownContext = new TaskTeardownContext(context, task, duration, skipped);
            publishEvent(TaskTeardown, new TaskTeardownEventArgs(taskTeardownContext));

            if (_taskTeardownAction != null)
            {
                try
                {
                    strategy.PerformTaskTeardown(_taskTeardownAction, taskTeardownContext);
                }
                catch (Exception ex)
                {
                    _logger.Error($"An error occurred in the custom task teardown action ({task.Name}).");
                    if (!exceptionWasThrown)
                    {
                        // If no other exception was thrown, we throw this one.
                        throw;
                    }
                    _logger.Error($"Task Teardown error ({task.Name}): {ex}");
                }
            }
        }

        public IReadOnlyList<CakeTask> Tasks => _tasks;
        public event EventHandler<SetupEventArgs> Setup;
        public event EventHandler<TeardownEventArgs> Teardown;
        public event EventHandler<TaskSetupEventArgs> TaskSetup;
        public event EventHandler<TaskTeardownEventArgs> TaskTeardown;
    }
}
