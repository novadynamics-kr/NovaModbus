# Task 3 Report: Client 기능 UI + 가상 루프백(안 A)

Issue: #3

## Plan vs Result

| Step | Plan | Result |
|------|------|--------|
| 1. Virtual bus + 팩토리 | 허브·선택 추상화·테스트 | 완료 (4 tests) |
| 2. 연결 관리 | Serial/Virtual·내장 slave·상태 | 완료 (4 tests) |
| 3. FC 탭 | 8종 실행 UI | 완료 (빌드, 수동 확인 필요) |
| 4. 폴링 + 로그 | 타이머·Hex·히스토리 | 완료 (빌드, 수동 확인 필요) |
| 5. 토글 + 게이트 | 토글·관문·보고 | 완료 (75/75) |

계획대로. Client ViewModel은 자동 테스트 없음 (net10.0-windows 소속) —
director 실행 확인 항목으로 남김.

## Totals

- 신규: VirtualTransport/Bus/Factory, ModbusConnection, SlaveServer,
  AsyncRelayCommand, ObservableLogger, ReadWrite/Polling VM, 탭 3종.
- 테스트 75 (71 + Connection 4), 커밋 5개 (stage별).

## Unresolved / Follow-ups

1. Client 실행 후 눈 확인: 가상 연결 → Execute → Polling Start → Log.
2. Slave.exe UI (후속 Task), 실물 COM 검증 (하드웨어 단계).

## Merge readiness

- Branch: `task/3-client-ui` (local-only) → `main` merge 대기.
- Gate: build 무경고 + test 75/75 확인됨.
- [ ] director merge approval → `git push nova main` → issue #3 close.
