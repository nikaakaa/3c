function Get-ServerProductSha256([string]$Path) {
    if (!(Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Server product file does not exist: $Path"
    }
    return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
}

function Assert-ServerProductRecord([string]$Root, $Record) {
    if ($null -eq $Record -or [string]::IsNullOrWhiteSpace($Record.moduleId) -or
        [string]::IsNullOrWhiteSpace($Record.relativePath) -or
        [string]::IsNullOrWhiteSpace($Record.sha256)) {
        throw "Server product manifest contains an incomplete file record."
    }
    $path = [System.IO.Path]::GetFullPath((Join-Path $Root $Record.relativePath))
    if (!$path.StartsWith(([System.IO.Path]::GetFullPath($Root) + [System.IO.Path]::DirectorySeparatorChar), [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Server product record escapes its product root: $($Record.relativePath)"
    }
    if ((Get-ServerProductSha256 $path) -ne $Record.sha256) {
        throw "Server product hash mismatch: $($Record.relativePath)"
    }
    if (![string]::IsNullOrWhiteSpace($Record.pdbRelativePath)) {
        $pdb = Join-Path $Root $Record.pdbRelativePath
        if ((Get-ServerProductSha256 $pdb) -ne $Record.pdbSha256) {
            throw "Server product PDB hash mismatch: $($Record.pdbRelativePath)"
        }
    }
}

function Get-ServerProductRuntimeAssemblies([string]$Root, [string]$Executable) {
    $dependencyManifest = Join-Path $Root ([System.IO.Path]::ChangeExtension($Executable, ".deps.json"))
    if (!(Test-Path -LiteralPath $dependencyManifest -PathType Leaf)) {
        throw "Server product dependency manifest does not exist: $dependencyManifest"
    }
    $document = Get-Content -LiteralPath $dependencyManifest -Raw -Encoding UTF8 | ConvertFrom-Json
    $assemblies = @()
    foreach ($target in $document.targets.PSObject.Properties.Value) {
        foreach ($library in $target.PSObject.Properties.Value) {
            foreach ($propertyName in @("runtime", "runtimeTargets")) {
                $assets = $library.$propertyName
                if ($null -eq $assets) {
                    continue
                }
                foreach ($asset in $assets.PSObject.Properties.Name) {
                    if ($asset.EndsWith(".dll", [System.StringComparison]::OrdinalIgnoreCase)) {
                        $assemblies += [System.IO.Path]::GetFileName($asset)
                    }
                }
            }
        }
    }
    $result = @($assemblies | Sort-Object -Unique)
    if ($result.Count -eq 0) {
        throw "Server product dependency manifest contains no runtime assemblies."
    }
    return $result
}

function Assert-ServerProductBuild {
    param(
        [Parameter(Mandatory = $true)][string]$Root,
        [Parameter(Mandatory = $true)][string]$ManifestPath,
        [Parameter(Mandatory = $true)][string]$ExpectedProductId,
        [Parameter(Mandatory = $true)][string]$ExpectedHostProductId,
        [Parameter(Mandatory = $true)][string]$ExpectedHostRouteKind,
        [Parameter(Mandatory = $true)][string]$ExpectedLaunchKind,
        [Parameter(Mandatory = $true)][int]$ExpectedHostManifestSchemaVersion,
        [Parameter(Mandatory = $true)][string]$ExpectedAuthoritySolverId,
        [Parameter(Mandatory = $true)][string]$ExpectedAuthoritySolverVersion,
        [Parameter(Mandatory = $true)][UInt64]$ExpectedAuthoritySolverCapabilities,
        [Parameter(Mandatory = $true)][UInt64]$ExpectedAuthoritySolverFeatures,
        [Parameter(Mandatory = $true)][string]$ExpectedExecutable,
        [Parameter(Mandatory = $true)][string[]]$ExpectedScenes,
        [Parameter(Mandatory = $true)][string[]]$ExpectedEntityModules,
        [Parameter(Mandatory = $true)][string[]]$ExpectedHotfixModules,
        [string[]]$RequiredPortableDependencies = @(),
        [string[]]$ExpectedAuthorityArtifacts = @(),
        [string[]]$ForbiddenModuleIds = @()
    )
    $manifest = Get-Content -LiteralPath $ManifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
    if ($manifest.schemaVersion -ne 3 -or $manifest.serverProductId -ne $ExpectedProductId -or
        [string]::IsNullOrWhiteSpace($manifest.candidateId)) {
        throw "Server product manifest identity is invalid."
    }
    if ($manifest.authorityHost.hostProductId -ne $ExpectedHostProductId -or
        $manifest.authorityHost.routeKind -ne $ExpectedHostRouteKind -or
        $manifest.authorityHost.launchKind -ne $ExpectedLaunchKind -or
        $manifest.authorityHost.manifestSchemaVersion -ne $ExpectedHostManifestSchemaVersion -or
        $manifest.authorityHost.authoritySolverId -ne $ExpectedAuthoritySolverId -or
        $manifest.authorityHost.authoritySolverVersion -ne $ExpectedAuthoritySolverVersion -or
        [UInt64]$manifest.authorityHost.authoritySolverCapabilities -ne $ExpectedAuthoritySolverCapabilities -or
        [UInt64]$manifest.authorityHost.authoritySolverFeatures -ne $ExpectedAuthoritySolverFeatures -or
        [string]::IsNullOrWhiteSpace($manifest.authorityHost.descriptorHash)) {
        throw "Server product Authority Host declaration is invalid."
    }
    if ($manifest.executable.relativePath -ne $ExpectedExecutable -or
        $manifest.configuration.relativePath -ne "Fantasy.config") {
        throw "Server product executable or configuration identity is invalid."
    }
    $actualScenes = @($manifest.sceneTypes | Sort-Object)
    $requiredScenes = @($ExpectedScenes | Sort-Object)
    if (($actualScenes -join "|") -ne ($requiredScenes -join "|")) {
        throw "Server product manifest Scene set is invalid."
    }
    $entityIds = @($manifest.entityModules.moduleId | Sort-Object)
    $hotfixIds = @($manifest.hotfixModules.moduleId | Sort-Object)
    if (($entityIds -join "|") -ne ((@($ExpectedEntityModules | Sort-Object)) -join "|") -or
        ($hotfixIds -join "|") -ne ((@($ExpectedHotfixModules | Sort-Object)) -join "|")) {
        throw "Server product module set is invalid."
    }
    $dependencyIds = @($manifest.portableDependencies.moduleId)
    foreach ($required in $RequiredPortableDependencies) {
        if ($dependencyIds -notcontains $required) {
            throw "Server product is missing required portable dependency: $required"
        }
    }
    $artifactIds = @($manifest.authorityArtifacts.moduleId | Sort-Object)
    if (($artifactIds -join "|") -ne ((@($ExpectedAuthorityArtifacts | Sort-Object)) -join "|")) {
        throw "Server product Authority artifact set is invalid."
    }
    $records = @($manifest.entityModules) + @($manifest.hotfixModules) +
        @($manifest.portableDependencies) + @($manifest.authorityArtifacts)
    foreach ($record in @($manifest.executable, $manifest.configuration) + $records) {
        Assert-ServerProductRecord $Root $record
    }
    $allModuleIds = @($records.moduleId)
    foreach ($forbidden in $ForbiddenModuleIds) {
        if ($allModuleIds -contains $forbidden) {
            throw "Server product contains forbidden module: $forbidden"
        }
    }
    $declaredDlls = @($manifest.entityModules.relativePath) + @($manifest.hotfixModules.relativePath) +
        @($manifest.portableDependencies.relativePath)
    $actualDlls = @(Get-ChildItem -LiteralPath $Root -File -Filter *.dll | ForEach-Object { $_.Name })
    if ((@($declaredDlls | Sort-Object) -join "|") -ne (@($actualDlls | Sort-Object) -join "|")) {
        throw "Server product directory contains missing or undeclared DLL files."
    }
    $runtimeDlls = @(Get-ServerProductRuntimeAssemblies $Root $ExpectedExecutable)
    if ((@($runtimeDlls | Sort-Object) -join "|") -ne (@($actualDlls | Sort-Object) -join "|")) {
        throw "Server product directory does not match its dependency manifest."
    }
    $artifactDirectory = Join-Path $Root "Authority"
    $rootPrefix = [System.IO.Path]::GetFullPath($Root)
    $directorySeparator = [System.IO.Path]::DirectorySeparatorChar.ToString()
    if (!$rootPrefix.EndsWith($directorySeparator, [System.StringComparison]::Ordinal)) {
        $rootPrefix += $directorySeparator
    }
    $actualArtifacts = if (Test-Path -LiteralPath $artifactDirectory -PathType Container) {
        @(Get-ChildItem -LiteralPath $artifactDirectory -Recurse -File | ForEach-Object {
            $fullPath = [System.IO.Path]::GetFullPath($_.FullName)
            if (!$fullPath.StartsWith($rootPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
                throw "Server product Authority artifact escapes its product root: $fullPath"
            }
            $fullPath.Substring($rootPrefix.Length).Replace(
                [System.IO.Path]::DirectorySeparatorChar,
                [char]'/')
        } | Sort-Object)
    } else {
        @()
    }
    $declaredArtifacts = @($manifest.authorityArtifacts.relativePath | Sort-Object)
    if (($actualArtifacts -join "|") -ne ($declaredArtifacts -join "|")) {
        throw "Server product Authority artifact directory does not match its manifest."
    }
    return $manifest
}
