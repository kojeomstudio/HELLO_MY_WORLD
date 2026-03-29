param(
    [string]$ProtoDir = "proto",
    [string]$GeneratedDir = "Assets/Generated/Protobuf"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Get-CombinedHash {
    param([System.IO.FileInfo[]]$Files)
    if (-not $Files -or $Files.Count -eq 0) {
        return ""
    }

    $builder = [System.Text.StringBuilder]::new()
    foreach ($file in ($Files | Sort-Object FullName)) {
        $hash = Get-FileHash -Path $file.FullName -Algorithm SHA256
        [void]$builder.AppendLine("$($file.FullName):$($hash.Hash)")
    }

    $bytes = [System.Text.Encoding]::UTF8.GetBytes($builder.ToString())
    $final = [System.Security.Cryptography.SHA256]::Create().ComputeHash($bytes)
    return ([System.BitConverter]::ToString($final) -replace "-", "").ToLowerInvariant()
}

if (-not (Test-Path $ProtoDir)) {
    Write-Error "Proto directory '$ProtoDir' not found."
}
if (-not (Test-Path $GeneratedDir)) {
    Write-Error "Generated protobuf directory '$GeneratedDir' not found."
}

$protoFiles = Get-ChildItem -Path $ProtoDir -Filter *.proto -File -Recurse
$generatedFiles = Get-ChildItem -Path $GeneratedDir -Filter *.cs -File -Recurse
if (-not $protoFiles) {
    Write-Error "No .proto files found under '$ProtoDir'."
}
if (-not $generatedFiles) {
    Write-Error "No generated C# protobuf files found under '$GeneratedDir'. Run protoc to generate them."
}

$protoHash = Get-CombinedHash -Files $protoFiles
$generatedHash = Get-CombinedHash -Files $generatedFiles
$protoNewest = ($protoFiles | Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1).LastWriteTimeUtc
$generatedNewest = ($generatedFiles | Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1).LastWriteTimeUtc

Write-Host "Proto combined hash:      $protoHash"
Write-Host "Generated C# combined:    $generatedHash"
Write-Host "Newest proto timestamp:   $protoNewest"
Write-Host "Newest generated file:    $generatedNewest"

if ($protoNewest -gt $generatedNewest) {
    Write-Warning "Proto schemas are newer than generated C# output. Re-run: protoc -I proto --csharp_out=Assets/Generated/Protobuf proto/*.proto"
    exit 2
}

Write-Host "Generated protobufs are up to date relative to proto sources."
exit 0
