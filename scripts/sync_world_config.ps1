param(
    [string]$ServerWorldConfigPath = "config/world.json",
    [string]$UnityWorldConfigPath = "Assets/MyAssets/Resources/TextAsset/GameWorld/WorldConfigData.json",
    [switch]$UpdateChunkSize
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $ServerWorldConfigPath))
{
    throw "Server world config not found: '$ServerWorldConfigPath'"
}

if (-not (Test-Path -LiteralPath $UnityWorldConfigPath))
{
    throw "Unity world config not found: '$UnityWorldConfigPath'"
}

$server = Get-Content -LiteralPath $ServerWorldConfigPath -Raw | ConvertFrom-Json
$unity = Get-Content -LiteralPath $UnityWorldConfigPath -Raw | ConvertFrom-Json

function Set-IfPresent
{
    param(
        [Parameter(Mandatory = $true)]
        [object]$Target,
        [Parameter(Mandatory = $true)]
        [string]$PropertyName,
        [Parameter(Mandatory = $true)]
        [object]$Value
    )

    if ($null -eq $Value)
    {
        return
    }

    $Target | Add-Member -MemberType NoteProperty -Name $PropertyName -Value $Value -Force
}

if ($UpdateChunkSize)
{
    Set-IfPresent $unity "ChunkSize" $server.ChunkSize
}

Set-IfPresent $unity "RenderDistance" $server.RenderDistance
Set-IfPresent $unity "SimulationDistance" $server.SimulationDistance

Set-IfPresent $unity "GlobalWaterLevel" $server.Water.GlobalWaterLevel
Set-IfPresent $unity "RiverCenterThreshold" $server.Water.RiverCenterThreshold
Set-IfPresent $unity "RiverBankThreshold" $server.Water.RiverBankThreshold
Set-IfPresent $unity "EnableRivers" $server.Water.EnableRivers
Set-IfPresent $unity "EnableLakes" $server.Water.EnableLakes
Set-IfPresent $unity "UseImprovedRivers" $server.Water.UseImprovedRivers
Set-IfPresent $unity "UseImprovedLakes" $server.Water.UseImprovedLakes

Set-IfPresent $unity "HydrologySmoothIterations" $server.Water.HydrologySmoothIterations
Set-IfPresent $unity "HydrologySmoothBlend" $server.Water.HydrologySmoothBlend
Set-IfPresent $unity "HydrologyShorePush" $server.Water.HydrologyShorePush
Set-IfPresent $unity "HydrologySlopePenalty" $server.Water.HydrologySlopePenalty
Set-IfPresent $unity "HydrologyFlowGain" $server.Water.HydrologyFlowGain
Set-IfPresent $unity "HydrologyContinuityWeight" $server.Water.HydrologyContinuityWeight
Set-IfPresent $unity "HydrologyEdgeFlowBias" $server.Water.HydrologyEdgeFlowBias
Set-IfPresent $unity "HydrologyEdgeTangentWeight" $server.Water.HydrologyEdgeTangentWeight
Set-IfPresent $unity "HydrologyEdgeFlowLockWeight" $server.Water.HydrologyEdgeFlowLockWeight
Set-IfPresent $unity "HydrologyEdgeBlendRadius" $server.Water.HydrologyEdgeBlendRadius
Set-IfPresent $unity "HydrologyEdgeStabilityIterations" $server.Water.HydrologyEdgeStabilityIterations
Set-IfPresent $unity "HydrologyEdgeStabilityWeight" $server.Water.HydrologyEdgeStabilityWeight
Set-IfPresent $unity "HydrologyEdgeVarianceClamp" $server.Water.HydrologyEdgeVarianceClamp
Set-IfPresent $unity "HydrologyWaterTableClampWeight" $server.Water.HydrologyWaterTableClampWeight
Set-IfPresent $unity "HydrologyWaterTableClampRange" $server.Water.HydrologyWaterTableClampRange
Set-IfPresent $unity "HydrologyWaterTableSlopeWeight" $server.Water.HydrologyWaterTableSlopeWeight
Set-IfPresent $unity "HydrologyFlowPersistence" $server.Water.HydrologyFlowPersistence
Set-IfPresent $unity "HydrologyGradientWeight" $server.Water.HydrologyGradientWeight
Set-IfPresent $unity "HydrologyGradientSlopeWeight" $server.Water.HydrologyGradientSlopeWeight
Set-IfPresent $unity "HydrologyGradientClamp" $server.Water.HydrologyGradientClamp
Set-IfPresent $unity "HydrologyGradientStabilityIterations" $server.Water.HydrologyGradientStabilityIterations
Set-IfPresent $unity "HydrologyGradientStabilityBlend" $server.Water.HydrologyGradientStabilityBlend
Set-IfPresent $unity "HydrologyCurvatureWeight" $server.Water.HydrologyCurvatureWeight
Set-IfPresent $unity "HydrologySeamRelaxIterations" $server.Water.HydrologySeamRelaxIterations
Set-IfPresent $unity "HydrologySeamRelaxBlend" $server.Water.HydrologySeamRelaxBlend
Set-IfPresent $unity "HydrologyWarpFrequency" $server.Water.HydrologyWarpFrequency
Set-IfPresent $unity "HydrologyWarpAmplitude" $server.Water.HydrologyWarpAmplitude

