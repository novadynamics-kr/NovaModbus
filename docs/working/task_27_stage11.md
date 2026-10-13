# Task 27 Stage 11: 65k행 선택 O(n²) 해소
Changes:
- `MainWindow.xaml.cs`: 선택 동기화를 전수 스캔에서 전환.
  - 단건 변경: 해당 항목만 양 리스트에 반영 (`SyncOne`, O(1)).
    Results 대응 행은 `ReferenceEquals` 선형 탐색 1회 (O(n), 수 ms).
  - 전체선택/해제: `SelectAll()` / `SelectedItems.Clear()` 네이티브 일괄
    (O(n)). 방향은 `Entries[0].IsSelected`로 판정
    (`SelectAllItems`는 항상 균일 설정). `DeselectAll()`은 WinUI
    `ListView`에 없음 → `SelectedItems.Clear()` 사용.
  - `SyncListsFromVm`/`SyncList` 전수 스캔 삭제.
- 배경: 사용자 실측 65,536행. 기존 코드는 체크박스 1회 토글마다
  65k×2 전수 × `SelectedItems.Contains` O(n) = 수십억 연산으로 UI 스레드
  장시간 블록. 대량 행에서 입력 큐 적체·오동작의 유력 원인.
Verification: `dotnet build` 경고 0·오류 0, `dotnet test` 207/207.
  (휠 실동작은 헤드리스 검증 불가 — 사용자 재확인 필요.)
Deviations: 없음.
Approval: 전체 stage 승인으로 갈음 (2026-10-13).
