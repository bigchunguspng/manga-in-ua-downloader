@echo off

REM LMAO I ChatGPTed it 0)))))

echo Removing files from ./pack/nupkg/...
del /Q "pack\nupkg\*"

echo Copying latest release from ./nupkg/ to ./pack/nupkg/...
for /f %%i in ('dir /B /O:D nupkg\*') do set "latest=%%i"
copy "nupkg\%latest%" "pack\nupkg\"

echo Zipping latest release into ./Releases/...
for %%F in ("pack\%latest%") do set "zipName=%%~nF"
powershell Compress-Archive -Path "pack\*" -Force -DestinationPath "Releases\%zipName%.zip"

echo Done.
pause