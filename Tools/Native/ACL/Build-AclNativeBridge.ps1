param(
    [string]$BuildDirectory = "$PSScriptRoot/build/windows-x64",
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release"
)

$sourceDirectory = Resolve-Path $PSScriptRoot
cmake -S $sourceDirectory -B $BuildDirectory -A x64 "-DCMAKE_CONFIGURATION_TYPES=$Configuration"
if ($LASTEXITCODE -ne 0) { throw "ACL native bridge configuration failed." }
cmake --build $BuildDirectory --config $Configuration --target 3c_acl_runtime --parallel
if ($LASTEXITCODE -ne 0) { throw "ACL native bridge build failed." }
