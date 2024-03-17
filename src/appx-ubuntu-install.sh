#!/bin/bash

# This script needs to be run from its current directroy directory
# to avoid changing any of the values and/or paths

# install dotnet core sdk
sudo snap install dotnet-sdk --classic
sudo snap alias /snap/bin/dotnet-sdk.dotnet /snap/bin/dotnet

# install & configure nginx proxy
sudo apt update
sudo apt install nginx

# configure nginx for asp.net mvc core
sudo cp /etc/nginx/nginx.conf /etc/nginx/nginx.conf.original
sudo cp nginx.conf /etc/nginx/
sudo cp proxy.conf /etc/nginx/
sudo cp mime.types /etc/nginx/
sudo cp appx.conf /etc/nginx/sites-available/

# deploy the site to default location (/var/www/microbians.com/)
sudo chmod a+x ./appx-deploy.sh
sudo source ./appx-deploy.sh

# make appx site live
sudo ln -s /etc/nginx/sites-available/microbians.com /etc/nginx/sites-enabled/

# add letsencrypt ssl cert
sudo chmod a+x ./letsencrypt.sh
sudo source ./letsencrypt.sh

# verify conf files are cool & restasrt server to accept changes
sudo nginx -t
sudo systemctl restart nginx

# Test / GET the home page
curl http://localhost/