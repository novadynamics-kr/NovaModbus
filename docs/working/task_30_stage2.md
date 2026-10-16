# Task 30 Stage 2: WinForms Client 스캐폴드
Changes:
- `src/NovaModbus.Client/NovaModbus.Client.csproj` (신규): `net10.0-windows` +
  `UseWindowsForms`, Rtu 참조. 새 NuGet 없음 (WinForms 프레임워크 내장).
- `Program.cs` (STAThread + `ApplicationConfiguration`), `MainForm.cs` +
  `MainForm.Designer.cs` (수기 InitializeComponent: 좌측 설정 패널 + TabControl
  4탭 + 상태바).
- `NovaModbus.slnx` 등록, `.gitignore` bin/obj (빌드 전 선행).
Verification: `dotnet build` (프로젝트·솔루션) 경고 0·오류 0,
  `dotnet test` 176/176 회귀 없음.
  실행 확인은 지시자 몫 (`dotnet run --project src/NovaModbus.Client`).
Deviations from plan: none.
Notes:
- Designer 파일은 `#nullable disable` 관례 (CS8669 회피).
- 산출물 혼입 3회 정정: (1) 삭제된 Task-27 잔재 디렉터리가 무시에 없어 추적됨,
  (2) Stage 1에서 ClientWPF.Tests/obj 일부가 이미 커밋됨 (추적 후 무시 불가),
  (3) VS Code 디자인타임 빌드가 삭제 경로를 수 초 내 재생성하여 `add -A`와
  경합. 대응: `rm --cached` 후 `add` 없이 amend, 사멸 경로 방패 ignore 4줄,
  이후 커밋은 경로 지정 스테이징.
Approval: [ ] director approved (date)
