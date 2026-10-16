# Task 31 Stage 3: 리스너·ID·편집 그리드 이식
Changes:
- `Services/WinFormsLogger.cs` (신규, Client 교훈 복사: `Control.Invoke` 마샬 +
  `BindingList` 바인딩), `Services/RegisterMapStore.cs` (WPF와 동일, Stage 4용 선행).
- `ViewModels/ViewModelBase.cs` (신규, 최소 INPC — WPF 의존성 제거).
- `ViewModels/SlavePortOptions.cs` (신규): 시리얼 전용 설정
  (`RefreshPorts`/`BuildSettings`, 가상 포트 없음 — WPF와 동일).
- `ViewModels/RegisterRow.cs`/`CoilRow.cs` (WPF와 동일 로직,
  `IsSelected` 유지 — 지시자 결정. UI는 네이티브 선택 사용).
  `CoilRow.AddressHex` 추가 (Holding 그리드와 일관).
- `SlaveStore.cs` (신규, `SlaveStore.cs` 루트): ID별 stores/maps, 정렬 ID 목록,
  `List<T>` 행 목록 (VirtualMode는 바인딩 불필요), 단건 추가, O(n) 인덱스 삭제,
  범위 추가·저장/불러오기·float·카운터, `StatusChanged` 이벤트.
  `Func<ModbusRtuSlave?>` provider로 카운터·엔진 연동.
- `MainForm(.cs)`: Start/Stop 리스너 (`TransportFactory` + `ModbusRtuSlave` +
  `SlaveServer`, 에러 주입 라이브 적용), ID 추가·삭제·선택, Holding/Coils
  VirtualMode 편집 그리드 (`CellValueNeeded` 문자열 반환 +
  `CellValuePushed`: Holding 값 파싱·커밋, Coil 체크 즉시 커밋),
  네이티브 다중선택 + `GetRowCount(Selected)` 고속 경로 + 삭제 전
  `ClearSelection` 선행. 체크박스 선택 컬럼 없음.
- `MainForm.Designer.cs`: 설정 패널 (콤보 5종 + Start/Stop + ID 목록 + 에러 주입) +
  Holding/Coils 탭 (툴바 + 진행률 + 가상 그리드). Counters/Log/Float는 빈 페이지.
Verification: `dotnet build` (프로젝트·솔루션) 경고 0·오류 0.
Deviations: `CoilRow.AddressHex` 추가 (WPF에 없음, 그리드 일관성).
Notes:
- `List<T>` + VirtualMode라 `BindingList`·`ResetBindings` 불필요 (Client 대비 단순).
- 두 그리드는 독립이라 Client식 미러링 불필요 — 삭제 억제 로직만 적용.
Approval: 전체 승인으로 갈음 (2026-10-16).
