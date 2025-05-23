﻿using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MeasureMap.UnitTest
{
    [TestFixture]
    public class MemoryProfileSessionTests
    {
        [Test]
        public void MemoryProfileSession_StartSessionTest()
        {
            var session = ProfilerSession.StartSession();

            session.Should().NotBeNull();
        }

        [Test]
        public void MemoryProfileSession_AddTask()
        {
            var session = ProfilerSession.StartSession()
                .Task(() =>
                {
                    // allocate some memory
                });

            // TODO: is it neccesary to run the session just to check if a task is set???
            session.RunSession();

            session.Should().NotBeNull();
        }

        [Test]
        public void MemoryProfileSession_RunSessionOnce()
        {
            int count = 0;
            var result = ProfilerSession.StartSession()
                .Task(() => count++)
                .RunSession();

            // the task is rune once more to be able to initialize properly
            result.Iterations.Count().Should().Be(count -1);
        }

        [Test]
        public void MemoryProfileSession_RunSessionMultipleTimes()
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
        public void MemoryProfileSession_AllocateMemory()
        {
            var result = ProfilerSession.StartSession()
                .Task(() =>
                {
                    // create some objects to allocate memory
                    var list = new List<byte[]>();
                    for (int i = 0; i < 10000; i++)
                    {
                        list.Add(new byte[1024]);
                    }
                })
                .SetIterations(100)
                .RunSession();

            result.Increase.Should().BeGreaterThan(0);
        }
    }
}
