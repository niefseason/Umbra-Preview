param([Parameter(Mandatory)][string]$Signtool,[Parameter(Mandatory)][string]$CertificateThumbprint,[string]$TimestampUrl='http://timestamp.digicert.com')
$ErrorActionPreference='Stop'; $root=Split-Path $PSScriptRoot -Parent; $installer=Join-Path (Split-Path $root -Parent) 'Umbra-0.1.0-Windows-Setup.exe'
if(-not (Test-Path -LiteralPath $installer)){throw 'Build the installer before signing.'}
& $Signtool sign /sha1 $CertificateThumbprint /fd SHA256 /tr $TimestampUrl /td SHA256 $installer
if($LASTEXITCODE -ne 0){throw 'Authenticode signing failed.'}
& $Signtool verify /pa /all $installer
if($LASTEXITCODE -ne 0){throw 'Authenticode verification failed.'}
Write-Output "Signed and verified $installer"
