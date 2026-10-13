# Task 27 Stage 15: 영역 인식 휠 라우팅
Changes:
- `MainWindow.xaml`: Watch 외부 Grid에 `x:Name="WatchGrid"`,
  양 ListView에 `ScrollViewer.VerticalScrollMode="Disabled"`
  (네이티브 휠 스크롤 원천 차단).
- `MainWindow.xaml.cs`: 요소별 핸들러 제거 → `WatchGrid.PointerWheelChanged`
  단일 핸들러 (`OnWatchGridWheel`). 포인터 위치를 양 리스트의 실시간
  경계(`TransformToVisual` + `ActualWidth/Height`)와 대조해 담당 리스트를
  판정, 해당 스크롤러만 즉시 스크롤. 고정 좌표 없음, 창 크기 무관.
  리스트 밖(헤더·여백)은 그대로 버블 (기존 동작 유지).
Verification: `dotnet build` 경고 0·오류 0, `dotnet test` 207/207.
  휠 실동작은 헤드리스 검증 불가 — 사용자 재확인 필요.
Deviations: 키보드 스크롤·터치 팬은 비활성 (방향키 선택 이동은 유지).
  네이티브 오배송 원인은 프레임워크 레벨로 미확정.
Approval: 전체 stage 승인으로 갈음 (2026-10-14).
