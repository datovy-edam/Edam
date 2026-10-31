
# LEARN ABOUT DAB --------------------------------------------
#
# https://learn.microsoft.com/en-us/azure/data-api-builder/
#
# Download the Schema (change v0.10.23) to needed version...
# https://github.com/Azure/data-api-builder/releases/download/v0.10.23/dab.draft.schema.json

# - Release Date: March 15, 2023
# - https://aka.ms/dab

# dotnet --list-sdks

# INSTALL/UPDATE DAB CLI -------------------------------------
#
# dotnet tool install --global Microsoft.DataApiBuilder
# dotnet tool update --global Microsoft.DataApiBuilder

# TEST DAB CLI INSTALLATION ----------------------------------
#
# after installation test dab with the following
dab --version
dab --help

# create an environment file (.env) as folows:
#
# my-connection-string="[PUT-YOUR-CONNECTION-STRING-HERE]"
# DAB_ENVIRONMENT=Development

# create a config file to start-up efforts
dab init --help
dab init --database-type mssql --host-mode Development -c dab.init.Development.json --connection-string "Server=.;Database=Edam.Database;User Id=DbUser;Password=MySecretPassword!;TrustServerCertificate=True"

# within your config-file you can specify the connection-string from the environment as follows:
# {
#    "connection-string": "@env('my-connection-string')"
# }

# move to the directory where the dab resources are located
cd C:\users\esobr\Documents\Edam.Studio\Edam.App.Other\Projects\MS.DataApiBuilder\Api

# start dab with a given config file
dab start -c dab-cli-demo.config.json

# experimental Edam.Studio entities generation
# dab start -c dab.sample.config.json
dab start -c dab-hc_cd-cli.config.json
dab start -c dab-hc_cd-cli.all.config.json

# export the required schema as a file and save to disk... NW
dab export -o ./schemas -c dab-cli.config.json -g schema.graphql

# test endpoints in the browser
#
# https://localhost:5001/api/DomainType
# https://localhost:5001/swagger/index.html
# https://localhost:5001/graphql/

