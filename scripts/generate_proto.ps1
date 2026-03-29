param(
    [string]$ProtoDir = "proto",
    [string]$OutputDir = "Assets/Generated/Protobuf",
    [string]$GrpcToolsVersion = "2.64.0"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Resolve-ProtocPath
{
    param([string]$Version)

    $grpcRoot = Join-Path $env:USERPROFILE ".nuget\\packages\\grpc.tools"
    if (-not (Test-Path -LiteralPath $grpcRoot))
    {
        return $null
    }

    $candidate = Join-Path $grpcRoot "$Version\\tools\\windows_x64\\protoc.exe"
    if (Test-Path -LiteralPath $candidate)
    {
        return $candidate
    }

    $fallback = Get-ChildItem -LiteralPath $grpcRoot -Directory -ErrorAction SilentlyContinue |
        Sort-Object Name -Descending |
        ForEach-Object {
            $path = Join-Path $_.FullName "tools\\windows_x64\\protoc.exe"
            if (Test-Path -LiteralPath $path)
            {
                return $path
            }
        } | Select-Object -First 1

    return $fallback
}

if (-not (Test-Path -LiteralPath $ProtoDir))
{
    throw "Proto directory not found: '$ProtoDir'"
}

if (-not (Test-Path -LiteralPath $OutputDir))
{
    New-Item -ItemType Directory -Path $OutputDir | Out-Null
}

Write-Host "Restoring SharedProtocol packages (Grpc.Tools)..."
dotnet restore "SharedProtocol/SharedProtocol.csproj" | Out-Null

$protoc = Resolve-ProtocPath -Version $GrpcToolsVersion
if (-not $protoc)
{
    throw "Unable to locate protoc.exe from Grpc.Tools in NuGet cache. Try running 'dotnet restore' or install protoc."
}

$protoDirAbs = (Resolve-Path -LiteralPath $ProtoDir).Path
$outDirAbs = (Resolve-Path -LiteralPath $OutputDir).Path
$protoFiles = Get-ChildItem -LiteralPath $protoDirAbs -Filter "*.proto" -File | ForEach-Object { $_.FullName }

if (-not $protoFiles -or $protoFiles.Count -eq 0)
{
    throw "No .proto files found under '$ProtoDir'."
}

Write-Host "Using protoc: $protoc"
Write-Host "Proto path  : $protoDirAbs"
Write-Host "Output dir  : $outDirAbs"

& $protoc --proto_path=$protoDirAbs --csharp_out=$outDirAbs $protoFiles
if ($LASTEXITCODE -ne 0)
{
    throw "protoc failed with exit code $LASTEXITCODE"
}

Write-Host "Done. Verify with:"
Write-Host "  powershell -NoProfile -ExecutionPolicy Bypass -File scripts\\verify_protobuf.ps1"
