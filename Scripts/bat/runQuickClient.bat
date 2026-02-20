@echo off
title Client.bat
cd ../../

call dotnet run --project Content.Client --no-build %*

pause
