﻿using System;
using MeasureMap.Diagnostics;
using MeasureMap.Runners;
using MeasureMap.TaskHandlers;

namespace MeasureMap
{
    /// <summary>
    /// Extension class for ProfilerSession
    /// </summary>
    public static class ProfilerSessionExtensions
    {
        /// <summary>
        /// Sets the Task that will be profiled
        /// </summary>
        /// <param name="session">The current session</param>
        /// <param name="task">The Task</param>
        /// <returns>The current profiling session</returns>
        public static ProfilerSession Task(this ProfilerSession session, Action task)
        {
            session.Task(new Task(task));

            return session;
        }

        /// <summary>
        /// Sets the Task that will be profiled
        /// </summary>
        /// <param name="session">The current session</param>
        /// <typeparam name="T">The return and parameter value</typeparam>
        /// <param name="task">The task to execute</param>
        /// <returns>The current profiling session</returns>
        public static ProfilerSession Task<T>(this ProfilerSession session, Func<T, T> task)
        {
            session.Task(new Task<T>(task));

            return session;
        }

        /// <summary>
        /// Sets the Task that will be profiled
        /// </summary>
        /// <param name="session">The current session</param>
        /// <typeparam name="T">The return and parameter value</typeparam>
        /// <param name="task">The task to execute</param>
        /// <param name="parameter">The parameter that is passed to the task</param>
        /// <returns>The current profiling session</returns>
        public static ProfilerSession Task<T>(this ProfilerSession session, Func<T, T> task, T parameter)
        {
            session.Task(new Task<T>(task, parameter));

            return session;
        }

        /// <summary>
        /// Sets the Task that will be profiled passing the current ExecutionContext as parameter
        /// </summary>
        /// <param name="session">The current session</param>
        /// <param name="task">The task to execute</param>
        /// <returns>The current profiling session</returns>
        public static ProfilerSession Task(this ProfilerSession session, Action<IExecutionContext> task)
        {
            session.Task(new ContextTask(task));

            return session;
        }

        /// <summary>
        /// Sets the Task that will be profiled passing the current ExecutionContext as parameter
        /// </summary>
        /// <typeparam name="T">The expected task output</typeparam>
        /// <param name="session">The current session</param>
        /// <param name="task">The task to execute</param>
        /// <returns>The current profiling session</returns>
        public static ProfilerSession Task<T>(this ProfilerSession session, Func<IExecutionContext, T> task)
        {
            session.Task(new OutputTask<T>(task));

            return session;
        }

        /// <summary>
        /// Sets the amount of threads that the profiling sessions should run in.
        /// All iterations are run on every thread.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="thredCount">The amount of threads that the task is run on</param>
        /// <returns>The current profiling session</returns>
        public static ProfilerSession SetThreads(this ProfilerSession session, int thredCount)
        {
            session.SetExecutionHandler(new MultyThreadSessionHandler(thredCount));

            return session;
        }

        /// <summary>
        /// Sets the amount of threads that the profiling sessions should run in.
        /// All iterations are run on every thread.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="thredCount">The amount of threads that the task is run on</param>
        /// <param name="rampupTime">The time it takes to setup all threads</param>
        /// <returns>The current profiling session</returns>
        public static ProfilerSession SetThreads(this ProfilerSession session, int thredCount, TimeSpan rampupTime)
        {
            session.SetExecutionHandler(new MultyThreadSessionHandler(thredCount, rampupTime));

            return session;
        }

        /// <summary>
        /// Set the <see cref="ThreadBehaviour"/> to the settings of the <see cref="ProfilerSession"/>
        /// </summary>
        /// <param name="session"></param>
        /// <param name="behaviour"></param>
        /// <returns></returns>
        public static ProfilerSession SetThreadBehaviour(this ProfilerSession session, ThreadBehaviour behaviour)
        {
            session.Settings.ThreadBehaviour = behaviour;

            if (behaviour == ThreadBehaviour.RunOnMainThread)
            {
                session.SetExecutionHandler(new MainThreadSessionHandler());
            }

            return session;
        }

