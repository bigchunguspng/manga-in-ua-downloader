dotnet pack -p:PackageID=manga.in.ua-downloader -c Release -v q
dotnet tool uninstall -g manga.in.ua-downloader
dotnet tool install -g --add-source .\nupkg\ manga.in.ua-downloader