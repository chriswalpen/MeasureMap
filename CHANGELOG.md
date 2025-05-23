# MeasureMap Changelog
All notable changes to this project will be documented in this file.
 
The format is based on [Keep a Changelog](http://keepachangelog.com/)
and this project adheres to [Semantic Versioning](http://semver.org/).
 
## vNext
### Added
- ThreadNumber is emitted in the result
- Extension to log to the console 
- OnStartPipeline Event that is run when starting the threads to create the IExecutionContext per Thread
- OnEndPipeline Event that is run after the pipeline per thread is finished
- Settings contain a IsWarmup flag to indicate if the run is a warmup run
- CreateContext Extensionmethod on Settings to create a new IExecutionContext based on the Settings
- Write debug log when the OnPipelineStart event gets executed
- Ensure that all threads are setup and created before the first task is run
- Added Rampup to start threads delayed

## Fixed
- Get from the IExecutionContext needed the Key to be lowercase
  
## v2.2.1
### Fixed
- Throughput was not calculated correctly when using multiple threads
 
## v2.2.0
### Added
- ThreadBehaviour to define how a thread is created
- Allow benchmarks to be run on the MainThread
- OnExecuted to run a delegate after each task run
- Trace - Edit TraceOptions as delegate
 
### Changed
- Added IDisposable to IThreadSessionHandler
 
### Fixed
- Markdowntracer traced all data when using DetailPerThread
 
## v2.1.0
### Added
- ThreadBehaviour to define how a thread is created
- Allow benchmarks to be run on the MainThread
 
### Changed
- Added IDisposable to IThreadSessionHandler
 
## v2.0.2
### Added
- Added Benchmarks and samples oh how to use the BenchmarkRunner
- SetDuration on BenchmarkRunner
- Factories to easily create trace metrics
- Benchmarks to test MeasureMap features
- Samples to show how to use MeasureMap
 
### Changed
- Traces are now writen to the Logger
- Refactored Trace
- Duration is calculated from ticks
- TraceOptions uses TraceDetail enum to define the granularity of the traces
 
### Fixed
- ProfilerSettings are now passed to all elements of a session
 
## v2.0.1
### Added
- Benchmarks Trace throughput per second
- Customizable Tracer for Results
 
### Changed
- Benchmarks now Trace iterartions instead of memory used
- Complete redo of the trace output
 
### Fixed
 
## v2.0.0
### Added
- Add ThreadId and Iteration to the Tracedetails
- Wait for all threads to end
 
### Changed
- Changed Targetframework to netstandard2.0
- Changed from Task to full Threads
- Display more infos in the traces
- Use Stopwatch instead of DateTime.Now for more accuracy
 
### Fixed
 
## v1.7.0
### Added
- Set the duration that a Profilersession should run for
- Set a Interval to define the pace a task should be executed at
 
### Changed
- Updated .NET Versions to .NetStandard 2.1 and .NET Framework 4.8
 
### Fixed
