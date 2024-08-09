@echo off

powershell write-host -fore Yellow Building the project
dotnet pack -p:PackageID=manga.in.ua-downloader -c Release

powershell write-host -fore Yellow Removing files from ./pack/nupkg/...
del /Q "pack\nupkg\*"

powershell write-host -fore Yellow Copying latest release from ./nupkg/ to ./pack/nupkg/...
for /f %%i in ('dir /B /O:D nupkg\*') do set "latest=%%i"
copy "nupkg\%latest%" "pack\nupkg\"

powershell write-host -fore Yellow Zipping latest release into ./Releases/...
for %%F in ("pack\%latest%") do set "zipName=%%~nF"
powershell Compress-Archive -Path "pack\*" -Force -DestinationPath "Releases\%zipName%.zip"

powershell write-host -fore Yellow Done.
pause