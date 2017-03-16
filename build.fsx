// include Fake libs
#I @"packages/NuGet.CommandLine/tools/"
#r "./packages/FAKE/tools/FakeLib.dll"

open Fake
open Fake.Azure
open Fake.Azure.WebJobs
open System
open System.IO

let webappProj = "./src/Dale.Server/Dale.Server.fsproj"
let buildDir  = "./build/"
let deployDir = "./deploy/"
let packagingDir = "./pkg/"

let appReferences  =
    !! "/**/*.csproj"
    ++ "/**/*.fsproj"
let allPackageFiles = [ "./build/Dale.dll" ]

// version info
let version = "1.1.0"

let deploymentPackage = "Dale.WebApp." + version + ".zip"

let dependencies =
  Paket.GetDependenciesForReferencesFile "./src/Dale/paket.references"
  |> Array.toList

Target "BuildSolution" (fun _ ->
    webappProj
    |> MSBuildHelper.build (fun defaults ->
        { defaults with
            Verbosity = Some Minimal
            Targets = [ "Build" ]
            Properties = [ "Configuration", "Release"
                           "OutputPath", Kudu.deploymentTemp ] })
    |> ignore)

// Targets
Target "Clean" (fun _ ->
    CleanDirs [buildDir; deployDir]
)

Target "Build" (fun _ ->
    webappProj
    |> MSBuildHelper.build (fun defaults ->
        { defaults with
            Verbosity = Some Diagnostic
            Targets = [ "Build" ]
            Properties = [ "Configuration", "Release"
                           "OutputPath", buildDir ] })
    |> ignore)

Target "Zip" (fun _ ->
    !! (buildDir + "/**/*.*")
    -- "*.zip"
    |> Zip buildDir (deployDir + deploymentPackage)
)

Target "Unzip" (fun _ ->
    Unzip Kudu.deploymentTemp (deployDir + deploymentPackage)
)

Target "MSDeploy" (fun _ ->
  webappProj
    |> MSBuildHelper.build (fun defaults ->
        { defaults with
            Verbosity = Some Minimal
            Targets = [ "Build" ]
            Properties = [ "Configuration", "Release"
                           "DeployOnBuild", "true"
                           "PublishProfile", "MSDeploy.pubxml"
                           "ProjectName", "Dale.Server.MSDeploy"
                           "ConfigurationName", "Release"
                           "PackageLocation", "../../build/"
                           "OutDir", "../../build/"] })
    |> ignore)

Target "StageWebsiteAssets" (fun _ ->
    let blacklist =
        [ "typings"
          ".fs"
          ".references"
          "tsconfig.json" ]
    let shouldInclude (file:string) =
        blacklist
        |> Seq.forall(not << file.Contains)
    Kudu.stageFolder (Path.GetFullPath @"src\Dale.Server\WebHost") shouldInclude)

Target "CreatePackage" (fun _ ->
  // Copy all the package files into a package folder
  CopyFiles packagingDir allPackageFiles

  NuGet (fun p ->
    {p with
      Authors = [ "Delegate" ]
      Project = "Delegate.AuditLogExporter"
      Description = "Export Office365 Audit logs via Webhook"
      Summary = "Export Office365 Audit logs via Webhook"
      Version = version
      OutputPath = packagingDir
      WorkingDir = packagingDir
      NoDefaultExcludes = true
      Dependencies = dependencies
      AccessKey = ""
      PublishUrl = "nuget.org"
      Publish = true })
    "./Delegate.AuditLogExporter.nuspec"
)

Target "Deploy" Kudu.kuduSync

// start build
RunTargetOrDefault "Build"
