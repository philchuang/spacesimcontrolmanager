$unp4kDir = "$PSScriptRoot\unp4k"
$destPath = "$unp4kDir\..\..\src\SSCM.StarCitizen\defaultProfile.xml"

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

Write-Host "Post-processing defaultProfile.xml..."

$content = [System.IO.File]::ReadAllText($xmlPath)
$content = $content -replace '(?s)<!\[CDATA\[\s*/?\>\s*\]\]>', ''

$xmlDoc = New-Object System.Xml.XmlDocument
$xmlDoc.PreserveWhitespace = $false
$xmlDoc.LoadXml($content)

$settings = New-Object System.Xml.XmlWriterSettings
$settings.Indent = $true
$settings.IndentChars = '  '
$settings.NewLineChars = "`n"
$settings.OmitXmlDeclaration = $true

$sw = New-Object System.IO.StringWriter
$writer = [System.Xml.XmlWriter]::Create($sw, $settings)
$xmlDoc.Save($writer)
$writer.Close()

[System.IO.File]::WriteAllText($xmlPath, $sw.ToString())

Write-Host "Copying defaultProfile.xml to SSCM.StarCitizen project..."
copy $xmlPath $destPath -Force

Write-Host "Deleting temporary data directory..."
del "$unp4kDir\Data" -Recurse -Force

Write-Host "Done!"
