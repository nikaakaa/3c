[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$repositoryRoot = & git rev-parse --show-toplevel 2>$null
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($repositoryRoot)) {
    [System.Console]::Error.WriteLine('Repository policy requires a Git worktree.')
    exit 2
}

$trackedOutput = @(& git -C $repositoryRoot ls-files)
if ($LASTEXITCODE -ne 0) {
    [System.Console]::Error.WriteLine('Repository policy could not read the Git index.')
    exit 2
}

$trackedPaths = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
foreach ($trackedPath in $trackedOutput) {
    $normalizedPath = $trackedPath.Replace('\', '/')
    if (-not [string]::IsNullOrWhiteSpace($normalizedPath)) {
        $null = $trackedPaths.Add($normalizedPath)
    }
}

$trackedDirectories = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
foreach ($trackedPath in $trackedPaths) {
    $directoryEnd = $trackedPath.LastIndexOf('/')
    while ($directoryEnd -gt 0) {
        $directory = $trackedPath.Substring(0, $directoryEnd)
        $null = $trackedDirectories.Add($directory)
        $directoryEnd = $directory.LastIndexOf('/')
    }
}

$violations = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
function Add-Violation {
    param([string]$Message)
    $null = $violations.Add($Message)
}

$requiredFiles = @(
    '3cDemo/Client/3C_Client/ProjectSettings/ProjectVersion.txt',
    '3cDemo/Client/3C_Client/Packages/manifest.json',
    '3cDemo/Client/3C_Client/Packages/packages-lock.json'
)

foreach ($requiredFile in $requiredFiles) {
    if (-not $trackedPaths.Contains($requiredFile)) {
        Add-Violation "required Unity project file is not tracked: $requiredFile"
    }
}

$assetsRoot = '3cDemo/Client/3C_Client/Assets/'
foreach ($trackedPath in $trackedPaths) {
    if (-not $trackedPath.StartsWith($assetsRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        continue
    }

    if (-not $trackedPath.EndsWith('.meta', [System.StringComparison]::OrdinalIgnoreCase)) {
        $metaPath = "$trackedPath.meta"
        if (-not $trackedPaths.Contains($metaPath)) {
            Add-Violation "tracked Unity asset has no tracked meta: $trackedPath"
        }
        continue
    }

    $assetPath = $trackedPath.Substring(0, $trackedPath.Length - 5)
    if (-not $trackedPaths.Contains($assetPath) -and -not $trackedDirectories.Contains($assetPath)) {
        Add-Violation "tracked Unity meta has no tracked file or directory descendants: $trackedPath"
    }
}

$forbiddenDirectoryNames = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
foreach ($name in @('Library', 'Temp', 'Obj', 'Logs', 'UserSettings', 'bin', 'artifacts', 'publish')) {
    $null = $forbiddenDirectoryNames.Add($name)
}

$allowedProjectFiles = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
foreach ($path in @(
    '3cDemo/Server/Gate/Entity/ThirdPerson.Server.Gate.Entity.csproj',
    '3cDemo/Server/Gate/Hotfix/ThirdPerson.Server.Gate.Hotfix.csproj',
    '3cDemo/Server/Shared/Host/ThirdPerson.Server.Host.csproj',
    '3cDemo/Server/Products/UnityAuthority/Entity/ThirdPerson.Server.UnityAuthority.Entity.csproj',
    '3cDemo/Server/Products/UnityAuthority/Hotfix/ThirdPerson.Server.UnityAuthority.Hotfix.csproj',
    '3cDemo/Server/Products/UnityAuthority/ThirdPerson.UnityAuthority.Server.csproj',
    '3cDemo/Server/Products/DotRecastAuthority/Entity/ThirdPerson.Server.DotRecastAuthority.Entity.csproj',
    '3cDemo/Server/Products/DotRecastAuthority/Hotfix/ThirdPerson.Server.DotRecastAuthority.Hotfix.csproj',
    '3cDemo/Server/Products/DotRecastAuthority/ThirdPerson.DotRecastAuthority.Server.csproj',
    '3cDemo/Server/Server.sln',
    'Tools/ThirdPersonSimulation.Portable/ThirdPersonSimulation.Core/ThirdPersonSimulation.Core.csproj',
    'Tools/ThirdPersonSimulation.Portable/ThirdPersonSimulation.Float32/ThirdPersonSimulation.Float32.csproj',
    'Tools/ThirdPersonSimulation.Portable/ThirdPersonSimulation.Reader/ThirdPersonSimulation.Reader.csproj',
    'Tools/ThirdPersonSimulation.Portable/ThirdPersonSimulation.Tests/ThirdPersonSimulation.Tests.csproj'
)) {
    $null = $allowedProjectFiles.Add($path)
}

$clientRoot = '3cDemo/Client/3C_Client/'
foreach ($trackedPath in $trackedPaths) {
    $segments = $trackedPath.Split('/')
    for ($index = 0; $index -lt $segments.Length - 1; $index++) {
        $segment = $segments[$index]
        if ($forbiddenDirectoryNames.Contains($segment)) {
            Add-Violation "generated output directory is tracked: $trackedPath"
            break
        }
    }

    if ($trackedPath.StartsWith($clientRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        for ($index = 0; $index -lt $segments.Length - 1; $index++) {
            $segment = $segments[$index]
            if ($segment.Equals('Build', [System.StringComparison]::OrdinalIgnoreCase) -or
                $segment.Equals('Builds', [System.StringComparison]::OrdinalIgnoreCase) -or
                $segment.Equals('Bundles', [System.StringComparison]::OrdinalIgnoreCase) -or
                $segment.Equals('HybridCLRData', [System.StringComparison]::OrdinalIgnoreCase)) {
                Add-Violation "Unity client build output is tracked: $trackedPath"
                break
            }
        }
    }

    $extension = [System.IO.Path]::GetExtension($trackedPath)
    if ($extension -ne '.sln' -and $extension -ne '.csproj') {
        continue
    }

    if ($trackedPath.StartsWith($clientRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        Add-Violation "Unity client generated project file is tracked: $trackedPath"
    }
    elseif (-not $allowedProjectFiles.Contains($trackedPath)) {
        Add-Violation "unapproved solution or project file is tracked: $trackedPath"
    }
}

if ($violations.Count -gt 0) {
    Write-Host "Repository policy found $($violations.Count) violation(s):"
    foreach ($violation in ($violations | Sort-Object)) {
        Write-Host "- $violation"
    }
    exit 1
}

Write-Host "Repository policy passed for $($trackedPaths.Count) tracked file(s)."
exit 0
