function Get-NetworkTestProductField($Fields, [string]$Key) {
    $fieldsProperty = $Fields.PSObject.Properties["fields"]
    if ($null -ne $fieldsProperty) {
        $Fields = $fieldsProperty.Value
    }
    $matches = @($Fields | Where-Object { $_.key -eq $Key })
    if ($matches.Count -ne 1 -or [string]::IsNullOrWhiteSpace($matches[0].value)) {
        throw "Network Test Product manifest field '$Key' is missing or duplicated."
    }
    return [string]$matches[0].value
}

function Get-NetworkTestProductArtifact($Manifest, [string]$RoleId) {
    $matches = @($Manifest.artifacts | Where-Object { $_.roleId -eq $RoleId })
    if ($matches.Count -ne 1) {
        throw "Network Test Product requires exactly one artifact '$RoleId'."
    }
    return $matches[0]
}

function Get-NetworkTestProductSha256([string]$Path) {
    if (!(Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Network Test Product file does not exist: $Path"
    }
    return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
}

function Resolve-NetworkTestProductPath([string]$Root, [string]$RelativePath) {
    if ([string]::IsNullOrWhiteSpace($RelativePath) -or [System.IO.Path]::IsPathRooted($RelativePath)) {
        throw "Network Test Product relative path is invalid: $RelativePath"
    }
    $fullRoot = [System.IO.Path]::GetFullPath($Root).TrimEnd([System.IO.Path]::DirectorySeparatorChar)
    $full = [System.IO.Path]::GetFullPath((Join-Path $fullRoot $RelativePath))
    if (!$full.StartsWith($fullRoot + [System.IO.Path]::DirectorySeparatorChar, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Network Test Product path escaped its root: $RelativePath"
    }
    return $full
}

function Assert-NetworkTestProductBuild {
    param(
        [Parameter(Mandatory = $true)][string]$Root,
        [Parameter(Mandatory = $true)][string]$ExpectedProductId,
        [Parameter(Mandatory = $true)][string]$ExpectedNetworkModelIdentity,
        [Parameter(Mandatory = $true)][string]$ExpectedRuntimeTopologyIdentity,
        [Parameter(Mandatory = $true)][string]$ExpectedPlayerRoleId,
        [Parameter(Mandatory = $true)][string[]]$ExpectedAdditionalArtifactRoleIds,
        [Parameter(Mandatory = $true)][string[]]$ExpectedScenes,
        [Parameter(Mandatory = $true)][string]$ExpectedBuildOptions,
        [Parameter(Mandatory = $true)][string]$ExpectedScriptingBackend
    )

    $fullRoot = [System.IO.Path]::GetFullPath($Root)
    $manifestPath = Join-Path $fullRoot "NetworkTestProduct.json"
    if (!(Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
        throw "Network Test Product manifest does not exist: $manifestPath"
    }
    $manifest = Get-Content -LiteralPath $manifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
    if ($manifest.schemaVersion -ne 3 -or $manifest.productId -ne $ExpectedProductId -or
        [string]::IsNullOrWhiteSpace($manifest.candidateId) -or
        [System.IO.Path]::GetFileName($fullRoot) -ne $manifest.candidateId -or
        [string]::IsNullOrWhiteSpace($manifest.candidateLabel) -or
        $manifest.sourceCommit.Length -ne 40 -or $manifest.sourceTreeHash.Length -ne 40 -or
        [string]::IsNullOrWhiteSpace($manifest.builtAtUtc) -or
        [string]::IsNullOrWhiteSpace($manifest.programIdentity) -or
        [string]::IsNullOrWhiteSpace($manifest.pipelineIdentity) -or
        $manifest.networkModelIdentity -ne $ExpectedNetworkModelIdentity -or
        $manifest.runtimeTopologyIdentity -ne $ExpectedRuntimeTopologyIdentity) {
        throw "Network Test Product manifest identity is invalid."
    }

    $artifacts = @($manifest.artifacts)
    $expectedRoles = @($ExpectedPlayerRoleId) + @($ExpectedAdditionalArtifactRoleIds) | Sort-Object
    $actualRoles = @($artifacts | ForEach-Object { [string]$_.roleId } | Sort-Object)
    if ($artifacts.Count -ne $expectedRoles.Count -or
        [string]::Join("|", $actualRoles) -ne [string]::Join("|", $expectedRoles)) {
        throw "Network Test Product runtime artifact roster is incompatible."
    }

    $roleIds = @{}
    $productIds = @{}
    $roots = @{}
    foreach ($artifact in $artifacts) {
        if ($null -eq $artifact -or [string]::IsNullOrWhiteSpace($artifact.roleId) -or
            @("UnityPlayer", "ManagedExecutable") -notcontains $artifact.kind -or
            [string]::IsNullOrWhiteSpace($artifact.productId) -or
            [string]::IsNullOrWhiteSpace($artifact.configurationIdentity) -or
            $roleIds.ContainsKey($artifact.roleId) -or $productIds.ContainsKey($artifact.productId) -or
            $roots.ContainsKey($artifact.root)) {
            throw "Network Test Product runtime artifact identity is invalid or duplicated."
        }
        $roleIds.Add([string]$artifact.roleId, $true)
        $productIds.Add([string]$artifact.productId, $true)
        $roots.Add([string]$artifact.root, $true)
        $artifactRoot = Resolve-NetworkTestProductPath $fullRoot $artifact.root
        $entryPoint = Resolve-NetworkTestProductPath $fullRoot $artifact.entryPoint
        if (!(Test-Path -LiteralPath $artifactRoot -PathType Container) -or
            !(Test-Path -LiteralPath $entryPoint -PathType Leaf) -or
            !$entryPoint.StartsWith($artifactRoot + [System.IO.Path]::DirectorySeparatorChar, [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "Network Test Product artifact closure is invalid: $($artifact.roleId)"
        }
        if ([string]::IsNullOrEmpty($artifact.manifestPath) -ne [string]::IsNullOrEmpty($artifact.manifestHash)) {
            throw "Network Test Product artifact manifest identity is incomplete: $($artifact.roleId)"
        }
        if (![string]::IsNullOrEmpty($artifact.manifestPath)) {
            $artifactManifest = Resolve-NetworkTestProductPath $fullRoot $artifact.manifestPath
            if (!$artifactManifest.StartsWith($artifactRoot + [System.IO.Path]::DirectorySeparatorChar, [System.StringComparison]::OrdinalIgnoreCase) -or
                (Get-NetworkTestProductSha256 $artifactManifest) -ne $artifact.manifestHash) {
                throw "Network Test Product artifact manifest is stale: $($artifact.roleId)"
            }
        }
    }

    $player = Get-NetworkTestProductArtifact $manifest $ExpectedPlayerRoleId
    if ($player.kind -ne "UnityPlayer" -or $player.root -ne "Player" -or
        $player.entryPoint -ne "Player/3C_Client.exe" -or
        (Get-NetworkTestProductField $player.fields "target") -ne "StandaloneWindows64" -or
        (Get-NetworkTestProductField $player.fields "buildOptions") -ne $ExpectedBuildOptions -or
        (Get-NetworkTestProductField $player.fields "scriptingBackend") -ne $ExpectedScriptingBackend -or
        (Get-NetworkTestProductField $player.fields "scenes") -ne [string]::Join("|", $ExpectedScenes)) {
        throw "Network Test Product Unity Player compile options or scenes are incompatible."
    }

    $tools = @($manifest.toolBundles)
    if ($tools.Count -lt 2 -or @($tools | Where-Object { $_.toolId -eq 'thirdperson.network-test-orchestrator' }).Count -ne 1) {
        throw "Network Test Product Tool Bundle roster is invalid."
    }
    $toolIds = @{}
    foreach ($tool in $tools) {
        if ($null -eq $tool -or [string]::IsNullOrWhiteSpace($tool.toolId) -or
            [string]::IsNullOrWhiteSpace($tool.toolVersion) -or $tool.contractVersion -le 0 -or
            [string]::IsNullOrWhiteSpace($tool.bundleHash) -or $toolIds.ContainsKey($tool.toolId)) {
            throw "Network Test Product Tool Bundle identity is invalid or duplicated."
        }
        $toolIds.Add([string]$tool.toolId, $true)
        $toolRoot = Resolve-NetworkTestProductPath $fullRoot $tool.root
        $toolEntryPoint = Resolve-NetworkTestProductPath $fullRoot $tool.entryPoint
        if (!(Test-Path -LiteralPath $toolRoot -PathType Container) -or
            !(Test-Path -LiteralPath $toolEntryPoint -PathType Leaf) -or
            !$toolEntryPoint.StartsWith($toolRoot + [System.IO.Path]::DirectorySeparatorChar, [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "Network Test Product Tool Bundle closure is invalid: $($tool.toolId)"
        }
    }
    if ($manifest.sessionPlan.schemaVersion -ne 1 -or
        [string]::IsNullOrWhiteSpace($manifest.sessionPlan.adapterId) -or
        @($manifest.sessionPlan.supportedSlotIds).Count -eq 0) {
        throw "Network Test Product Session Plan is invalid."
    }
    $adapterPath = Resolve-NetworkTestProductPath $fullRoot $manifest.sessionPlan.adapterPath
    if ((Get-NetworkTestProductSha256 $adapterPath) -ne $manifest.sessionPlan.adapterHash) {
        throw "Network Test Product Session adapter hash is stale."
    }

    $declared = @{}
    foreach ($record in @($manifest.files)) {
        if ($null -eq $record -or [string]::IsNullOrWhiteSpace($record.path) -or
            [string]::IsNullOrWhiteSpace($record.sha256) -or $declared.ContainsKey($record.path)) {
            throw "Network Test Product manifest contains an invalid or duplicate file record."
        }
        $declared.Add([string]$record.path, $record)
    }
    $actual = @(Get-ChildItem -LiteralPath $fullRoot -Recurse -File | Where-Object { $_.FullName -ne $manifestPath })
    if ($actual.Count -ne $declared.Count) {
        throw "Network Test Product exact file closure count does not match its manifest."
    }
    $rootPrefix = $fullRoot.TrimEnd([System.IO.Path]::DirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar
    foreach ($file in $actual) {
        if (!$file.FullName.StartsWith($rootPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "Network Test Product file escaped its product root: $($file.FullName)"
        }
        $relative = $file.FullName.Substring($rootPrefix.Length).Replace("\", "/")
        if (!$declared.ContainsKey($relative)) {
            throw "Network Test Product contains an undeclared file: $relative"
        }
        $record = $declared[$relative]
        if ([int64]$record.length -ne $file.Length -or (Get-NetworkTestProductSha256 $file.FullName) -ne $record.sha256) {
            throw "Network Test Product file closure mismatch: $relative"
        }
    }
    return $manifest
}
