# Coverage: test with collectors, render an HTML report for VS Code.
# - "XPlat Code Coverage" (coverlet): Cobertura with line+branch -> HTML via ReportGenerator.
# - "Code Coverage" (Microsoft): .coverage for Visual Studio (see CodeCoverage.runsettings).
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $root

dotnet tool restore
dotnet test NovaModbus.slnx -c Release --settings CodeCoverage.runsettings --collect:"XPlat Code Coverage" --collect:"Code Coverage" --nologo -v q

# Latest Cobertura per test project (ReportGenerator merges them).
$reports = @(
    'tests/NovaModbus.Rtu.Tests',
    'tests/NovaModbus.Client.Wpf.Tests',
    'tests/NovaModbus.Client.Tests',
    'tests/NovaModbus.Slave.Tests'
) | ForEach-Object {
    Get-ChildItem "$_/TestResults" -Recurse -Filter coverage.cobertura.xml -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime |
        Select-Object -Last 1
}
$reports = @($reports | Where-Object { $_ -ne $null })
if ($reports.Count -eq 0) {
    throw "No Cobertura file produced."
}

if (Test-Path coverage-report) {
    Remove-Item coverage-report -Recurse -Force
}
$inputs = ($reports | ForEach-Object { $_.FullName }) -join ';'
dotnet tool run reportgenerator -reports:$inputs -targetdir:coverage-report -reporttypes:Html

Write-Output ("HTML report: " + (Join-Path $root 'coverage-report/index.html'))
