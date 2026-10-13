# Task 27 Stage 12: 리스트별 휠 확정 (최종)
Changes:
- `MainWindow.xaml.cs` `OnWatchWheel` 확정판:
  `AddHandler(handledEventsToo: true)`로 등록 — 네이티브가 소비한 경우 스킵,
  미처리인 경우 해당 리스트만 즉시 스크롤 (`ChangeView` 애니메이션 OFF로
  연타 시 stale offset 경합 제거). 창 크기와 무관 (좌표 미사용).
- 임시 코드 전부 원복 (시드·플래그·진단, diff 확인).
Verification: `dotnet build` 경고 0·오류 0, `dotnet test` 207/207.
  휠 실동작은 헤드리스 검증 불가.
Deviations: 없음.
Notes:
- Entries→Results 오동작은 코드 경로상 불가능 (핸들러는 sender 자신의
  viewer만 조작). 구 빌드 실행 또는 프레임워크 레벨이면 차기 단계에서
  대체 UX (스크롤 버튼) 검토.
Approval: 전체 stage 승인으로 갈음 (2026-10-13).
