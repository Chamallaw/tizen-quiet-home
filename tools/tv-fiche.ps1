<#
.SYNOPSIS
  Fiche technique de la TV + sauvegarde des sélecteurs Home/startup d'origine.
  STRICTEMENT EN LECTURE SEULE : aucune commande ne modifie la TV.

.DESCRIPTION
  1. API REST locale Samsung (port 8001) : modèle, OS, résolution. Mode développeur non requis.
  2. sdb capability : version de plateforme, architecture CPU, profil.
  3. sdb shell : lecture des clés vconf Home/startup et du runtime .NET.
     Sur firmware grand public, sdb shell est une liste blanche : une commande
     refusée renvoie "closed". C'est en soi une information (porte go/no-go).

  Le résultat est écrit dans .local/fiche-<date>.txt et .json.
  GARDEZ LE PREMIER FICHIER : il contient les valeurs d'origine à restaurer.

.EXAMPLE
  .\tools\tv-fiche.ps1 -TvIp <IP_DE_LA_TV>
#>
param(
    [Parameter(Mandatory = $true)] [string] $TvIp,
    [string] $Sdb = 'C:\tizen-studio\tools\sdb.exe'
)

$ErrorActionPreference = 'Continue'
$Root   = Split-Path -Parent $PSScriptRoot
$OutDir = Join-Path $Root '.local'
$Serial = "${TvIp}:26101"
$Stamp  = Get-Date -Format 'yyyyMMdd-HHmmss'
New-Item -ItemType Directory -Force $OutDir | Out-Null

$report = [ordered]@{ date = (Get-Date).ToString('s'); tvIp = $TvIp }

# 1. API REST locale (lecture seule, sans authentification depuis le LAN).
try {
    $info = Invoke-RestMethod -Uri "http://${TvIp}:8001/api/v2/" -TimeoutSec 5
    $report.rest = $info.device
} catch {
    $report.rest = "indisponible : $($_.Exception.Message)"
}

# 2 et 3 : nécessitent sdb (Tizen Studio) et le mode développeur pointant vers ce PC.
if (Test-Path $Sdb) {
    & $Sdb connect $TvIp | Out-Null
    $report.sdbDevices = (& $Sdb devices) -join "`n"
    $report.capability = (& $Sdb -s $Serial capability) -join "`n"

    $probes = [ordered]@{
        home_appid             = 'vconftool get db/appfw/home_appid'
        startup_appid          = 'vconftool get db/appfw/startup_appid'
        booting_appid_backup   = 'vconftool get db/appfw/booting_appid_backup'
        first_screen_type      = 'vconftool get db/first_screen/type'
        tizen_release          = 'cat /etc/tizen-release'
        dotnet_runtime         = 'readlink -f /usr/share/dotnet.tizen/netcoreapp'
        dotnet_dirs            = 'ls /usr/share/dotnet.tizen/'
        whoami                 = 'id'
        arch                   = 'uname -m'
    }
    $report.shell = [ordered]@{}
    foreach ($k in $probes.Keys) {
        $out = (& $Sdb -s $Serial shell $probes[$k] 2>&1) -join "`n"
        $report.shell[$k] = [ordered]@{ cmd = $probes[$k]; exit = $LASTEXITCODE; out = $out.Trim() }
    }
} else {
    $report.sdb = "sdb introuvable ($Sdb) : seule la partie REST a été collectée"
}

$json = $report | ConvertTo-Json -Depth 6
$jsonPath = Join-Path $OutDir "fiche-$Stamp.json"
$json | Out-File -Encoding utf8 $jsonPath
Write-Host $json
Write-Host "`nFiche écrite : $jsonPath" -ForegroundColor Green
Write-Host 'Conservez la PREMIÈRE fiche : elle contient les valeurs Home/startup d''origine.' -ForegroundColor Yellow
