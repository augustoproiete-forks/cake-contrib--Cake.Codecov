#load "nuget:https://f.feedz.io/wormiecorp/packages/nuget?package=Cake.Recipe&version=2.0.0-unstable0232&prerelease"

Environment.SetVariableNames();

BuildParameters.SetParameters(
                            context: Context,
                            buildSystem: BuildSystem,
                            sourceDirectoryPath: "./Source",
                            title: "Cake.Codecov",
                            repositoryOwner: "cake-contrib",
                            repositoryName: "Cake.Codecov",
                            appVeyorAccountName: "cakecontrib",
                            shouldRunDotNetCorePack: true,
                            shouldGenerateDocumentation: false,
                            shouldRunCodecov: true,
                            shouldRunGitVersion: true);

BuildParameters.PrintParameters(Context);

ToolSettings.SetToolSettings(
                            context: Context,
                            dupFinderExcludePattern: new string[] {
                                BuildParameters.RootDirectoryPath + "/Source/Cake.Codecov.Tests/*.cs"
                            },
                            dupFinderExcludeFilesByStartingCommentSubstring: new string[] {
                                "<auto-generated>"
                            },
                            testCoverageFilter: "+[Cake.Codecov]*");

// Tasks we want to override
((CakeTask)BuildParameters.Tasks.UploadCodecovReportTask.Task).Actions.Clear();
BuildParameters.Tasks.UploadCodecovReportTask
    .IsDependentOn("DotNetCore-Pack")
    .Does(() => RequireTool(BuildParameters.IsDotNetCoreBuild ? ToolSettings.CodecovGlobalTool : ToolSettings.CodecovTool, () => {
        var nugetPkg = $"nuget:file://{MakeAbsolute(BuildParameters.Paths.Directories.NuGetPackages)}?package=Cake.Codecov&version={BuildParameters.Version.SemVersion}&prerelease";
        Information("PATH: " + nugetPkg);

        var coverageFilter = BuildParameters.Paths.Directories.TestCoverage + "/coverlet/*.xml";
        Information($"Passing coverage filter to codecov: \"{coverageFilter}\"");

        var environmentVariables = new Dictionary<string, string>();

        if (BuildParameters.Version != null && !string.IsNullOrEmpty(BuildParameters.Version.FullSemVersion) && BuildParameters.IsRunningOnAppVeyor)
        {
            var buildVersion = string.Format("{0}.build.{1}",
                BuildParameters.Version.FullSemVersion,
                BuildSystem.AppVeyor.Environment.Build.Number);
            environmentVariables.Add("APPVEYOR_BUILD_VERSION", buildVersion);
        }

        var script = string.Format(@"#addin ""{0}""
Codecov(new CodecovSettings {{
    Files = new[] {{ ""{1}"" }},
    Root = ""{2}"",
    Required = true
}});",
            nugetPkg, coverageFilter, BuildParameters.RootDirectoryPath);

        RequireAddin(script, environmentVariables);
    })
);

// Enable drafting a release when running on the master branch
if (BuildParameters.IsRunningOnAppVeyor &&
    BuildParameters.IsMainRepository && BuildParameters.BranchType == BranchType.Master && !BuildParameters.IsTagged)
{
    BuildParameters.Tasks.ContinuousIntegrationTask.IsDependentOn("Create-Release-Notes");
}


Build.RunDotNetCore();
