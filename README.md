# Cake.Parallel
[![Build status](https://ci.appveyor.com/api/projects/status/fsap4e20blw55cb0/branch/master?svg=true)](https://ci.appveyor.com/project/aabenoja/cake-parallel/branch/master)

This module overrides the original cake engine to run tasks in parallel.

## Installation
Download the `Cake.Parallel.Module.dll` file from the releases and drop
it into your cake modules directory. This is typically the `/tools/modules`
directory that you may have to create, unless you have configured otherwise.
Your cake build should recognize the module and run it instead.

Please ensure all your tasks have the correct `IsDependent()` chains. This
ensures when the parallelizer goes through the dependency graph the correct
tasks are scheduled.

## Compatibility
This project has been built with `Cake v0.25.0`. If you find your build
is throwing exceptions around methods not being implemented please check
what version of `Cake` you've pulled down during your build.
