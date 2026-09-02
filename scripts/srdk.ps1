$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
try { [Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12 } catch {}

$ReleaseSourceId = 'gh:LiPolymer/ShulkerRDK@B0.16'

$BaseDir = $PSScriptRoot
$BinDir = Join-Path $BaseDir 'shulker/local/bin'
$BinPath = Join-Path $BinDir 'srdk.exe'
$MarkerPath = Join-Path $BinDir 'srdk.version'

function Get-ReleaseAssets([string]$SourceId) {
    $m = [regex]::Match($SourceId, '^(?<platform>[^:]+):(?<repo>[^@]+)@(?<tag>.+)$')
    if (-not $m.Success) {
        throw "无效的版本源 identifier [$SourceId] , 约定为 platform:user/repo@tag"
    }
    $platform = $m.Groups['platform'].Value
    $repo = $m.Groups['repo'].Value
    $tag = [Uri]::EscapeDataString($m.Groups['tag'].Value)
    $headers = @{ 'User-Agent' = 'ShulkerRDK' }

    switch ($platform) {
        'gh' {
            $headers['Accept'] = 'application/vnd.github+json'
            $release = Invoke-RestMethod -Uri "https://api.github.com/repos/$repo/releases/tags/$tag" -Headers $headers
            foreach ($asset in @($release.assets)) {
                if ($null -eq $asset) { continue }
                $sha256 = $null
                if ($asset.digest -is [string] -and $asset.digest.StartsWith('sha256:')) {
                    $sha256 = $asset.digest.Substring(7)
                }
                [PSCustomObject]@{ Name = $asset.name; Url = $asset.browser_download_url; Sha256 = $sha256 }
            }
        }
        'gl' {
            $project = [Uri]::EscapeDataString($repo)
            $release = Invoke-RestMethod -Uri "https://gitlab.com/api/v4/projects/$project/releases/$tag" -Headers $headers
            foreach ($link in @($release.assets.links)) {
                if ($null -eq $link) { continue }
                $url = $link.direct_asset_url
                if (-not $url) { $url = $link.url }
                [PSCustomObject]@{ Name = $link.name; Url = $url; Sha256 = $null }
            }
        }
        default {
            throw "未知的扩展源平台[$platform]"
        }
    }
}

function Get-TargetAssetNames {
    $arch = $env:PROCESSOR_ARCHITEW6432
    if (-not $arch) { $arch = $env:PROCESSOR_ARCHITECTURE }
    if ($arch -eq 'ARM64') { return ,@('srdk_arm64.exe', 'srdk.exe') }
    return ,@('srdk.exe')
}

function Test-NeedsInstall {
    if (-not (Test-Path -LiteralPath $BinPath)) { return $true }
    if (-not (Test-Path -LiteralPath $MarkerPath)) { return $false }
    $installed = Get-Content -LiteralPath $MarkerPath -Raw
    if (-not $installed) { return $false }
    return $installed.Trim() -ne $ReleaseSourceId
}

function Install-Srdk {
    Write-Host "[srdk] 正在从 [$ReleaseSourceId] 获取 srdk..."
    try {
        $assets = @(Get-ReleaseAssets -SourceId $ReleaseSourceId)
    } catch {
        Write-Host "[srdk] 无法获取 release 信息: $($_.Exception.Message)"
        exit 1
    }

    $candidates = Get-TargetAssetNames
    $asset = $null
    foreach ($name in $candidates) {
        $asset = $assets | Where-Object { $_.Name -eq $name } | Select-Object -First 1
        if ($null -ne $asset) { break }
    }
    if ($null -eq $asset) {
        $names = ($assets | ForEach-Object { $_.Name }) -join ', '
        Write-Host "[srdk] release [$ReleaseSourceId] 中未找到资产 [$($candidates -join ' / ')] , 可用资产: $names"
        exit 1
    }

    New-Item -ItemType Directory -Force -Path $BinDir | Out-Null
    $tmpPath = "$BinPath.download"
    try {
        Invoke-WebRequest -Uri $asset.Url -OutFile $tmpPath -UseBasicParsing -Headers @{ 'User-Agent' = 'ShulkerRDK' }
    } catch {
        if (Test-Path -LiteralPath $tmpPath) { Remove-Item -LiteralPath $tmpPath -Force }
        Write-Host "[srdk] 下载失败: $($_.Exception.Message)"
        exit 1
    }

    if ($asset.Sha256) {
        $actual = (Get-FileHash -LiteralPath $tmpPath -Algorithm SHA256).Hash.ToLowerInvariant()
        if ($actual -ne $asset.Sha256.ToLowerInvariant()) {
            Remove-Item -LiteralPath $tmpPath -Force
            Write-Host "[srdk] SHA256 校验失败 (期望 $($asset.Sha256) , 实际 $actual)"
            exit 1
        }
    }

    Move-Item -LiteralPath $tmpPath -Destination $BinPath -Force
    Set-Content -LiteralPath $MarkerPath -Value $ReleaseSourceId -Encoding ASCII
    Write-Host "[srdk] 已安装 $BinPath"
}

if (Test-NeedsInstall) { Install-Srdk }

Push-Location $BaseDir
try {
    & $BinPath @args
    $code = $LASTEXITCODE
} finally {
    Pop-Location
}
exit $code
