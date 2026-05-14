$unp4kDir = "$PSScriptRoot\unp4k"

cd $unp4kDir

if (Test-Path "$unp4kDir\Data") {	
	Write-Host "Deleting temporary data directory..."
	del "$unp4kDir\Data" -Recurse -Force
}

Write-Host "Extracting defaultProfile.xml from Star Citizen data.p4k..."
.\unp4k.exe "$env:ProgramFiles\Roberts Space Industries\StarCitizen\LIVE\Data.p4k" 'Data/Libs/Config/defaultProfile.xml'

$xmlPath = "$unp4kDir\Data\Libs\Config\defaultProfile.xml"

if (-not (Test-Path $xmlPath)) {
    Write-Error "Error: $xmlPath not found!"
    exit 1
}

Write-Host "Converting defaultProfile.xml..."
.\unforge.cli.exe $xmlPath

Write-Host "Copying defaultProfile.xml to SSCM.StarCitizen project..."
copy $xmlPath "$unp4kDir\..\..\src\SSCM.StarCitizen\defaultProfile.xml" -Force

Write-Host "Deleting temporary data directory..."
del "$unp4kDir\Data" -Recurse -Force

Write-Host "Done!"