# Task 30 Stage 4: Polling·Watch·Log 탭 이식
Changes:
- `ViewModels/WatchArea.cs`, `WatchEntry.cs`, `WatchResult.cs`,
  `Services/WatchListStore.cs` (WPF와 동일 의미로 복사, 수동 INPC 유지).
- `ViewModels/PollingRunner.cs` (신규): `PeriodicTimer` 루프 + `BindingList` 이력.
  UI 어피니티 유지 (WinForms 바인딩은 UI 스레드 필수).
- `ViewModels/WatchList.cs` (신규): `BindingList` 기반 등록·결과,
  Add/Remove/Clear/SelectAll/DeselectAll/ReadAll/AddRange/SaveLoad/Auto-refresh.
  `StatusChanged` 이벤트, `Progress{T}` 진행률, 벌크 교체 헬퍼.
- `MainForm(.cs)`: WatchList/PollingRunner 합류 + Dispose 확장,
  그리드 키 (Delete/Ctrl+A/Escape), 체크박스 즉시 커밋,
  결과 그리드 `CellFormatting` (중첩 `Entry.*` 바인딩 불가 대응),
  WinForms FileDialog 저장/불러오기, 로그 바인딩.
- `MainForm.Designer.cs`: Polling/Log 탭, Watch `SplitContainer` + `DataGridView` 2개
  (체크박스 컬럼 + 네이티브 FullRowSelect, 드래그 선택 미이식).
Verification: `dotnet build` 경고 0·오류 0, `dotnet test` 176/176.
  실행 확인은 지시자 몫.
Deviations: 없음.
Notes:
- `#nullable disable` 파일에서는 `?` 어노테이션도 금지 (CS8669) — oblivious 타입 사용.
- `ListView`가 아닌 `DataGridView`라 WinUI 때의 휠·선택 문제 없음.
Approval: 전체 승인으로 갈음 (2026-10-16).
