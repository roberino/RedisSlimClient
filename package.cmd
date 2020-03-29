rem build main lib

dotnet restore RedisTribute.sln
dotnet build RedisTribute.sln
dotnet pack "src\RedisTribute\RedisTribute.csproj" --output .\artifacts
dotnet pack "src\RedisTribute.Json\RedisTribute.Json.csproj" --output .\artifacts
dotnet pack "src\RedisTribute.ApplicationInsights\RedisTribute.ApplicationInsights.csproj" --output .\artifacts

PAUSE