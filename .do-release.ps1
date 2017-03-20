# Post-build script for Appveyor

APPVEYOR_REPO_BRANCH
if (($env:APPVEYOR_REPO_BRANCH -ne "master") -and ($env:APPVEYOR_REPO_BRANCH -ne "feature_travis"))
{
  Write-Host "$env:APPVEYOR_REPO_BRANCH is not a release branch. Exiting ... "
  exit
}

$date = Get-Date -Format "yyyy-MM-dd"
$GIT_TAG="rev$($env:APPVEYOR_BUILD_NUMBER)_$date"
Write-Host "Tag is $($GIT_TAG)."
# RELEASE becomes part of build artifact, accessible from web service
Write-Host "Patching RELEASE file ... "
$GIT_TAG | Out-File -Append ./src/Dale.Server/static/RELEASE
$env:APPVEYOR_REPO_COMMIT | Out-File -Append ./src/Dale.Server/static/RELEASE
$env:APPVEYOR_REPO_COMMIT_TIMESTAMP | Out-File -Append ./src/Dale.Server/static/RELEASE
Write-Host "RELEASE file patched."

Write-Host "Starting MSDeploy target ... "
./build.cmd MSDeploy
Write-host "MSDeploy target finished."

Write-Host "Preparing new git tag ... "
git config --global user.email "builds@appveyor.com"
git config --global user.name "Appveyor CI"
git add -f ./src/Dale.Server/static/RELEASE
git commit -m "Prepare release $($env:APPVEYOR_BUILD_NUMBER)"
git tag $GIT_TAG -a -m "Generated tag from Appveyor build $($env:APPVEYOR_BUILD_NUMBER)"
git push "https://$($env:GITHUBKEY)@github.com/delegateas/dale" $GIT_TAG

Write-Host "Creating GitHub release ... "
$resp = curl -Method Post -Headers @{"Content-Type" = "application/json"} -Body '{"tag_name": "$GIT_TAG", "name": "$GIT_TAG"}' -Uri https://api.github.com/repos/delegateas/dale/releases?access_token=$GITHUBKEY
Write-Host "GitHub release: $($resp.StatusDescription)"
$apiurl = $resp.Headers.Location
$uploadurl = $apiurl.Replace("api.github","upload.github")

Write-Host "Posting release to $uploadurl ... "
$resp2 = curl -Method POST -Headers @{"Content-Type" = "application/zip"} -InFile ./build/Dale.Server.zip -Uri "$uploadurl?name=Dale.Server.zip&access_token=$($env:GITHUBKEY)"
Write-Host "GitHub upload: $($resp2.StatusDescription)"


Write-Host "Posting artifact to Azure Blob storage ... "
$resp3 = curl -Method POST -Headers @{"Content-Type" = "application/zip"} -InFile ./build/Dale.Server.zip -Uri "$env:AZUREBLOBURL/$GIT_TAG/Dale.Server.zip$($env:AZUREBLOBSAS)"
Write-Host "Azure blob storage: $($resp3.StatusDescription)"

Write-Host "Release finished."
