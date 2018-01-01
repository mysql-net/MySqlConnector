#!/bin/bash

cd $(dirname $0)

if [ $TRAVIS_BRANCH != "master" ]; then
    echo "Build Docs only runs on master.  Exiting."
    exit 0
fi

if [ $TRAVIS_PULL_REQUEST != "false" ]; then
    echo "Build Docs does not run for pull requests.  Exiting."
    exit 0
fi

openssl aes-256-cbc -K $encrypted_6d671fff73d6_key -iv $encrypted_6d671fff73d6_iv -in id_rsa.enc -out ~/.ssh/id_rsa -d && chmod 600 ~/.ssh/id_rsa

# download and install hugo
curl -SsL https://github.com/gohugoio/hugo/releases/download/v0.32/hugo_0.32_Linux-64bit.deb > ~/hugo.deb
sudo dpkg -i ~/hugo.deb

# build docs
cd ../docs/
hugo
mv public ~

# push docs
mkdir -p ~/gh-pages
cd ~/gh-pages
git clone --depth=50 --branch=gh-pages git@github.com:mysql-net/MySqlConnector.git
cd MySqlConnector
rm -rf *
mv ~/public/* .
touch .nojekyll
git add --all
git commit -m "automatic docs update"
git push
