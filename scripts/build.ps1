param([string]$Dotnet='dotnet',[string]$InnoCompiler='')
$ErrorActionPreference='Stop'
$projectRoot=Split-Path $PSScriptRoot -Parent
function Run-Dotnet([string[]]$Arguments) { & $Dotnet @Arguments; if($LASTEXITCODE -ne 0) { throw 'Build or test failed.' } }
Run-Dotnet -Arguments @('build',"$projectRoot\tests\Umbra.TestBackend",'-c','Release')
Run-Dotnet -Arguments @('build',"$projectRoot\tests\Umbra.Tests",'-c','Release')
Run-Dotnet -Arguments @("$projectRoot\tests\Umbra.Tests\bin\Release\net8.0\Umbra.Tests.dll","$projectRoot\tests\Umbra.TestBackend\bin\Release\net8.0\Umbra.TestBackend.exe")
Run-Dotnet -Arguments @('publish',"$projectRoot\src\Umbra.Desktop",'-c','Release','-r','win-x64','--self-contained','true','-o',"$projectRoot\artifacts\app")
if($InnoCompiler) { & $InnoCompiler "$PSScriptRoot\Umbra.iss"; if($LASTEXITCODE -ne 0) { throw 'Installer compilation failed.' } }
