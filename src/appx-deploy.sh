#!/bin/bash

# build & publish the appx site
dotnet build src/AppX.Web/AppX.csproj -c Release
dotnet publish src/AppX.Web/AppX.csproj -c Release

DIRECTORY="/var/www/microbians.com"

# create home for appx web app
if [[ ! -d "$DIRECTORY" ]]; then
    sudo mkdir -p /var/www/microbians.com
fi

sudo chmod -R 755 $DIRECTORY

# backup the current appsettings
sudo cp -f /var/www/microbians.com/appsettings.json /var/www/microbians.com/appsettings.json.bak

# make sure appx.service has reference to the appropriate directory
sudo cp -rf src/AppX.Web/bin/Release/net6.0/publish/* /var/www/microbians.com/

sudo systemctl enable appx.service

sudo systemctl start appx.service

#Controlling the Service
#
## Control whether service loads on boot
#systemctl enable
#systemctl disable
#
## Manual start and stop
#systemctl start
#systemctl stop
#
## Restarting/reloading
#systemctl daemon-reload # Run if .service file has changed
#systemctl restart
#
#View Status/Logs
#
## See if running, uptime, view latest logs
#systemctl status
#systemctl status [service_name]
#
## See all systemd logs
#journalctl
#
## Tail logs
#journalctl -f
#
## Show logs for specific service
#journalctl -u my_daemon.service