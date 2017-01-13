# Cake.Parallel

This module overrides the original cake engine to run tasks in parallel.

## Installation
Download the `Cake.Parallel.Module.dll` file from the releases and drop
it into your cake modules directory. This is typically the `/tools/modules`
directory that you may have to create, unless you have configured otherwise.
Your cake build should recognize the module and run it instead.

## Compatibility
This project has been built with `Cake.Core v0.15.0`. If you find your build
is throwing exceptions around methods not being implemented please check
what version of `Cake.Core` you've pulled down during your build.

## Caveats
This module heavily relies on your task dependencies to correctly form the
graph. Please ensure all your tasks have all their necessary `IsDependentOn()`
calls.
