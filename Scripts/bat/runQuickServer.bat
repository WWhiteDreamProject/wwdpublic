@echo off
title Server.bat
cd ../../

call dotnet run --project Content.Server --no-build %*

pause
