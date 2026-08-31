function Assert-RollbackGmProduct {
    param($Product, $RelayManifest, [string]$Root)
    $gmPath = Join-Path $Root 'Gm\GmServerManifest.json'
    $relayPath = Join-Path $Root 'Server\RelayQueryManifest.json'
    $clientPath = Join-Path $Root 'Gm\GmConsoleManifest.json'
    $gm = Get-Content -LiteralPath $gmPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $query = Get-Content -LiteralPath $relayPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $client = Get-Content -LiteralPath $clientPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $artifact = Get-NetworkTestProductArtifact $Product 'development-gm-server'
    $gmHash = Get-NetworkTestProductSha256 $gmPath
    if ($artifact.kind -ne 'ManagedExecutable' -or $artifact.productId -ne 'thirdperson.server-product.development-gm' -or
        $artifact.entryPoint -ne 'Gm/ThirdPerson.Development.Gm.Service.exe' -or $artifact.manifestPath -ne 'Gm/GmServerManifest.json' -or
        $artifact.configurationIdentity -ne $gmHash -or $artifact.manifestHash -ne $gmHash) {
        throw 'GM artifact does not match the published service manifest.'
    }
    foreach ($configuration in @($gm, $query, $client)) {
        if ($configuration.schemaVersion -ne 1 -or $configuration.buildId -ne $Product.buildId -or
            $configuration.sessionId -ne $RelayManifest.sessionId) { throw 'GM configuration identity mismatch.' }
    }
    $gmEndpoint = "http://127.0.0.1:$($gm.http.listenPort)/"
    $relayEndpoint = "http://127.0.0.1:$($query.http.listenPort)/"
    if ($gm.http.listenAddress -ne '127.0.0.1' -or $query.http.listenAddress -ne '127.0.0.1' -or
        $gm.http.listenPort -eq $query.http.listenPort -or $client.endpoint -ne $gmEndpoint -or
        $gm.relayQueryEndpoint -ne $relayEndpoint -or $gm.http.accessToken -cne $client.accessToken -or
        $gm.relayQueryToken -cne $query.http.accessToken -or $gm.http.accessToken -ceq $gm.relayQueryToken -or
        $gm.http.accessToken -cnotmatch '^[0-9a-f]{64}$' -or $gm.relayQueryToken -cnotmatch '^[0-9a-f]{64}$') {
        throw 'GM endpoint or development access configuration mismatch.'
    }
    $expectedFields = @{
        endpoint = $gmEndpoint
        relayQueryManifestHash = Get-NetworkTestProductSha256 $relayPath
        consoleManifestHash = Get-NetworkTestProductSha256 $clientPath
    }
    foreach ($fieldName in $expectedFields.Keys) {
        $field = @($artifact.fields | Where-Object { $_.key -eq $fieldName })
        if ($field.Count -ne 1 -or $field[0].value -ne $expectedFields[$fieldName]) {
            throw "GM artifact field mismatch: $fieldName"
        }
    }
    return [pscustomobject]@{ Gm = $gm; Relay = $query; Client = $client }
}

function Wait-RollbackToolService {
    param([System.Diagnostics.Process]$Process, [string]$Uri, [string]$Token,
        [string]$BuildId, [string]$SessionId)
    $deadline = (Get-Date).AddSeconds(10)
    do {
        $Process.Refresh()
        if ($Process.HasExited) { throw "Tool service process exited: $($Process.ProcessName)" }
        $identity = $null
        try {
            $identity = Invoke-RestMethod -Uri $Uri -Headers @{ Authorization = "Bearer $Token" } -TimeoutSec 2
        } catch {
            $identity = $null
        }
        if ($null -ne $identity) {
            if ($identity.protocolVersion -ne 1 -or $identity.buildId -ne $BuildId -or $identity.sessionId -ne $SessionId) {
                throw "Tool service identity mismatch: $Uri"
            }
            return $identity
        }
        Start-Sleep -Milliseconds 100
    } while ((Get-Date) -lt $deadline)
    throw "Tool service did not become ready: $Uri"
}
