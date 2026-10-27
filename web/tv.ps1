<#
.SYNOPSIS
  Construit, installe, lance, débogue ou désinstalle Quiet Home sur la TV de test.

.EXAMPLE
  .\web\tv.ps1 -TvIp <IP_DE_LA_TV> -Action deploy -CertProfile QuietHomeTV
  .\web\tv.ps1 -TvIp <IP_DE_LA_TV> -Action debug
  .\web\tv.ps1 -TvIp <IP_DE_LA_TV> -Action uninstall
#>
param(
    [Parameter(Mandatory = $true)] [string] $TvIp,
    [ValidateSet('connect', 'build', 'deploy', 'run', 'debug', 'uninstall', 'info')]
    [string] $Action = 'deploy',
    [string] $CertProfile = 'QuietHomeTV',
    [string] $TizenStudio = 'C:\tizen-studio'
)

$ErrorActionPreference = 'Stop'
$AppId   = 'QuietHome1.QuietHome'
$PkgId   = 'QuietHome1'
$Root    = $PSScriptRoot
$Src     = Join-Path $Root 'src'
$Out     = Join-Path $Root 'dist'
$Tizen   = Join-Path $TizenStudio 'tools\ide\bin\tizen.bat'
$Sdb     = Join-Path $TizenStudio 'tools\sdb.exe'
$Serial  = "${TvIp}:26101"

foreach ($tool in @($Tizen, $Sdb)) {
    if (-not (Test-Path $tool)) { throw "Introuvable : $tool (installez Tizen Studio ou passez -TizenStudio)" }
}

function Invoke-Checked([string] $Exe, [string[]] $ArgList) {
    Write-Host "> $Exe $($ArgList -join ' ')" -ForegroundColor DarkGray
    & $Exe @ArgList
    if ($LASTEXITCODE -ne 0) { throw "Échec ($LASTEXITCODE) : $Exe $($ArgList -join ' ')" }
}

function Connect-Tv {
    Invoke-Checked $Sdb @('connect', $TvIp)
    $devices = & $Sdb devices
    if (-not ($devices -match [regex]::Escape($Serial))) { throw "TV $Serial non visible dans 'sdb devices'. Mode développeur actif avec l'IP de ce PC ?" }
}

function Build-Wgt {
    # Copie propre de src/ : rien d'autre (tests, docs, .git) n'entre dans le paquet.
    if (Test-Path $Out) { Remove-Item -Recurse -Force $Out }
    New-Item -ItemType Directory -Force (Join-Path $Out 'app') | Out-Null
    Copy-Item -Recurse (Join-Path $Src '*') (Join-Path $Out 'app')
    Invoke-Checked $Tizen @('package', '-t', 'wgt', '-s', $CertProfile, '--', (Join-Path $Out 'app'))
    $wgt = Get-ChildItem (Join-Path $Out 'app') -Filter *.wgt | Select-Object -First 1
    if (-not $wgt) { throw 'Aucun .wgt produit' }
    Move-Item $wgt.FullName (Join-Path $Out 'QuietHome.wgt') -Force
    Write-Host "Paquet : $(Join-Path $Out 'QuietHome.wgt')" -ForegroundColor Green
}

switch ($Action) {
    'connect' { Connect-Tv }
    'build'   { Build-Wgt }
    'deploy'  {
        Connect-Tv
        Build-Wgt
        Invoke-Checked $Tizen @('install', '-n', 'QuietHome.wgt', '-s', $Serial, '--', $Out)
        Invoke-Checked $Tizen @('run', '-p', $AppId, '-s', $Serial)
    }
    'run'     { Connect-Tv; Invoke-Checked $Tizen @('run', '-p', $AppId, '-s', $Serial) }
    'debug'   {
        # Lance l'app en mode debug et expose le Web Inspector sur le PC.
        Connect-Tv
        $outText = (& $Sdb -s $Serial shell 0 debug $AppId) -join "`n"
        Write-Host $outText
        if ($outText -notmatch 'port:\s*(\d+)') { throw 'Port de debug introuvable dans la réponse de la TV.' }
        $port = $Matches[1]
        Invoke-Checked $Sdb @('-s', $Serial, 'forward', "tcp:$port", "tcp:$port")
        Write-Host "Ouvrez Chrome sur http://localhost:$port (ou chrome://inspect > Configure > localhost:$port)" -ForegroundColor Green
    }
    'uninstall' { Connect-Tv; Invoke-Checked $Tizen @('uninstall', '-p', $AppId, '-s', $Serial) }
    'info'    {
        Connect-Tv
        & $Sdb -s $Serial capability
        & $Sdb -s $Serial shell 0 vd_applist | Select-String $PkgId
    }
}
