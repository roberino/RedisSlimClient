$version = GitVersion | ConvertFrom-Json
# Write-Host $version
$currentPath = Split-Path $MyInvocation.MyCommand.Path
$paths = "$currentPath/src/RedisTribute/RedisTribute.csproj", "$currentPath/src/RedisTribute.ApplicationInsights/RedisTribute.ApplicationInsights.csproj", "$currentPath/src/RedisTribute.Json/RedisTribute.Json.csproj"

foreach($path in $paths) {
	$xml = [XML](Get-Content $path)
	$nodes = $xml.Project.PropertyGroup.ChildNodes

	foreach($node in $nodes) 
	{
		if($node.Name -eq 'Version') 
		{
			Write-Host "Set" $node.Name "to" $version.NuGetVersionV2
			$node.InnerText = $version.NuGetVersionV2
		}
	}
	$xml.Save($path)
}