cd $PSScriptRoot

if (Test-Path "$PSScriptRoot\Data") {	
	Write-Host "Deleting temporary data directory..."
	del "$PSScriptRoot\Data" -Recurse -Force
}

Write-Host "Extracting defaultProfile.xml from Star Citizen data.p4k..."
.\unp4k.exe "$env:ProgramFiles\Roberts Space Industries\StarCitizen\LIVE\Data.p4k" 'Data/Libs/Config/defaultProfile.xml'

$xmlPath = "$PSScriptRoot\Data\Libs\Config\defaultProfile.xml"

if (-not (Test-Path $xmlPath)) {
    Write-Error "Error: $xmlPath not found!"
    exit 1
}

Write-Host "Converting defaultProfile.xml..."
.\unforge.exe $xmlPath

Write-Host "Copying defaultProfile.xml to SSCM.StarCitizen project..."
copy $xmlPath "$PSScriptRoot\..\..\src\SSCM.StarCitizen\defaultProfile.xml" -Force

Write-Host "Deleting temporary data directory..."
del "$PSScriptRoot\Data" -Recurse -Force

Write-Host "Done!"