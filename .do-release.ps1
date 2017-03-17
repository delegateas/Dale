# Post-build script for Appveyor

# Only work on the selected branches:
if (($APPVEYOR_REPO_BRANCH -ne "master") -and ($APPVEYOR_REPO_BRANCH -ne "feature_travis"))
{
  exit
}

choco install curl

# RELEASE becomes part of build artifact, accessible from web service
$GIT_TAG | Out-File -Append ./src/Dale.Server/static/RELEASE
$APPVEYOR_REPO_COMMIT | Out-File -Append ./src/Dale.Server/static/RELEASE
$APPVEYOR_REPO_COMMIT_TIMESTAMP | Out-File -Append ./src/Dale.Server/static/RELEASE

# Generate the package
./build.cmd MSDeploy

# Prepare a new tag 
git config --global user.email "builds@appveyor.com"
git config --global user.name "Appveyor CI"
git add -f ./src/Dale.Server/static/RELEASE
git commit -m "Prepare release $APPVEYOR_BUILD_NUMBER"
git tag $GIT_TAG -a -m "Generated tag from Appveyor build $APPVEYOR_BUILD_NUMBER"
git push --quiet https://$GITHUBKEY@github.com/delegateas/dale $GIT_TAG > /dev/null 2>&1

# Create release based on tag
$resphdrs=$(curl -i -X POST -H "Content-Type: application/json" \
  -d '{"tag_name": "$GIT_TAG", "name": "$GIT_TAG"}' \
  https://api.github.com/repos/delegateas/dale/releases?access_token=$GITHUBKEY)
$apiurl=$(printf $resphdrs | grep -Fi Location | cut -d ' ' -f 2)
$uploadurl=$(sed -i 's/api.github/upload.github/g' $resphdrs)
curl -X POST -H "Content-Type: application/zip" -d ./build/Dale.Server.MSDeploy.zip "$apiurl?name=Dale.Server.zip&access_token=$GITHUBKEY"

# Dump to public Azure Blob Storage for reference in ARM template
curl -X PUT -H "Content-Type: application/zip" -d ./build/Dale.Server.MSDeploy.zip "$AZUREBLOBURL/$GIT_TAG/Dale.Server.zip$AZUREBLOBSAS"
