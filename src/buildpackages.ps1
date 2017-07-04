Param(
	[string]$Configuration="Debug",
	[string]$OutputDirectory="..\NuGetFeed"
)

$ErrorActionPreference="Stop"

trap {
	Write-Output $_
	exit 1
}

$scriptRoot = Split-Path (Resolve-Path $myInvocation.MyCommand.Path) 
$OutputDirectory = Resolve-Path "$scriptRoot\$OutputDirectory"

Set-Alias Build-Pkg-Internal $scriptRoot\NuGet.exe


$CommonVersion = [System.Diagnostics.FileVersionInfo]::GetVersionInfo("$scriptRoot\FG.Common\bin\Debug\FG.Common.dll").FileVersion
$ServiceFabricExtensionsVersion = [System.Diagnostics.FileVersionInfo]::GetVersionInfo("$scriptRoot\FG.ServiceFabric.Extensions\bin\Debug\FG.ServiceFabric.Extensions.dll").FileVersion
$ServiceFabricServicesRemotingExtensionsVersion = [System.Diagnostics.FileVersionInfo]::GetVersionInfo("$scriptRoot\FG.ServiceFabric.Services.Remoting\bin\Debug\FG.ServiceFabric.Services.Remoting.dll").FileVersion

function Build-Pkg ($ProjectFile)
{
	Write-Host -ForegroundColor Cyan "Packaging $ProjectFile into $OutputDirectory"
	Build-Pkg-Internal pack $ProjectFile -Properties Configuration=$Configuration -OutputDirectory $OutputDirectory -Symbols -Prop CommonVersion=$CommonVersion -Prop ServiceFabricExtensionsVersion=$ServiceFabricExtensionsVersion -Prop ServiceFabricServicesRemotingExtensionsVersion=$ServiceFabricServicesRemotingExtensionsVersion
	
	if($LASTEXITCODE -ne 0)
	{
		Write-Error "Bailing out because nuget.exe exited with code $LASTEXITCODE"
	}
	Write-Host
}

Build-Pkg "$scriptRoot\FG.Common\FG.Common.csproj"
Build-Pkg "$scriptRoot\FG.ServiceFabric.Extensions\FG.ServiceFabric.Extensions.csproj"
Build-Pkg "$scriptRoot\FG.ServiceFabric.Testing\FG.ServiceFabric.Testing.csproj"
Build-Pkg "$scriptRoot\FG.ServiceFabric.Actors.Remoting\FG.ServiceFabric.Actors.Remoting.csproj"
Build-Pkg "$scriptRoot\FG.ServiceFabric.Services.Remoting\FG.ServiceFabric.Services.Remoting.csproj"

