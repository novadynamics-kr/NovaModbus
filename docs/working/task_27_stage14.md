# Task 27 Stage 14: 조상 Background 지정 (히트테스트 확정)
Changes:
- `MainWindow.xaml`: EntriesList·ResultsList의 상위 노드 전부 명시적 브러시.
  Root/Watch 외부 Grid — `ApplicationPageBackgroundThemeBrush`,
  내부 Grid 2개 + 양 ListView — `LayerFillColorDefaultBrush`
  (Border 배경과 동일, 시각 변화 없음).
  배경 누락(null) 시 히트테스트 통과로 하위 영역 이벤트 오인식 방지.
- 임시 코드 전부 원복 (65k 시드·기하 덤프·IsSelected 플래그, diff 확인).
Verification: `dotnet build` 경고 0·오류 0, `dotnet test` 207/207.
  휠 실동작은 헤드리스 검증 불가 — 사용자 재확인 필요.
Deviations: 없음.
Approval: 전체 stage 승인으로 갈음 (2026-10-13).
