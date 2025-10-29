#!/bin/bash

rm -rf Agia.Bot.Poc/bin/ Agia.Bot.Poc/obj
dotnet build --configuration Release Agia.Bot.Poc/Agia.Bot.Poc.csproj
dotnet publish --configuration Release Agia.Bot.Poc/Agia.Bot.Poc.csproj
# zip -r Agia.Bot.Poc.zip Agia.Bot.Poc/bin/Release/net9.0/publish/*

rm deploy.zip
( cd Agia.Bot.Poc/bin/Release/net9.0/publish && zip -r "$OLDPWD/deploy.zip" . )
# tar -C