# Task 27 Stage 6: Watch 마우스 다중 선택 (WinUI Client)
Changes:
- `MainWindow.xaml`: Entries/Results ListView `SelectionMode="None"` → `"Multiple"`.
  (WinUI 추가 패키지 없이 네이티브 제스처: 클릭·Ctrl+클릭·Shift+범위·드래그·Ctrl+A.)
- `MainWindow.xaml.cs`: 양방향 동기화 (재진입 가드):
  ListView `SelectionChanged` → `IsSelected` 반영,
  항목 `IsSelected` 변경 → 양 리스트 선택 반영,
  VM `SelectionBulkChanged` (신규 이벤트, 전체선택/해제 시) → 전체 동기화.
  기존 Del/단축키/버튼 경로는 `IsSelected` 기반 그대로 동작.
- `ViewModels/WatchViewModel.cs`: `SelectionBulkChanged` 이벤트 추가
  (bulk 구간 알림 억제 보완).
- `tests/.../WatchViewModelTests.cs`: bulk 이벤트 테스트 1개 추가.
Verification: `dotnet build` 경고 0·오류 0, `dotnet test` 206/206.
  제스처 실동작 확인은 지시자 몫 (헤드리스 검증 불가).
Deviations: WPF `DataGridDragSelect` 상당의 드래그-마퀴는 프레임워크 기본 동작에 위임.
Notes:
- XAML 컴파일러 특이: `x:Name` + XAML 이벤트 연결을 같은 요소에 두면
  생성 코드에 `+= Handler};` 오기입 (CS1002/CS1003) → 이벤트는 코드비하인드에서 연결.
- 셸 전송 계층이 `}"` 시퀀스를 손상 → XAML 편집은 라인 단위 처리로 우회.
Approval: 전체 stage 승인으로 갈음 (2026-10-13).
