@echo off

for %%k in (nugetfeed\*.symbols.nupkg) DO ( 
	copy %%k c:\NugetPackages
	"c:\program files (x86)\NuGet\nuget.exe" push %%k  -apikey VSTS -source testinsclear
	del %%k
	)
	
for %%k in (nugetfeed\*.nupkg) DO ( 
	copy %%k c:\NugetPackages
	del %%k
	)

pause

