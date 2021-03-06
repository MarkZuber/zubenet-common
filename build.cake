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
        UpdateAssemblyInfoFilePath = "./src/gen/GlobalAssemblyInfo.cs",
        ArgumentCustomization = args => args.Append("/ensureAssemblyInfo")
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
    var settings = new DotNetCoreMSBuildSettings()
    {
        ArgumentCustomization = args => args.Append("/p:SemVer=" + versionInfo.NuGetVersionV2)
    };
    settings.SetConfiguration(configuration);

    DotNetCoreMSBuild("./src/zubenet.common.sln", settings);
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
    .IsDependentOn("Test")
    .Does(() =>
    {
        foreach (var project in GetFiles("./src/**/*.csproj"))
        {
            DotNetCorePack(
                project.GetDirectory().FullPath,
                new DotNetCorePackSettings()
                {
                    Configuration = configuration,
                    OutputDirectory = artifactsDirectory,
                    ArgumentCustomization = args => args.Append("/p:PackageVersion=" + versionInfo.NuGetVersionV2)
                });
        }
    });

Task("Push")
    .IsDependentOn("Pack")
    .Does(() =>
    {
        if (AppVeyor.IsRunningOnAppVeyor && 
            AppVeyor.Environment.Repository.Branch == "master" &&
            EnvironmentVariable("APPVEYOR_PULL_REQUEST_NUMBER") == null) {
            var settings = new DotNetCoreNuGetPushSettings() {
                // Source = "https://www.nuget.org/",
                // ApiKey = EnvironmentVariable("nuget_api_key")
                Source = "https://www.myget.org/F/zubenet",
                ApiKey = EnvironmentVariable("myget_api_key")
            };
            DotNetCoreNuGetPush(System.IO.Path.Combine(artifactsDirectory, "*.nupkg"), settings);
        } else {
            Information("We're not on AppVeyor with current branch == master, or we're a pull request so not pushing packages...");
        }
    });

if (AppVeyor.IsRunningOnAppVeyor) {
    Task("Default").IsDependentOn("Push");
} else {
    Task("Default").IsDependentOn("Pack");
}

RunTarget(target);
