param(
    [Parameter(Mandatory)][string]$InputBlock,
    [Parameter(Mandatory)][string]$LibraryDirectory,
    [Parameter(Mandatory)][string[]]$NodeNames,
    [Parameter(Mandatory)][string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
$libraryPath = [IO.Path]::GetFullPath($LibraryDirectory)
$outputPath = [IO.Path]::GetFullPath($OutputDirectory)
if (Test-Path -LiteralPath $outputPath) { throw 'Output directory already exists' }
$resolver = [Func[System.Runtime.Loader.AssemblyLoadContext, Reflection.AssemblyName, Reflection.Assembly]] {
    param($context, $name)
    $dependencyPath = Join-Path $libraryPath ($name.Name + '.dll')
    if (Test-Path -LiteralPath $dependencyPath) { return $context.LoadFromAssemblyPath($dependencyPath) }
    return $null
}
[System.Runtime.Loader.AssemblyLoadContext]::Default.add_Resolving($resolver)
try {
    [void][System.Runtime.Loader.AssemblyLoadContext]::Default.LoadFromAssemblyPath((Join-Path $libraryPath 'AnimeStudio.dll'))
    $game = [AnimeStudio.GameManager]::GetGame('ZZZ')
    $inputPath = [IO.Path]::GetFullPath($InputBlock)
    $inputStream = [IO.File]::OpenRead($inputPath)
    $offsetStream = [AnimeStudio.OffsetStream]::new($inputStream, 0)
    [void][IO.Directory]::CreateDirectory($outputPath)
    $records = [Collections.Generic.List[object]]::new()
    try {
        while ($offsetStream.Remaining -gt 0) {
            $start = $offsetStream.AbsolutePosition
            $offsetStream.Offset = $start
            $reader = [AnimeStudio.FileReader]::new($inputPath, $offsetStream, $true)
            try {
                if ($reader.FileType -ne [AnimeStudio.FileType]::MhyFile) { throw 'Unexpected container type' }
                $container = [AnimeStudio.MhyFile]::new($reader, $game)
                foreach ($node in $container.fileList) {
                    try {
                        $nodeName = [IO.Path]::GetFileName($node.path)
                        if ($NodeNames -cnotcontains $nodeName) { continue }
                        $destination = Join-Path $outputPath $nodeName
                        $target = [IO.File]::Open($destination, [IO.FileMode]::CreateNew)
                        try { $node.stream.Position = 0; $node.stream.CopyTo($target) }
                        finally { $target.Dispose() }
                        $records.Add([pscustomobject]@{ node = $node.path; containerOffset = $start; bytes = (Get-Item -LiteralPath $destination).Length; sha256 = (Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash })
                    }
                    finally { $node.stream.Dispose() }
                }
            }
            finally { $reader.Dispose() }
            if ($offsetStream.AbsolutePosition -le $start) { throw 'Container made no progress' }
        }
    }
    finally { $offsetStream.Dispose(); $inputStream.Dispose() }
    if ($records.Count -ne $NodeNames.Count) { throw 'Requested nodes were not all recovered exactly once' }
    $manifest = @{ input = $inputPath; inputSha256 = (Get-FileHash -LiteralPath $inputPath -Algorithm SHA256).Hash; library = $libraryPath; librarySha256 = (Get-FileHash -LiteralPath (Join-Path $libraryPath 'AnimeStudio.dll') -Algorithm SHA256).Hash; toolSha256 = (Get-FileHash -LiteralPath $PSCommandPath -Algorithm SHA256).Hash; nodes = $records }
    [IO.File]::WriteAllText((Join-Path $outputPath 'container-evidence.json'), ($manifest | ConvertTo-Json -Depth 6), [Text.UTF8Encoding]::new($false))
    $records | ConvertTo-Json -Depth 4
}
finally { [System.Runtime.Loader.AssemblyLoadContext]::Default.remove_Resolving($resolver) }
