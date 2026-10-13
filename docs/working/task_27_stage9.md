# Task 27 Stage 9: Watch 휠 라우팅 확정
Changes:
- `MainWindow.xaml.cs`: 각 ListView에 `PointerWheelChanged` 핸들러
  (`OnWatchWheel`) — 호버된 리스트의 자체 ScrollViewer를 `ChangeView`로
  스크롤하고 `Handled` 처리. 네이티브 스크롤이 먼저 동작하면 핸들러는
  스킵됨 (이중 스크롤 없음).
Verification:
- 기하 실측: Entries y210-321 / Results y393-591 (겹침 없음),
  각 viewer viewport < extent (내부 스크롤 가능) — 레이아웃 정상 확정.
- `dotnet build` 경고 0·오류 0, `dotnet test` 207/207.
- 휠 실동작은 헤드리스 검증 불가 (합성 입력이 앱에 도달하지 않음 —
  SendInput 성공 반환에도 무반응, 원인 미상).
  임시 시드·진단·IsSelected 플래그는 전부 원복됨 (diff 확인).
Deviations: 없음.
Notes:
- 실행 중인 구 빌드에서 본 증상일 가능: 새 빌드에서 Border 섹션이 보이는지
  먼저 확인 필요. 미해결 시 다음 단계 (스크롤버튼 등 대체 UX).
Approval: 전체 stage 승인으로 갈음 (2026-10-13).
