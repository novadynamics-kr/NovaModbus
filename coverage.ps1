# Coverage: test with Code Coverage collection, convert to Cobertura,
# render an HTML report for VS Code. Product assemblies only (see CodeCoverage.runsettings).
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $root

dotnet tool restore
dotnet test NovaModbus.slnx -c Release --settings CodeCoverage.runsettings --collect:"Code Coverage" --nologo -v q

$cov = Get-ChildItem tests/NovaModbus.Rtu.Tests/TestResults -Recurse -Filter *.coverage |
    Sort-Object LastWriteTime |
    Select-Object -Last 1
if ($null -eq $cov) {
    throw "No .coverage file produced."
}

if (Test-Path coverage-report) {
    Remove-Item coverage-report -Recurse -Force
}
dotnet tool run dotnet-coverage merge -o coverage-report/cobertura.xml -f cobertura $cov.FullName
dotnet tool run reportgenerator -reports:coverage-report/cobertura.xml -targetdir:coverage-report -reporttypes:Html

Write-Output ("HTML report: " + (Join-Path $root 'coverage-report/index.html'))
