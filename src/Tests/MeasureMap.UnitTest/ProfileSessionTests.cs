﻿using System;
using System.Diagnostics;
using NUnit.Framework;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using MeasureMap.Diagnostics;
using MeasureMap.Runners;
using Polaroider;
using MeasureMap.UnitTest.Tracers;
using MeasureMap.Tracers;
using Moq;

namespace MeasureMap.UnitTest
{
    [SingleThreaded]
    public class ProfileSessionTests
    {
        [SetUp]
        public void Setup()
        {
            MeasureMap.Tracers.TraceOptions.Default.Tracer = new MarkDownTracer();
        }

        [Test]
        public void ProfileSession_StartSessionTest()
        {
            var session = ProfilerSession.StartSession();

            session.Should().NotBeNull();
        }

        [Test]
        public void ProfileSession_WithoutSetIterations()
        {
            var session = ProfilerSession.StartSession();

            session.Settings.Iterations.Should().Be(1);
        }

        [Test]
        public void ProfileSession_SetIterations()
        {
            var session = ProfilerSession.StartSession()
                .SetIterations(12);

            session.Settings.Iterations.Should().Be(12);
        }

        [Test]
        public void ProfileSession_SetDuration()
        {
            var session = ProfilerSession.StartSession()
                .SetDuration(TimeSpan.FromSeconds(1));

            session.Settings.Duration.Should().Be(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void ProfileSession_AddTask()
        {
            var result = string.Empty;
            var session = ProfilerSession.StartSession()
                .Task(() => result = "passed");

            // TODO: is it neccesary to run the session just to check if a task is set???
            session.RunSession();

            result.Should().Be("passed");
        }

        [Test]
        public void ProfileSession_RunSessionOnce()
        {
            int count = 0;
            var result = ProfilerSession.StartSession()
                .Task(() => count++)
                .RunSession();

            // the task is rune once more to be able to initialize properly
            result.Iterations.Count().Should().Be(count - 1);
        }

        [Test]
        public void ProfileSession_RunSessionMultipleTimes()
        {
            int count = 0;
            var result = ProfilerSession.StartSession()
                .Task(() => count++)
                .SetIterations(20)
                .RunSession();

            // the task is rune once more to be able to initialize properly
            result.Iterations.Count().Should().Be(count - 1);
        }

        [Test]
        public void ProfileSession_AverageMillisecond()
        {
            var result = ProfilerSession.StartSession()
                .Task(Task)
                .SetIterations(200)
                .RunSession();

            result.AverageMilliseconds().Should().BeGreaterThan(0);
        }

        [Test]
        public void ProfileSession_AverageTicks()
        {
            var result = ProfilerSession.StartSession()
                .Task(Task)
                .SetIterations(200)
                .RunSession();

            result.AverageTicks.Should().BeGreaterThan(0);
        }

        [Test]
        public void ProfileSession_Throughput()
        {
            var result = ProfilerSession.StartSession()
                .Task(()=>{})
                .SetDuration(TimeSpan.FromSeconds(2))
                .RunSession();

            result.Throughput().Should().BeGreaterThan(10);
        }

        [Test]
        public void ProfileSession_TrueCondition()
        {
            ProfilerSession.StartSession()
                .Task(Task)
                .Assert(pr => pr.Iterations.Count() == 1)
                .RunSession();
        }

        [Test]
        public void ProfileSession_MultipleTrueCondition()
        {
            ProfilerSession.StartSession()
                .Task(Task)
                .Assert(pr => pr.Iterations.Count() == 1)
                .Assert(pr => pr.AverageMilliseconds() > 0)
                .RunSession();
        }

        [Test]
        public void ProfileSession_FalseCondition()
        {
            Assert.Throws<MeasureMap.AssertionException>(() => ProfilerSession.StartSession()
                .Task(Task)
                .SetIterations(2)
                .Assert(pr => pr.Iterations.Count() == 1)
                .RunSession());
        }

        [Test]
        public void ProfileSession_WithoutTask()
        {
            Assert.Throws<ArgumentException>(() => ProfilerSession.StartSession()
                .RunSession());
        }

        [Test]
        public void ProfileSession_Trace()
        {
            var writer = new StringResultWriter();
            ProfilerSession.StartSession()
                .Task(Task)
                .SetIterations(10)
                .RunSession()
                .Trace(writer);

            writer.Value.Should().Contain("Duration");
            writer.Value.Should().Contain("Total Time");
            writer.Value.Should().Contain("Avg. Time");
            writer.Value.Should().Contain("Memory init size");
            writer.Value.Should().Contain("Memory end size");
            writer.Value.Should().Contain("Memory increase");
        }

        [Test]
        public void ProfileSession_Trace_Detail()
        {
            System.Threading.Tasks.Task.Delay(5000).Wait();

            var writer = new StringResultWriter();
            var options = new MeasureMap.Tracers.TraceOptions
            {
                ResultWriter = writer,
                TraceDetail = TraceDetail.FullDetail
            };

            ProfilerSession.StartSession()
                .Task(Task)
                .SetIterations(5)
                .RunSession()
                .Trace(options);

            Regex.Matches(writer.Value, " \\| 1         \\| ").Count.Should().Be(1);
            Regex.Matches(writer.Value, " \\| 2         \\| ").Count.Should().Be(1);
            Regex.Matches(writer.Value, " \\| 3         \\| ").Count.Should().Be(1);
            Regex.Matches(writer.Value, " \\| 4         \\| ").Count.Should().Be(1);
            Regex.Matches(writer.Value, " \\| 5         \\| ").Count.Should().Be(1);
        }

        [Test]
        public void ProfileSession_Trace_MultipleThreads_Detail()
        {
            System.Threading.Tasks.Task.Delay(5000).Wait();

            var writer = new StringResultWriter();
            var options = new MeasureMap.Tracers.TraceOptions
            {
                ResultWriter = writer,
                TraceDetail = TraceDetail.FullDetail
            };

            ProfilerSession.StartSession()
                .Task(Task)
                .SetThreads(3)
                .SetIterations(5)
                .RunSession()
                .Trace(options);

            Regex.Matches(writer.Value, " \\| 1         \\| ").Count.Should().Be(3);
            Regex.Matches(writer.Value, " \\| 2         \\| ").Count.Should().Be(3);
            Regex.Matches(writer.Value, " \\| 3         \\| ").Count.Should().Be(3);
            Regex.Matches(writer.Value, " \\| 4         \\| ").Count.Should().Be(3);
            // 3 in thread details and 3 in full trace
            Regex.Matches(writer.Value, " \\| 5         \\| ").Count.Should().Be(3);
        }

        [Test]
        public void ProfileSession_Fastest()
        {
            var result = ProfilerSession.StartSession()
                .Task(c =>
                {
                    var i = c.Get<int>(ContextKeys.Iteration);
                    if (i > 1)
                    {
                        System.Threading.Thread.Sleep(TimeSpan.FromSeconds(0.5));
                    }

                    return i;
                })
                .SetIterations(10)
                .RunSession();

            Assert.That((int)result.Fastest.Data == 1);
        }

        [Test]
        public void ProfileSession_Slowest()
        {
            var result = ProfilerSession.StartSession()
                .Task(c =>
                {
                    var i = c.Get<int>(ContextKeys.Iteration);
                    if (i == 9)
                    {
                        System.Threading.Thread.Sleep(TimeSpan.FromSeconds(0.5));
                    }

                    return i;
                })
                .SetIterations(10)
                .RunSession();

            Assert.That((int)result.Slowest.Data == 9);
        }

        [Test]
        public void ProfileSession_ContextTask()
        {
            var result = ProfilerSession.StartSession()
                .Task(c =>
                {
                    var iteration = c.Get<int>(ContextKeys.Iteration);
                    if (iteration == 9)
                    {
                        System.Threading.Thread.Sleep(TimeSpan.FromSeconds(0.5));
                    }
                })
                .SetIterations(10)
                .RunSession();
            
            Assert.That(result.Slowest.Iteration == 9);
        }

        [Test]
        public void ProfileSession_OutputTask()
        {
            var result = ProfilerSession.StartSession()
                .Task(c =>
                {
                    var iteration = c.Get<int>(ContextKeys.Iteration);
                    if (iteration == 9)
                    {
                        System.Threading.Thread.Sleep(TimeSpan.FromSeconds(0.5));
                    }

                    return iteration;
                })
                .SetIterations(10)
                .RunSession();

            Assert.That((int)result.Slowest.Data == 9);
        }

        [Test]
        public void ProfileSession_SetIterations_Runner()
        {
            var session = ProfilerSession.StartSession()
                .SetIterations(1);

            session.Settings.Runner.Should().BeOfType<IterationRunner>();
        }

        [Test]
        public void ProfileSession_SetDuration_Runner()
        {
            var session = ProfilerSession.StartSession()
                .SetDuration(TimeSpan.FromSeconds(1));

            session.Settings.Runner.Should().BeOfType<DurationRunner>();
        }

        [Test]
        public void ProfileSession_SetDuration_CheckTime()
        {
            var sw = new Stopwatch();
            sw.Start();
            
            ProfilerSession.StartSession()
                .SetDuration(TimeSpan.FromSeconds(5))
                .Task(() => { })
                .RunSession();
            
            sw.Stop();

            sw.Elapsed.Should().BeGreaterThan(TimeSpan.FromSeconds(5)).And.BeLessThan(TimeSpan.FromSeconds(5.5));
        }

        [Test]
        public void ProfileSession_SetDuration_SetInterval_CheckTime()
        {
            var sw = new Stopwatch();
            sw.Start();

            ProfilerSession.StartSession()
                .SetDuration(TimeSpan.FromSeconds(5))
                .SetInterval(TimeSpan.FromMilliseconds(5))
                .Task(() => { })
                .RunSession();

            sw.Stop();

            sw.Elapsed.Should().BeGreaterThan(TimeSpan.FromSeconds(5));
        }

        [Test]
        [Explicit]
        public void ProfileSession_SetDuration_SetInterval_CheckTime_UpperBOunds()
        {
            var sw = new Stopwatch();
            sw.Start();

            ProfilerSession.StartSession()
                .SetDuration(TimeSpan.FromSeconds(5))
                .SetInterval(TimeSpan.FromMilliseconds(5))
                .Task(() => { })
                .RunSession();

            sw.Stop();

            sw.Elapsed.Should().BeGreaterThan(TimeSpan.FromSeconds(5)).And.BeLessThan(TimeSpan.FromSeconds(5.5));
        }

        [Test]
        public void ProfileSession_SetDuration_Trace_TotalTime()
        {
            var writer = new StringResultWriter();
            var result = ProfilerSession.StartSession()
                .SetDuration(TimeSpan.FromSeconds(1))
                .Task(() => { })
                .RunSession();

            result.Trace(writer);
            writer.Value.Split(new[]{ Environment.NewLine }, StringSplitOptions.None).Should().Contain(l => l.Contains("Duration") && l.Contains("00:00:01."));
        }

        [Test]
        public void ProfileSession_SetDuration_TotalTime()
        {
            var result = ProfilerSession.StartSession()
                .SetDuration(TimeSpan.FromSeconds(1))
                .Task(() => { })
                .RunSession();

            result.Elapsed().Should().BeGreaterThan(TimeSpan.FromSeconds(1)).And.BeLessThan(TimeSpan.FromSeconds(1.5));
        }

        [Test]
        public void ProfileSession_SetDuration_Iterations()
        {
            var cnt = 0;
            var result = ProfilerSession.StartSession()
                .SetDuration(TimeSpan.FromSeconds(1))
                .Task(() => { cnt++; })
                .RunSession();

            result.Iterations.Count().Should().BeGreaterThan(10);
        }

        [Test]
        public void ProfileSession_SetDuration_TaskExecutions()
        {
            var cnt = 0;
            ProfilerSession.StartSession()
                .SetDuration(TimeSpan.FromSeconds(1))
                .Task(() => { cnt++; })
                .RunSession();

            cnt.Should().BeGreaterThan(10);
        }

        [Test]
        public void ProfileSession_SetOptioins_After_Iterations()
        {
            var session = ProfilerSession.StartSession()
                .SetIterations(10)
                .SetSettings(new ProfilerSettings());

            session.Settings.Iterations.Should().Be(10);
        }

        [Test]
        public void ProfileSession_SetOptioins_After_Duration()
        {
            var session = ProfilerSession.StartSession()
                .SetDuration(TimeSpan.FromSeconds(1))
                .SetSettings(new ProfilerSettings());

            session.Settings.Duration.Should().Be(TimeSpan.FromSeconds(1));
        }

        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        public void ProfileSession_Delay(int delay)
        {
            var session = ProfilerSession.StartSession()
                .SetIterations(2)
                .RunWarmup(false)
                .AddDelay(TimeSpan.FromSeconds(delay))
                .Task(() => { });

            var sw = new Stopwatch();
            sw.Start();
            session.RunSession();

            sw.Stop();
            sw.Elapsed.Should().BeGreaterThan(TimeSpan.FromSeconds(delay * 2)).And.BeLessThan(TimeSpan.FromSeconds((delay * 2) + .5));
        }

        [Test]
        public void ProfileSession_Delay_TimePerIteration()
        {
            var result = ProfilerSession.StartSession()
                .SetIterations(2)
                .RunWarmup(false)
                .AddDelay(TimeSpan.FromSeconds(1))
                .Task(() => { })
                .RunSession();

            result.Iterations.Should().OnlyContain(i => i.Duration < TimeSpan.FromSeconds(1));
        }

        [Test]
        public void ProfileSession_Executino_Default()
        {
            var session = ProfilerSession.StartSession()
                .Task(c => { });

            session.Settings.Execution.Should().BeOfType<SimpleTaskExecution>();
        }

        [Test]
        public void ProfileSession_Interval()
        {
            var session = ProfilerSession.StartSession()
                .Task(c => { })
                .SetInterval(TimeSpan.FromSeconds(.5));

            session.Settings.Execution.Should().BeOfType<TimedTaskExecution>();
        }

        [Test]
        public void ProfileSession_Interval_Integration()
        {
            var result = ProfilerSession.StartSession()
                .Task(c =>
                {
                    var i = c.Get<int>(ContextKeys.Iteration);
                    Trace.WriteLine(DateTime.Now);
                    return i;
                })
                .SetIterations(10)
                .SetInterval(TimeSpan.FromSeconds(.5))
                .RunSession();

            System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(1)).Wait();

            result.Iterations.Should().HaveCount(10);
        }

        [Test]
        public void ProfileSession_Interval_Duration()
        {
            var result = ProfilerSession.StartSession()
                .Task(c =>
                {
                    var i = c.Get<int>(ContextKeys.Iteration);
                    return i;
                })
                .SetIterations(10)
                .SetInterval(TimeSpan.FromSeconds(.5))
                .RunSession();

            // 0.5*10 + some overhead = >5 and <6
            result.Elapsed().Should().BeGreaterThan(TimeSpan.FromSeconds(5));
        }

        [Test]
        [Explicit]
        public void ProfileSession_Interval_Duration_Upperbounds()
        {
            var result = ProfilerSession.StartSession()
                .Task(c =>
                {
                    var i = c.Get<int>(ContextKeys.Iteration);
                    return i;
                })
                .SetIterations(10)
                .SetInterval(TimeSpan.FromSeconds(.5))
                .RunSession();

            // 0.5*10 + some overhead = >5 and <6
            result.Elapsed().Should().BeGreaterThan(TimeSpan.FromSeconds(5)).And.BeLessThan(TimeSpan.FromSeconds(6));
        }


        [Test]
        public void ProfileSession_IterationRunner_NoDuplicateIterartion()
        {
            var result = ProfilerSession.StartSession()
                .Task(c =>
                {
                    var i = c.Get<int>(ContextKeys.Iteration);
                    Trace.WriteLine(DateTime.Now);
                    return i;
                })
                .SetIterations(10)
                .SetInterval(TimeSpan.FromSeconds(.5))
                .RunSession();

            result.Iterations.GroupBy(r => r.Iteration).All(i => i.Count() == 1).Should().BeTrue();
        }

        [Test]
        public void ProfileSession_DurationRunner_NoDuplicateIterartion()
        {
            var result = ProfilerSession.StartSession()
                .Task(c =>
                {
                    var i = c.Get<int>(ContextKeys.Iteration);
                    Trace.WriteLine(DateTime.Now);
                    return i;
                })
                .SetDuration(TimeSpan.FromSeconds(5))
                .SetInterval(TimeSpan.FromSeconds(.5))
                .RunSession();

            result.Iterations.GroupBy(r => r.Iteration).All(i => i.Count() == 1).Should().BeTrue();
        }

        [Test]
        public void ProfileSession_Iteration_Delayed_NoDuplicateIterartion()
        {
            var result = ProfilerSession.StartSession()
                .Task(c =>
                {
                    var i = c.Get<int>(ContextKeys.Iteration);
                    return i;
                })
                .SetIterations(10)
                .AddDelay(TimeSpan.FromSeconds(.5))
                .RunSession();

            result.Iterations.GroupBy(r => r.Iteration).All(i => i.Count() == 1).Should().BeTrue();
        }

        [Test]
        public void ProfileSession_Interval_Delayed_NoDuplicateIterartion()
        {
            var result = ProfilerSession.StartSession()
                .Task(c =>
                {
                    var i = c.Get<int>(ContextKeys.Iteration);
                    Trace.WriteLine(DateTime.Now);
                    return i;
                })
                .SetDuration(TimeSpan.FromSeconds(5))
                .AddDelay(TimeSpan.FromSeconds(.5))
                .RunSession();

            result.Iterations.GroupBy(r => r.Iteration).All(i => i.Count() == 1).Should().BeTrue();
        }

        [TestCase(LogLevel.Debug)]
        [TestCase(LogLevel.Info)]
        [TestCase(LogLevel.Warning)]
        [TestCase(LogLevel.Error)]
        [TestCase(LogLevel.Critical)]
        public void ProfileSession_SetMinLogLevel(LogLevel level)
        {
            ProfilerSession.StartSession()
                .SetMinLogLevel(level).Settings.Logger.MinLogLevel.Should().Be(level);
        }

        [TestCase(ThreadBehaviour.Task)]
        [TestCase(ThreadBehaviour.Thread)]
        [TestCase(ThreadBehaviour.RunOnMainThread)]
        public void ProfileSession_SetThreadBehaviour(ThreadBehaviour behaviour)
        {
            ProfilerSession.StartSession()
                .SetThreadBehaviour(behaviour).Settings.ThreadBehaviour.Should().Be(behaviour);
        }

        [Test]
        public void ProfileSession_SetExecutionHandler()
        {
            var mock = new Mock<IThreadSessionHandler>();
            mock.Setup(x => x.Execute(It.IsAny<ITask>(), It.IsAny<ProfilerSettings>())).Returns(() => new ProfilerResult());

            ProfilerSession.StartSession()
                .SetExecutionHandler(mock.Object)
                .Task(() => { })
                .RunSession();

            mock.Verify(x => x.Execute(It.IsAny<ITask>(), It.IsAny<ProfilerSettings>()));
        }

        [Test]
        public void ProfileSession_OnExecuted()
        {
            var calls = 0;

            ProfilerSession.StartSession()
                .SetIterations(5)
                .RunWarmup(false)
                .Task(() => System.Threading.Tasks.Task.Delay(50).Wait())
                .OnExecuted(r => calls++)
                .RunSession();

            //
            // OnExecuted is run async so only test if called at least once
            calls.Should().BeGreaterThan(0);
        }

        [Test]
        public void ProfileSession_OnExecuted_Warmup()
        {
            var calls = 0;

            ProfilerSession.StartSession()
                .OnExecuted(r => calls++)
                .Task(() => System.Threading.Tasks.Task.Delay(50).Wait())
                .RunSession();

            calls.Should().Be(2);
        }

        [Test]
        public void ProfileSession_OnExecuted_IIterationResult()
        {
            ProfilerSession.StartSession()
                .OnExecuted(r => r.Should().BeAssignableTo<IterationResult>())
                .Task(() => { })
                .RunSession();
        }

        [Test]
        public void ProfileSession_OnExecuted_MultipleRegistrations()
        {
            var first = 0;
            var second = 0;

            ProfilerSession.StartSession()
                .OnExecuted(r => first++)
                .OnExecuted(r => second++)
                .Task(() => System.Threading.Tasks.Task.Delay(50).Wait())
                .RunSession();

            first.Should().Be(2);
            second.Should().Be(2);
        }

        private void Task()
        {
            System.Threading.Thread.Sleep(TimeSpan.FromSeconds(0.002));
        }
    }
}
