rem build main lib

dotnet restore RedisTribute.sln
dotnet build RedisTribute.sln
dotnet pack "src\RedisTribute\RedisTribute.csproj" --output ..\..\artifacts

PAUSE