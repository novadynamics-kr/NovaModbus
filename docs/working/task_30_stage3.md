# Task 30 Stage 3: 설정·Read/Write 탭 이식
Changes:
- `Services/ValueFormatter.cs` (복사, 무변경), `Services/WinFormsLogger.cs` (신규:
  `IModbusLogger` + `Control.Invoke` 마샬 + `BindingList` 바인딩).
- `ViewModels/SerialPortOptions.cs` (신규): WPF 설정 로직을 플레인 클래스로
  이식 (INPC/커맨드 제거, `RefreshPorts`/`BuildSettings` 동일).
- `ViewModels/ReadWriteRunner.cs` (신규): 18종 FC 파싱·실행 로직 그대로 이식.
  `ConfigureAwait(false)` 유지 (UI를 직접 만지지 않음).
- `MainForm.cs`: 연결 생명주기 + 내장 가상 슬레이브 (WPF MainViewModel 대응).
  `ConnectionChanged`는 `Invoke` 마샬, 폼 종료 시 해제.
- `MainForm.Designer.cs`: 설정 패널 (콤보 5종 + 버튼 3종 + 상태) +
  Read/Write 탭 (기능 콤보·입출력 박스·힌트·결과창).
Verification: `dotnet build` (프로젝트·솔루션) 경고 0·오류 0,
  `dotnet test` 176/176 회귀 없음.
  실행 확인은 지시자 몫 (`dotnet run --project src/NovaModbus.Client`).
Deviations from plan: none.
Notes:
- Toolkit 미사용 (WinForms는 Click 이벤트로 충분, 의존성 최소).
- `Labeled` 헬퍼는 Designer에 상주 (UI 구성용).
Approval: 전체 승인으로 갈음 (2026-10-16).
