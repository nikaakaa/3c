param(
    [Parameter(Mandatory = $true)]
    [string]$CandidateId,
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$productRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\..\..\Build\Server\Startup"))
$allowedRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\..\..\Build\Server"))
if (-not $productRoot.StartsWith($allowedRoot + [System.IO.Path]::DirectorySeparatorChar, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Startup publish root escapes Build/Server."
}

$stagingRoot = Join-Path $allowedRoot (".startup-" + [Guid]::NewGuid().ToString("N"))
try {
    dotnet publish (Join-Path $PSScriptRoot "ThirdPerson.Startup.Server.csproj") `
        -c $Configuration `
        -o $stagingRoot `
        --disable-build-servers `
        /nr:false `
        /p:UseSharedCompilation=false
    if ($LASTEXITCODE -ne 0) {
        throw "Startup Server publish failed."
    }

    & (Join-Path $stagingRoot "ThirdPerson.Startup.Server.exe") --write-server-product-manifest $CandidateId
    if ($LASTEXITCODE -ne 0) {
        throw "Startup Server manifest generation failed."
    }

    $previousRoot = $productRoot + ".previous"
    if (Test-Path -LiteralPath $previousRoot) {
        Remove-Item -LiteralPath $previousRoot -Recurse -Force
    }
    if (Test-Path -LiteralPath $productRoot) {
        Move-Item -LiteralPath $productRoot -Destination $previousRoot
    }
    Move-Item -LiteralPath $stagingRoot -Destination $productRoot
    if (Test-Path -LiteralPath $previousRoot) {
        Remove-Item -LiteralPath $previousRoot -Recurse -Force
    }
}
finally {
    dotnet build-server shutdown
    if (Test-Path -LiteralPath $stagingRoot) {
        Remove-Item -LiteralPath $stagingRoot -Recurse -Force
    }
}
