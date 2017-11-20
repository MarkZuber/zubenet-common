#tool "nuget:?package=GitVersion.CommandLine"

var target = Argument("target", "Default");

// 1. If command line parameter parameter passed, use that.
// 2. Otherwise if an Environment variable exists, use that.
var configuration =
    HasArgument("Configuration") ? Argument<string>("Configuration") :
    EnvironmentVariable("Configuration") != null ? EnvironmentVariable("Configuration") : "Release";

// The build number to use in the version number of the built NuGet packages.
// There are multiple ways this value can be passed, this is a common pattern.
// 1. If command line parameter parameter passed, use that.
// 2. Otherwise if running on AppVeyor, get it's build number.
// 3. Otherwise if running on Travis CI, get it's build number.
// 4. Otherwise if an Environment variable exists, use that.
// 5. Otherwise default the build number to 0.
var buildNumber =
    HasArgument("BuildNumber") ? Argument<int>("BuildNumber") :
    AppVeyor.IsRunningOnAppVeyor ? AppVeyor.Environment.Build.Number :
    EnvironmentVariable("BuildNumber") != null ? int.Parse(EnvironmentVariable("BuildNumber")) : 0;

// A directory path to an Artifacts directory.
var artifactsDirectory = Directory("./Artifacts");
 
Task("Clean")
    .Does(() =>
{
    CleanDirectory(artifactsDirectory);
});

Task("Restore")
    .IsDependentOn("Clean")
    .Does(() =>
{
    DotNetCoreRestore("./src/zubenet.common.sln");
});

Task("Build")
    .IsDependentOn("Restore")
    .Does(() =>
{
    var projects = GetFiles("./**/*.csproj");
    foreach(var project in projects)
    {
        DotNetCoreBuild(
            project.GetDirectory().FullPath,
            new DotNetCoreBuildSettings()
            {
                Configuration = configuration
            });
    }
});

Task("Test")
    .IsDependentOn("Build")
    .Does(() =>
{
    var projects = GetFiles("./Tests/**/*.csproj");
    foreach(var project in projects)
    {
        DotNetCoreTest(
            project.GetDirectory().FullPath,
            new DotNetCoreTestSettings()
            {
                ArgumentCustomization = args => args
                    .Append("-xml")
                    .Append(artifactsDirectory.Path.CombineWithFilePath(project.GetFilenameWithoutExtension()).FullPath + ".xml"),
                Configuration = configuration,
                NoBuild = true
            });
    }
});

// Run dotnet pack to produce NuGet packages from our projects. Versions the package
// using the build number argument on the script which is used as the revision number 
// (Last number in 1.0.0.0). The packages are dropped in the Artifacts directory.
Task("Pack")
    .IsDependentOn("Test")
    .Does(() =>
    {
        // var revision = "a" + buildNumber.ToString("D4");
        foreach (var project in GetFiles("./src/**/*.csproj"))
        {
            DotNetCorePack(
                project.GetDirectory().FullPath,
                new DotNetCorePackSettings()
                {
                    Configuration = configuration,
                    OutputDirectory = artifactsDirectory,
                    // VersionSuffix = revision
                });
        }
    });

Task("Default")
    .IsDependentOn("Pack");

RunTarget(target);
