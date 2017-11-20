#tool "nuget:?package=GitVersion.CommandLine"

GitVersion versionInfo = null;

var target = Argument("target", "Default");

// 1. If command line parameter parameter passed, use that.
// 2. Otherwise if an Environment variable exists, use that.
var configuration =
    HasArgument("Configuration") ? Argument<string>("Configuration") :
    EnvironmentVariable("Configuration") != null ? EnvironmentVariable("Configuration") : "Release";

// A directory path to an Artifacts directory.
var artifactsDirectory = Directory("./Artifacts");
 
Task("Clean")
    .Does(() =>
{
    CleanDirectory(artifactsDirectory);
});

Task("SetVersionInfo")
    .IsDependentOn("Clean")
    .Does(() =>
{
    versionInfo = GitVersion(new GitVersionSettings {
        RepositoryPath = ".",
        UpdateAssemblyInfo = true,
    });
});

Task("Restore")
    .IsDependentOn("SetVersionInfo")
    .Does(() =>
{
    DotNetCoreRestore("./src/zubenet.common.sln");
});

Task("Build")
    .IsDependentOn("Restore")
    .Does(() =>
{
    DotNetCoreMSBuild("./src/zubenet.common.sln", new DotNetCoreMSBuildSettings()
    {
        ArgumentCustomization = args => args.Append("/p:SemVer=" + versionInfo.NuGetVersionV2)
    });
});

Task("Test")
    .IsDependentOn("Build")
    .Does(() =>
{
    var projects = GetFiles("./src/**/*.Tests.csproj");
    foreach(var project in projects)
    {
        DotNetCoreTest(
            project.GetDirectory().FullPath,
            new DotNetCoreTestSettings()
            {
                Configuration = configuration,
                NoBuild = true
            });
    }
});

// Run dotnet pack to produce NuGet packages from our projects. Versions the package
// using the build number argument on the script which is used as the revision number 
// (Last number in 1.0.0.0). The packages are dropped in the Artifacts directory.
Task("Pack")
    .IsDependentOn("Build")
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
                    ArgumentCustomization = args => args.Append("/p:PackageVersion=" + versionInfo.NuGetVersionV2)
                    // VersionSuffix = revision
                });
        }
    });

Task("Default")
    .IsDependentOn("Pack");

RunTarget(target);