        /// <summary>
        /// Sets the amount of iterations that the profileing session should run the task
        /// </summary>
        /// <param name="session">The current session</param>
        /// <param name="iterations">The iterations to run the task</param>
        /// <returns>The current profiling session</returns>
        public static ProfilerSession SetIterations(this ProfilerSession session, int iterations)
        {
            session.Settings.Iterations = iterations;

            return session;
        }

        /// <summary>
        /// Sets the duration that the profileing session should run the task for
        /// </summary>
        /// <param name="session">The current session</param>
        /// <param name="duration">The iterations to run the task</param>
        /// <returns>The current profiling session</returns>
        public static ProfilerSession SetDuration(this ProfilerSession session, TimeSpan duration)
        {
            session.Settings.Duration = duration;

            return session;
        }

        /// <summary>
        /// The task will be executed at the given interval.
        /// To ensure the execution interval, the task is executed in a new thread
        /// </summary>
        /// <param name="session"></param>
        /// <param name="interval"></param>
        /// <returns></returns>
        public static ProfilerSession SetInterval(this ProfilerSession session, TimeSpan interval)
        {
            session.Settings.Execution = new TimedTaskExecution(interval);

            return session;
        }

        /// <summary>
        /// Defines if the task is initially run as a warmup. Defaults to true
        /// </summary>
        /// <param name="session"></param>
        /// <param name="run"></param>
        /// <returns></returns>
        public static ProfilerSession RunWarmup(this ProfilerSession session, bool run)
        {
            session.Settings.RunWarmup = run;

            return session;
        }

        /// <summary>
        /// Sets the settings that the profiler should use
        /// </summary>
        /// <param name="session">The current session</param>
        /// <param name="settings">The settings for thr profiler</param>
        /// <returns>The current profiling session</returns>
        public static ProfilerSession SetSettings(this ProfilerSession session, ProfilerSettings settings)
        {
            settings.MergeChangesTo(session.Settings);
            
            if(session.Settings.ThreadBehaviour != ThreadBehaviour.Thread)
            {
                // ensure the ThreadBehaviour is set when using the new settings
                // this is only set when using the default ThreadBehaviour in a ProfilerSession
                session.SetThreadBehaviour(session.Settings.ThreadBehaviour);
            }

            return session;
        }

		/// <summary>
		/// Set values in the settings
		/// </summary>
		/// <param name="session">The current session</param>
		/// <param name="settings">The settings for thr profiler</param>
		/// <returns></returns>
		public static ProfilerSession Settings(this ProfilerSession session, Action<ProfilerSettings> settings)
        {
	        settings(session.Settings);

	        return session;
        }

		/// <summary>
		/// Add the middleware to the processing pipeline
		/// </summary>
		/// <param name="session">The current session</param>
		/// <param name="middleware">The middleware to add</param>
		/// <returns></returns>
		public static ProfilerSession AddMiddleware(this ProfilerSession session, ITaskMiddleware middleware)
        {
            session.ProcessingPipeline.SetNext(middleware);
            return session;
        }

        /// <summary>
        /// Add the middleware to the session pipeline
        /// </summary>
        /// <param name="session">The current session</param>
        /// <param name="middleware">The middleware to add</param>
        /// <returns></returns>
        public static ProfilerSession AddMiddleware(this ProfilerSession session, ISessionHandler middleware)
        {
            session.SessionPipeline.SetNext(middleware);
            return session;
        }

        /// <summary>
        /// Sets a Task that will be executed before each profiling task execution
        /// </summary>
        /// <param name="session">The current session</param>
        /// <param name="task">The task to execute before each profiling task</param>
        /// <returns>The current profiling session</returns>
        public static ProfilerSession PreExecute(this ProfilerSession session, Action task)
        {
            return session.AddMiddleware(new PreExecutionTaskHandler(task));
        }

