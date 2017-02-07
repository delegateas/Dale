// include Fake libs
#I @"packages/NuGet.CommandLine/tools/"
#r "./packages/FAKE/tools/FakeLib.dll"

open Fake

// Directories
let buildDir  = "./build/"
let deployDir = "./deploy/"
let packagingDir = "./pkg/"

// Filesets
let appReferences  =
    !! "/**/*.csproj"
    ++ "/**/*.fsproj"
let allPackageFiles = [ "./build/Dale.dll" ]

// version info
let version = "0.2.34"

let dependencies =
  Paket.GetDependenciesForReferencesFile "./src/Dale/paket.references"
  |> Array.toList

// Targets
Target "Clean" (fun _ ->
    CleanDirs [buildDir; deployDir]
)

Target "Build" (fun _ ->
    // compile all projects below src/app/
    MSBuildDebug buildDir "Build" appReferences
    |> Log "AppBuild-Output: "
)

Target "Deploy" (fun _ ->
    !! (buildDir + "/**/*.*")
    -- "*.zip"
    |> Zip buildDir (deployDir + "ApplicationName." + version + ".zip")
)

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

// Build order
"Clean"
  ==> "Build"
  ==> "Deploy"

// start build
RunTargetOrDefault "Build"
