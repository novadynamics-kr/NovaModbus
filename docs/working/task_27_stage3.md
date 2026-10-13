# Task 27 Stage 3: 설정·Read/Write 탭 이식 (Toolkit)
Changes:
- `CommunityToolkit.Mvvm` 추가 (MS 관리, 승인済).
- `ViewModels/SerialPortSettingsViewModel.cs` (신규): WPF 동등 로직을 Toolkit
  partial properties + `RelayCommand`로 이식.
- `ViewModels/ReadWriteViewModel.cs` (신규): 18종 FC 파싱·실행 로직 그대로,
  `AsyncRelayCommand` + UI 어피니티 await (WinUI 바인딩은 UI 스레드 필수 —
  `ConfigureAwait(false)` 제거). `IsBusy` ↔ CanExecute 연동.
- `ViewModels/MainViewModel.cs` (신규): 연결 생명주기 + 내장 가상 슬레이브.
  `ConnectionChanged`는 DispatcherQueue 마샬 (Rtu가 워커 스레드에서 발생).
  Watch/Polling은 Stage 4에서 추가.
- `Services/ValueFormatter.cs` (복사), `Services/ObservableLogger.cs`
  (Dispatcher → DispatcherQueue).
- `MainWindow.xaml`: 좌측 설정 + `TabView` (Read/Write 실구현, 나머지 placeholder).
  WinUI 차이 반영: `Window` 무 `DataContext` (루트 Grid에 설정),
  `StackPanel` 무 `IsEnabled` (입력 컨트롤로 이동), `GroupBox`/`Label` → TextBlock.
Verification: `dotnet build` (프로젝트·솔루션) 경고 0·오류 0, `dotnet test` 176/176.
  실행 확인은 지시자 몫 (`dotnet run --project src/NovaModbus.Client`).
Deviations from plan: MVVM을 Toolkit으로 확정 (전체 stage 승인 시 결정).
Notes:
- MVVMTK0045: WinUI에서는 `[ObservableProperty]` 필드 금지 → partial properties 사용
  (Task 29 AOT에도 유리). Slave 이식(#28) 동일 규칙 적용.
Approval: 전체 stage 승인으로 갈음 (2026-10-13).
