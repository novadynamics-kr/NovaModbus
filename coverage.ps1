# Coverage: test with collectors, render an HTML report for VS Code.
# - "XPlat Code Coverage" (coverlet): Cobertura with line+branch -> HTML via ReportGenerator.
# - "Code Coverage" (Microsoft): .coverage for Visual Studio (see CodeCoverage.runsettings).
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $root

dotnet tool restore
dotnet test NovaModbus.slnx -c Release --settings CodeCoverage.runsettings --collect:"XPlat Code Coverage" --collect:"Code Coverage" --nologo -v q

$cov = Get-ChildItem tests/NovaModbus.Rtu.Tests/TestResults -Recurse -Filter coverage.cobertura.xml |
    Sort-Object LastWriteTime |
    Select-Object -Last 1
if ($null -eq $cov) {
    throw "No Cobertura file produced."
}

if (Test-Path coverage-report) {
    Remove-Item coverage-report -Recurse -Force
}
dotnet tool run reportgenerator -reports:$($cov.FullName) -targetdir:coverage-report -reporttypes:Html

Write-Output ("HTML report: " + (Join-Path $root 'coverage-report/index.html'))
