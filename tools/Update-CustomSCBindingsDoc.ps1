$ErrorActionPreference = "Stop"

$pythonScript = Join-Path $PSScriptRoot "update-sc-bindings-docs\generate_star_citizen_bindings.py"
if (-not (Test-Path $pythonScript)) {
    throw "Could not find script: $pythonScript"
}

if (Get-Command py -ErrorAction SilentlyContinue) {
    & py -3 $pythonScript
}
elseif (Get-Command python -ErrorAction SilentlyContinue) {
    & python $pythonScript
}
else {
    throw "Python was not found. Install Python or add it to PATH."
}

if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
