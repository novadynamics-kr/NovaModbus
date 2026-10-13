# Task 27 Stage 4: Polling·Watch·Log 탭 이식
Changes:
- `ViewModels/WatchArea.cs`, `WatchEntry.cs`, `WatchResult.cs` (신규): WPF와 동일
  의미로 복사 (수동 INPC 유지 — WinUI 흐름이 UI 어피니티라 마샬 불필요).
  `WatchResult.SuppressSelectionNotification`은 WPF처럼 Entry에 위임.
- `Services/WatchListStore.cs` (복사, 무변경).
- `ViewModels/PollingViewModel.cs` (신규): Toolkit port. 루프·History UI-affine 유지.
- `ViewModels/WatchViewModel.cs` (신규): Toolkit port. 변경점:
  WPF `ICollectionView.DeferRefresh` 제거 (WinUI 미지원 — bulk 연산 자체가 단일 알림),
  `BulkProgress`는 `Progress{T}`로 마샬 (워커 스레드 직접 쓰기 금지),
  Save/Load 대화상자는 코드비하인드 + `SaveWatchTo`/`LoadWatchFrom(path)` 분리 (테스트 용이).
- `ViewModels/MainViewModel.cs`: Watch/Polling 합류 + Dispose 확장.
- `MainWindow.xaml(.cs)`: Polling/Log 실탭, Watch는 ListView 2개 (WinUI 무 DataGrid) +
  `KeyboardAccelerator` (Ctrl+A/Delete/Escape) + FilePicker 저장/불러오기
  (WinRT `InitializeWithWindow` HWND 연동).
Verification: `dotnet build` (프로젝트·솔루션) 경고 0·오류 0, `dotnet test` 176/176.
  실행 확인은 지시자 몫.
Deviations: 드래그 다중선택 미이식 (WinUI ListView에 DataGridRubberBand 없음 —
  체크박스+버튼+단축키로 동등 조작 가능). DataGrid 미사용 (외부 패키지 추가 회피).
Approval: 전체 stage 승인으로 갈음 (2026-10-13).
