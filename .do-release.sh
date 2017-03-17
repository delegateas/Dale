#!/bin/sh

# Post-build script for Travis CI

# Only work on the selected branches:
if [ "$TRAVIS_BRANCH" != "master" ] && [ "$TRAVIS_BRANCH" != "feature_travis" ]
  exit
fi
# RELEASE becomes part of build artifact, accessible from web service
printf $GIT_TAG > ./src/Dale.Server/static/RELEASE
# Generate the package
./build.sh MSDeploy
# Prepare a new tag 
git config --global user.email "builds@travis-ci.com"
git config --global user.name "Travis CI"
git add -f ./src/Dale.Server/static/RELEASE
git commit -m "Prepare release $TRAVIS_BUILD_NUMBER"
git tag $GIT_TAG -a -m "Generated tag from TravisCI build $TRAVIS_BUILD_NUMBER"
git push --quiet https://$GITHUBKEY@github.com/delegateas/dale $GIT_TAG > /dev/null 2>&1
# Create release based on tag
resphdrs=$(curl -i -X POST -H "Content-Type: application/json" \
  -d '{"tag_name": "$GIT_TAG", "name": "$GIT_TAG"}' \
  https://api.github.com/repos/delegateas/dale/releases?access_token=$GITHUBKEY)
apiurl=$(printf $resphdrs | grep -Fi Location | cut -d ' ' -f 2)
uploadurl=$(sed -i 's/api.github/upload.github/g' $resphdrs)
curl -X POST -H "Content-Type: application/zip" -d ./build/Dale.Server.MSDeploy.zip \
 "$apiurl?name=Dale.Server.zip&access_token=$GITHUBKEY"

# Dump to public Azure Blob Storage for reference in ARM template
curl -X PUT -H "Content-Type: application/zip" -d ./build/Dale.Server.MSDeploy.zip \
 "$AZUREBLOBURL/$GIT_TAG/Dale.Server.zip$AZUREBLOBSAS
