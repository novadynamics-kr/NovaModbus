# Task 31 Stage 2: WinForms Slave 스캐폴드
Changes:
- `src/NovaModbus.Slave/NovaModbus.Slave.csproj` (신규): `net10.0-windows` +
  `UseWindowsForms`, Rtu 참조. 새 NuGet 없음 (WinForms 프레임워크 내장).
- `Program.cs` (STAThread + `ApplicationConfiguration`), `MainForm.cs` +
  `MainForm.Designer.cs` (수기 InitializeComponent: 좌측 설정 패널 + TabControl
  5탭 Holding/Coils/Counters/Log/Float + 상태바).
- `NovaModbus.slnx` 등록, `.gitignore` bin/obj (빌드 전 선행).
Verification: `dotnet build` (프로젝트·솔루션) 경고 0·오류 0,
  `dotnet test` 209/209 회귀 없음.
  실행 확인은 지시자 몫 (`dotnet run --project src/NovaModbus.Slave`).
Deviations from plan: none.
Notes:
- Designer 파일은 `#nullable disable` 관례 (CS8669 회피, Task 30 교훈).
Approval: [ ] director approved (date)
