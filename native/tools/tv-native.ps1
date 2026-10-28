<#
.SYNOPSIS
  Build, signature, installation et suivi du launcher natif Quiet Home (QHLauncher).

.EXAMPLE
  .\native\tools\tv-native.ps1 -Action test
  .\native\tools\tv-native.ps1 -Action deploy -CertProfile QuietHomeTV
  .\native\tools\tv-native.ps1 -Action logs
  .\native\tools\tv-native.ps1 -Action uninstall

.NOTES
  Actions :
    test       tests unitaires du Core (PC uniquement)
    build      compile et assemble dist\QHLauncher-<v>-unsigned.tpk
    sign       signe le .tpk avec le profil de certificat Samsung
    deploy     build + sign + install + run
    install    installe le dernier .tpk signé
    run        lance l'app sur la TV
    logs       affiche les logs UDP envoyés par l'app (port 5140)
    uninstall  désinstalle l'app (la TV revient exactement à son état d'avant)
#>
param(
    [ValidateSet('test', 'build', 'sign', 'deploy', 'install', 'run', 'logs', 'uninstall')]
    [string] $Action = 'deploy',
    # IP de la TV : paramètre, ou variable d'environnement QH_TV_IP.
    [string] $TvIp = $env:QH_TV_IP,
    [string] $CertProfile = 'QuietHomeTV',
    [string] $Version = '0.1.0',
    [string] $TizenStudio = 'C:\tizen-studio',
    [int] $LogPort = 5140
)

$ErrorActionPreference = 'Stop'
if ($Action -notin @('test', 'build', 'sign', 'logs') -and -not $TvIp) { throw 'Indiquez -TvIp <IP_DE_LA_TV> (ou la variable QH_TV_IP).' }
$PkgId   = 'QHLauncher'
$AppId   = 'QHLauncher.Home'
$Native  = Split-Path -Parent $PSScriptRoot
$Dist    = Join-Path $Native 'dist'
$Project = Join-Path $Native 'src\QuietHome.Launcher\QuietHome.Launcher.csproj'
$BinDir  = Join-Path $Native 'src\QuietHome.Launcher\bin\Release\netcoreapp3.1'
$Unsigned = Join-Path $Dist "$PkgId-$Version-unsigned.tpk"
$SignedDir = Join-Path $Dist 'signed'
$Serial  = "${TvIp}:26101"
$Dotnet  = Join-Path $env:USERPROFILE '.dotnet\dotnet.exe'
if (-not (Test-Path $Dotnet)) { $Dotnet = 'dotnet' }
$Tizen   = Join-Path $TizenStudio 'tools\ide\bin\tizen.bat'
$Sdb     = Join-Path $TizenStudio 'tools\sdb.exe'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_NOLOGO = '1'

function Invoke-Checked([string] $Exe, [string[]] $ArgList) {
    Write-Host "> $Exe $($ArgList -join ' ')" -ForegroundColor DarkGray
    & $Exe @ArgList
    if ($LASTEXITCODE -ne 0) { throw "Échec ($LASTEXITCODE) : $Exe" }
}

function Connect-Tv {
    & $Sdb connect $TvIp | Out-Null
    if (-not ((& $Sdb devices) -match [regex]::Escape($Serial))) {
        throw "TV $Serial absente de 'sdb devices' : mode développeur actif avec l'IP de ce PC ?"
    }
}

function Get-SignedTpk {
    $tpk = Get-ChildItem $SignedDir -Filter *.tpk -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if (-not $tpk) { throw "Aucun .tpk signé dans $SignedDir : lancez -Action sign" }
    return $tpk.FullName
}

function Build-Tpk {
    Invoke-Checked $Dotnet @('build', $Project, '-c', 'Release', "-p:Version=$Version", '--nologo', '-v', 'q')
    $PackArgs = @('-I', (Join-Path $PSScriptRoot 'pack_tpk.py'), $BinDir,
        (Join-Path $Native 'src\QuietHome.Launcher\tizen-manifest.xml'), (Join-Path $Native 'res'), $Version, $Unsigned)
    # Réglages propres à cette TV (lieu de la météo) : .local/ n'est jamais publié, seul le paquet les embarque.
    $LocalJson = Join-Path (Split-Path -Parent $Native) '.local\quiethome.json'
    if (Test-Path $LocalJson) { $PackArgs += $LocalJson }
    Invoke-Checked 'python' $PackArgs
}

function Sign-Tpk {
    if (-not (Test-Path $Unsigned)) { throw "$Unsigned absent : lancez -Action build" }
    if (Test-Path $SignedDir) { Remove-Item -Recurse -Force $SignedDir }
    New-Item -ItemType Directory -Force $SignedDir | Out-Null
    # Le CLI peut renvoyer 0 en cas d'erreur : on vérifie la présence des signatures dans le paquet.
    Invoke-Checked $Tizen @('package', '-t', 'tpk', '-s', $CertProfile, '-o', $SignedDir, '--', $Unsigned)
    $tpk = Get-SignedTpk
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $zip = [IO.Compression.ZipFile]::OpenRead($tpk)
    try { $names = $zip.Entries | ForEach-Object FullName } finally { $zip.Dispose() }
    if (-not ($names -contains 'author-signature.xml' -and $names -contains 'signature1.xml')) {
        throw "Le paquet n'est pas signé (profil '$CertProfile' introuvable ou invalide ?)"
    }
    Write-Host "Signé : $tpk" -ForegroundColor Green
}

function Install-Tpk {
    Connect-Tv
    $tpk = Get-SignedTpk
    $out = (& $Sdb -s $Serial install $tpk 2>&1) -join "`n"
    Write-Host $out
    # sdb install peut sortir en 0 même si l'installeur refuse le paquet.
    if ($out -match 'fail|error|denied|invalid') { throw "Installation refusée par la TV (voir ci-dessus)" }
}

function Run-App {
    Connect-Tv
    Invoke-Checked $Tizen @('run', '-p', $AppId, '-s', $Serial)
}

switch ($Action) {
    'test'    { Invoke-Checked $Dotnet @('test', (Join-Path $Native 'tests\QuietHome.Core.Tests'), '--nologo') }
    'build'   { Build-Tpk }
    'sign'    { Sign-Tpk }
    'deploy'  { Build-Tpk; Sign-Tpk; Install-Tpk; Run-App }
    'install' { Install-Tpk }
    'run'     { Run-App }
    'logs'    {
        # Écoute locale ; autorisez UDP $LogPort entrant sur le profil réseau Privé si rien n'arrive.
        $udp = New-Object System.Net.Sockets.UdpClient($LogPort)
        Write-Host "En écoute UDP :$LogPort (Ctrl+C pour arrêter)" -ForegroundColor Green
        try {
            $ep = New-Object System.Net.IPEndPoint([System.Net.IPAddress]::Any, 0)
            while ($true) { [Text.Encoding]::UTF8.GetString($udp.Receive([ref]$ep)) }
        } finally { $udp.Close() }
    }
    'uninstall' {
        Connect-Tv
        Invoke-Checked $Tizen @('uninstall', '-p', $PkgId, '-s', $Serial)
    }
}
