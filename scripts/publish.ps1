param(
    # OPTIONS

    [Parameter()]
    [switch] $AddHash = $false,

    [Parameter()]
    [switch] $AllTargets = $false,

    [Parameter()]
    [switch] $Zipped = $false,

    # METADATA

    [Parameter()]
    [string] $BuildId = $null,
    
    [Parameter()]
    [string] $CommitAuthor = $null,
    
    [Parameter()]
    [string] $CommitDate = $null,

    [Parameter()]
    [string] $CommitHash = $null,
    
    [Parameter()]
    [string] $RefName = $null
)

function Coalesce($a, $b) { if ($null -ne $a -and '' -ne $a) { $a } else { $b } }

function Exec($cmd, $suppress)
{
    if (!$suppress) { Write-Host "> $cmd" }
    return (iex $cmd)
}

function GetMetadata()
{
    # cheating by referencing the parameters directly, but oh well
    return @{
        build_id = $BuildId
        commit_author = if ($CommitAuthor) { $CommitAuthor } else { Exec "git show -s --format=%ae" $true }
        commit_date = if ($CommitDate) { $CommitDate } else { Exec "git show -s --format=%ci" $true }
        commit_hash = if ($CommitHash) { $CommitHash } else { Exec "git show -s --format=%h" $true }
        publish_time = (Get-Date -Format "O")
        ref_name = if ($RefName) { $RefName } else { Exec "git branch --show" $true }
    }   
}

function WriteMetadata($metadata, $path)
{
    $dir = [System.IO.Path]::GetDirectoryName($path)
    New-Item -Path $dir -ItemType Directory -Force | Out-Null
    Set-Content -Path $path -Value (ConvertTo-Json $metadata) -Encoding Ascii
}

function Publish($srcFolder, $outputPath, $options, $metadataPath, $zip)
{
    Write-Host "Publishing..."
    Exec "dotnet publish $srcFolder -o $outputPath $options"

    Copy-Item -Path $metadataPath -Destination $outputPath

    if ($zip) {
        Write-Host "Zipping..."
        $zipPath = "$outputPath.zip"
        $ProgressPreference = 'SilentlyContinue'
        Compress-Archive -Path $outputPath -DestinationPath $zipPath -Force
        Write-Host "Cleaning up..."
        Remove-Item -Path $outputPath -Recurse -Force
        Write-Host "Published to [$zipPath]."
    } else {
        Write-Host "Published to [$outputPath]."
    }
}

&{
    $workspaceFolder = [System.IO.Path]::GetFullPath("$PSScriptRoot\..")
    $srcFolder = "$workspaceFolder\src\SSCM.cli"
    $releasesFolder = "$workspaceFolder\releases"
    $metadataPath = "$releasesFolder\release.json"

    Write-Host "Writing metadata..."
    $metadata = GetMetadata
    WriteMetadata $metadata $metadataPath
    $suffix = if ($AddHash) { "-$($metadata.commit_hash)" } else { "" }

    Publish $srcFolder "$releasesFolder\sscm-win$suffix" "" $metadataPath $Zipped

    if ($AllTargets) {
        Publish $srcFolder "$releasesFolder\sscm-win-full$suffix" "--sc" $metadataPath $Zipped
        Publish $srcFolder "$releasesFolder\sscm-linux$suffix" "--os linux" $metadataPath $Zipped
        Publish $srcFolder "$releasesFolder\sscm-linux-full$suffix" "--os linux --sc" $metadataPath $Zipped
    }
}