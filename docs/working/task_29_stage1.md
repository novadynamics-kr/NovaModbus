# Task 29 Stage 1: Rtu trim/AOT-safe 감사 (Replan 적용)
Changes:
- `src/NovaModbus.Rtu/NovaModbus.Rtu.csproj`: `<IsTrimmable>true</IsTrimmable>` 추가
  (라이브러리 트림 주석 출하 + 분석 게이트 상시화).
- `docs/plans/task_29.md`: Replan 기록 (WinUI Phases 2·3 void).
Verification:
- 위험 API 전수 grep: `Reflection`/`Serializer`/`Regex`/`Emit`/`Assembly.Load` 등
  0건. 유일 적중은 델리게이트·이벤트 `Invoke` 호출 (AOT 안전).
- `dotnet build` (Rtu 단독·솔루션) 경고 0·오류 0 — 트림 분석(ILxxxx) clean.
- `dotnet test` 233/233 회귀 없음.
Deviations: Replan (필수, #27 FAILED·#28 CANCELLED의 귀결 — 신규 판단 아님).
Notes:
- WinForms 앱 2종의 `System.Text.Json` 리플렉션 직렬화
  (`WatchListStore`·`RegisterMapStore`)도 앱 제외 사유에 추가 기록
  (프레임워크 미지원이 1차 사유).
- `IsTrimmable`은 참조 앱에 트리밍을 강제하지 않음 (주석+분석のみ).
Approval: 자동 승인으로 갈음 (2026-10-16, "#29 자동 승인 진행").
