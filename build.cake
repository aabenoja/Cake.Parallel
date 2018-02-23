var paketPath = ".paket/paket.exe";
var isPaketInstalled = FileExists(paketPath);

var target = Argument("target", "default");

Task("restore")
  .Does(() =>
  {
    DotNetCoreRestore("./src/");
  });

Task("compile")
  .IsDependentOn("restore")
  .Does(() =>
  {
    MSBuild("./src/Cake.Parallel.sln");
  });

Task("compile-release")
  .IsDependentOn("restore")
  .Does(() =>
  {
    MSBuild("./src/Cake.Parallel.sln", new MSBuildSettings {
      Configuration = "Release"
    });
  });

Task("pack")
  .IsDependentOn("compile-release")
  .Does(() =>
  {
    DotNetCorePack("./src/Cake.Parallel/Cake.Parallel.Module.csproj", new DotNetCorePackSettings
    {
      Configuration = "Release",
      OutputDirectory = "./"
    });
  });

Task("xUnit")
  .IsDependentOn("compile")
  .Does(() =>
  {
    DotNetCoreTest("./src/Cake.Parallel.Tests/");
  });

Task("Default")
  .IsDependentOn("xUnit")
  .Does(() => {});

RunTarget(target);
