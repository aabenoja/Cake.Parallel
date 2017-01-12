#tool "nuget:?package=xunit.runner.console"

var paketPath = ".paket/paket.exe";
var isPaketInstalled = FileExists(paketPath);

var target = Argument("target", "default");

Task("bootstrap-paket")
    .WithCriteria(!isPaketInstalled)
    .Does(() =>
    {
        if (StartProcess(".paket/paket.bootstrapper.exe") != 0)
            Error("Unable to fetch paket.exe");

        paketPath = ".paket/paket.exe";
    });

Task("paket-restore")
    .IsDependentOn("bootstrap-paket")
    .Does(() =>
    {
        if (StartProcess(paketPath, "restore") != 0)
            Error("Paket restore failed");
    });

Task("compile")
  .IsDependentOn("paket-restore")
  .Does(() =>
  {
    DotNetBuild("./src/Cake.Parallel.sln");
  });

Task("xUnit")
  .IsDependentOn("compile")
  .Does(() =>
  {
    XUnit2("./src/Cake.Parallel.Tests/bin/**/Cake.Parallel.Tests.dll");
  });

Task("Default")
  .IsDependentOn("xUnit")
  .Does(() =>
  {
    Information("It works!");
  });

RunTarget(target);