        /// <summary>
        /// Sets a Task that will be executed before each profiling task execution
        /// </summary>
        /// <param name="session">The current session</param>
        /// <param name="task">The task to execute before each profiling task</param>
        /// <returns>The current profiling session</returns>
        public static ProfilerSession PreExecute(this ProfilerSession session, Action<IExecutionContext> task)
        {
            return session.AddMiddleware(new PreExecutionTaskHandler(task));
        }

        /// <summary>
        /// Sets a Task that will be executed after each profiling task execution
        /// </summary>
        /// <param name="session">The current session</param>
        /// <param name="task">The task to execute after each profiling task</param>
        /// <returns>The current profiling session</returns>
        public static ProfilerSession PostExecute(this ProfilerSession session, Action task)
        {
            return session.AddMiddleware(new PostExecutionTaskHandler(task));
        }

        /// <summary>
        /// Sets a Task that will be executed after each profiling task execution
        /// </summary>
        /// <param name="session">The current session</param>
        /// <param name="task">The task to execute after each profiling task</param>
        /// <returns>The current profiling session</returns>
        public static ProfilerSession PostExecute(this ProfilerSession session, Action<IExecutionContext> task)
        {
            return session.AddMiddleware(new PostExecutionTaskHandler(task));
        }

        /// <summary>
        /// Add a delay before each task gets executed. The delay is not countet to the execution time of the task
        /// </summary>
        /// <param name="session"></param>
        /// <param name="duration"></param>
        /// <returns></returns>
        public static ProfilerSession AddDelay(this ProfilerSession session, TimeSpan duration)
        {
            return session.AddMiddleware(new DelayTaskHandler(duration));
        }

        /// <summary>
        /// Adds a setup task to the sessionpipeline
        /// </summary>
        /// <param name="session">The current session</param>
        /// <param name="setup">The setuptask</param>
        /// <returns>The current profiling session</returns>
        public static ProfilerSession Setup(this ProfilerSession session, Action setup)
        {
            return session.AddMiddleware(new PreExecutionSessionHandler(setup));
        }

        /// <summary>
        /// Defines the minimal <see cref="LogLevel"/>. All higher levels are writen to the log
        /// </summary>
        /// <param name="session"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        public static ProfilerSession SetMinLogLevel(this ProfilerSession session, LogLevel level)
        {
            session.Settings.Logger.MinLogLevel = level;
            return session;
        }

        /// <summary>
        /// Add a delegate that is run after the task is executed. It is possible to add multiple delegates by calling this method multiple times. This does the same as PostExecute with the difference that it is run async
        /// </summary>
        /// <param name="session"></param>
        /// <param name="execution"></param>
        /// <returns></returns>
        public static ProfilerSession OnExecuted(this ProfilerSession session, Action<IIterationResult> execution)
        {
            session.AddMiddleware(new OnExecutedTaskHandler(execution));
            return session;
        }

        /// <summary>
        /// Event that is executed at the start of each thread run
        /// </summary>
        /// <param name="session"></param>
        /// <param name="event"></param>
        /// <returns></returns>
        public static ProfilerSession OnStartPipeline(this ProfilerSession session, Func<ProfilerSettings, IExecutionContext> @event)
        {
            session.Settings.OnStartPipelineEvent = @event;
            return session;
        }

        /// <summary>
        /// Event that is executed at the end of each thread run
        /// </summary>
        /// <param name="session"></param>
        /// <param name="event"></param>
        /// <returns></returns>
        public static ProfilerSession OnEndPipeline(this ProfilerSession session, Action<IExecutionContext> @event)
        {
            session.Settings.OnEndPipelineEvent = @event;
            return session;
        }

        internal static ProfilerSession AppendSettings(this ProfilerSession session, ProfilerSettings settings)
        {
            session.SetMinLogLevel(settings.Logger.MinLogLevel);
            return session;
        }
    }
}
