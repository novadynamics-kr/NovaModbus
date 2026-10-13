# Task 26 Stage 1: 테스트 프로젝트 2개 생성
Changes:
- `tests/NovaModbus.Client.Tests/NovaModbus.Client.Tests.csproj` (신규): `net10.0-windows` MSTest, Client+Rtu 참조. NuGet은 Rtu.Tests와 동일 버전 고정 (`MSTest` 4.5.1, `coverlet.collector` 10.1.0, `Microsoft.CodeCoverage` 18.10.1).
- `tests/NovaModbus.Slave.Tests/NovaModbus.Slave.Tests.csproj` (신규): 동종, Slave+Rtu 참조.
- `NovaModbus.slnx`: tests 폴더에 2개 프로젝트 등록.
Verification: `dotnet build NovaModbus.slnx -c Release` — 경고 0·오류 0.
Deviations from plan: none.
Approval: [ ] director approved (date)
