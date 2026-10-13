# Task 27 Stage 13: 요소 직접 이벤트 연결
Changes:
- `MainWindow.xaml.cs`: `AddHandler(handledEventsToo)` → 요소 직접 연결
  (`EntriesList.PointerWheelChanged += OnWatchWheel` 외 1).
  핸들러 로직 동일 (해당 리스트 자체 스크롤 + 즉시 + Handled).
Verification: `dotnet build` 경고 0·오류 0, `dotnet test` 207/207.
Deviations: XAML 속성 연결은 미사용 — `x:Name`+이벤트 동시 사용 시 XAML
  컴파일러가 오기입 (`+= Handler};`, Stage 6 기록)하므로 코드비하인드
  `+=`로 동일 연결.
Approval: 전체 stage 승인으로 갈음 (2026-10-13).