Set-IfPresent $unity "RiverFlowAlignmentWeight" $server.Water.RiverFlowAlignmentWeight
Set-IfPresent $unity "RiverGradientPenalty" $server.Water.RiverGradientPenalty
Set-IfPresent $unity "RiverHeadwaterStabilityWeight" $server.Water.RiverHeadwaterStabilityWeight
Set-IfPresent $unity "RiverAnisotropyWeight" $server.Water.RiverAnisotropyWeight
Set-IfPresent $unity "RiverReliefPenaltyWeight" $server.Water.RiverReliefPenaltyWeight
Set-IfPresent $unity "RiverBankErosionWeight" $server.Water.RiverBankErosionWeight
Set-IfPresent $unity "LakeRimErosionWeight" $server.Water.LakeRimErosionWeight
Set-IfPresent $unity "LakeInflowBlendWeight" $server.Water.LakeInflowBlendWeight
Set-IfPresent $unity "RiverNoiseScale" $server.Water.RiverNoiseScale
Set-IfPresent $unity "RiverDepth" $server.Water.RiverDepth
Set-IfPresent $unity "RiverIntensitySmoothIterations" $server.Water.RiverIntensitySmoothIterations
Set-IfPresent $unity "RiverIntensitySmoothBlend" $server.Water.RiverIntensitySmoothBlend
Set-IfPresent $unity "RiverConfluenceBoost" $server.Water.RiverConfluenceBoost

Set-IfPresent $unity "EnableCaves" $server.Caves.EnableCaves
Set-IfPresent $unity "UseImprovedCaves" $server.Caves.UseImprovedCaves
Set-IfPresent $unity "UseRegionalMainCaves" $server.Caves.UseRegionalMainCaves
Set-IfPresent $unity "RegionalMainCaveRegionSizeChunks" $server.Caves.RegionalMainCaveRegionSizeChunks
Set-IfPresent $unity "RegionalMainCaveWormCountMin" $server.Caves.RegionalMainCaveWormCountMin
Set-IfPresent $unity "RegionalMainCaveWormCountMax" $server.Caves.RegionalMainCaveWormCountMax
Set-IfPresent $unity "RegionalMainCaveStepsMin" $server.Caves.RegionalMainCaveStepsMin
Set-IfPresent $unity "RegionalMainCaveStepsMax" $server.Caves.RegionalMainCaveStepsMax
Set-IfPresent $unity "RegionalMainCaveMinY" $server.Caves.RegionalMainCaveMinY
Set-IfPresent $unity "RegionalMainCaveMaxY" $server.Caves.RegionalMainCaveMaxY
Set-IfPresent $unity "RegionalMainCaveRadiusMin" $server.Caves.RegionalMainCaveRadiusMin
Set-IfPresent $unity "RegionalMainCaveRadiusMax" $server.Caves.RegionalMainCaveRadiusMax
Set-IfPresent $unity "CaveStabilitySmoothIterations" $server.Caves.StabilitySmoothIterations
Set-IfPresent $unity "CaveStabilitySmoothBlend" $server.Caves.StabilitySmoothBlend
Set-IfPresent $unity "CaveSupportDensity" $server.Caves.SupportDensity
Set-IfPresent $unity "SupportHydrationBias" $server.Caves.SupportHydrationBias
Set-IfPresent $unity "SupportFlowBias" $server.Caves.SupportFlowBias
Set-IfPresent $unity "HydrologyStabilityWeight" $server.Caves.HydrologyStabilityWeight
Set-IfPresent $unity "FlowStabilityWeight" $server.Caves.FlowStabilityWeight
Set-IfPresent $unity "RoughnessStabilityWeight" $server.Caves.RoughnessStabilityWeight
Set-IfPresent $unity "RiverSuppressionWeight" $server.Caves.RiverSuppressionWeight
Set-IfPresent $unity "MoistureRetentionWeight" $server.Caves.MoistureRetentionWeight

Set-IfPresent $unity "LakeMinDepth" $server.Lakes.MinDepth
Set-IfPresent $unity "LakeMaxDepth" $server.Lakes.MaxDepth
Set-IfPresent $unity "LakeMaxRadius" $server.Lakes.MaxRadius
Set-IfPresent $unity "LakeBasinSmoothIterations" $server.Lakes.LakeBasinSmoothIterations
Set-IfPresent $unity "LakeSpawnWeightBias" $server.Lakes.SpawnWeightBias
Set-IfPresent $unity "LakeShorelineBlend" $server.Lakes.ShorelineBlend
Set-IfPresent $unity "RiverProximitySuppression" $server.Lakes.RiverProximitySuppression

$json = $unity | ConvertTo-Json -Depth 64
[System.IO.File]::WriteAllText($UnityWorldConfigPath, $json + [Environment]::NewLine, [System.Text.Encoding]::UTF8)

Write-Host "Synced shared world generation keys."
Write-Host "Server: $ServerWorldConfigPath"
Write-Host "Unity : $UnityWorldConfigPath"
