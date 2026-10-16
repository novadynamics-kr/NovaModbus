# Task 30 Stage 7: Watch VirtualMode + 대량 삭제 프리징 수정
Changes:
- `MainForm.Designer.cs` `SetupWatchGrid`: `VirtualMode=true` + `ReadOnly` +
  `AutoSizeColumnsMode/None` + `AutoSizeRowsMode/None` + `RowHeadersVisible=false` +
  네이티브 `FullRowSelect`/`MultiSelect`. 체크박스 컬럼 삭제 (지시자 금지 조항).
  `SuspendLayout`/`ResumeLayout` 적용.
- `MainForm.cs`: 바인딩(`DataSource`) 제거 → `CellValueNeeded` 2종 +
  `SyncRowCounts` (O(1) `RowCount` 푸시). 셀 값은 문자열로만 반환
  (`FormatException` 방지). `FileDialog` 저장/불러오기에 선택 억제 적용.
- `MirrorSelection` 최적화: 빈 선택→`ClearSelection` 단일 호출, 전체 선택→
  `SelectAll` 단일 호출, 2000행 초과 부분 선택은 라이브 미러 생략
  (삭제는 양 그리드 합집합으로 수집하므로 정합성 유지).
- `OnWatchDelete` 재작성: 삭제 전 과정에서 `_syncingGrids` 억제 +
  양 그리드 `SuspendLayout`. 전체 선택 시 열거 없이 `ClearAll` 직행.
  부분 삭제는 인덱스 합집합 → `ClearSelection` 선행 (선택 캐스케이드 차단) →
  `RemoveAtIndices` → `RowCount` 축소.
- `OnWatchClearAll`/`OnWatchLoad` 동일 억제 패턴. `OnWatchSelectAll`/
  `DeselectAll`은 VM `IsSelected`를 건드리지 않음 (체크박스 방식 금지) —
  그리드 네이티브 선택 + 상태 라벨만 갱신.
- `WatchList.RemoveAtIndices` (신규, O(n) 단일 패스): 전체 삭제는 `ClearAll`
  직행. `RaiseListChangedEvents` 억제 하에 survivors 재구성.
Verification: `dotnet build` 경고 0·오류 0, `dotnet test` 209/209
  (159+14+33+3, 신규 `RemoveAtIndices` 3종 포함).
  실측 확인은 지시자 몫 (65k Add Range → 전체 선택 → 삭제).
Deviations: 없음 (지시된 VirtualMode·선택 방식 그대로).
Notes:
- `BindingList`가 `INPC` 항목을 구독하므로 survivors 재구성이
  `RemoveAt` 반복보다 유리 (O(n) 고정).
Approval: 전체 승인으로 갈음 (2026-10-16).
