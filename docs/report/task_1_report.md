# Task 1 Report: Transport 수신 루프 + Client/Slave 본구현

Issue: #1

## Plan vs Result

| Step | Plan | Result |
|------|------|--------|
| 1. RtuFramer 수신 루프 | 무음 구분 + Fake + 테스트 | 완료 (6 tests) |
| 2. 트랜잭션 코어 | Exchange + 검증 + 재시도 | 완료 (6 tests) |
| 3. Client 본구현 | FC 8종 + echo 검증 | 완료 (10 tests) |
| 4. Slave 본구현 | 디스패치 + 예외 + 멀티 ID | 완료 (13 tests) |
| 5. 루프백 + 게이트 | 통합 2종 + 전 관문 | 완료 (67/67) |

계획과 결과의 차이: Stage 2에서 테스트 오케스트레이션 방식 수정 1건
(순차 enqueue → Task 선행 + 지연 enqueue). 그 외 계획대로.

## Totals

- 신규 소스 4개 (ModbusTransaction, Client 재작성, Slave 재작성, RtuFramer 확장),
  테스트 37개 추가 (30 → 67), 커밋 5개 (stage별).

## Unresolved / Follow-ups

1. 실물 `SerialPort.BaseStream.ReadAsync` 취소 토큰 지원 여부 — 하드웨어
   검증 시 확인 (`TimeoutException` 경로로 1차 방어済).
2. FC 08/11/17/22/23 (2차), 브로드캐스트 쓰기 미지원.
3. Client/Slave WPF 기능 탭 (후속 Task).
4. 토큰 권한 부족으로 #1 self-assign 미완 — close 전 웹에서 처리 또는 무시.

## Merge readiness

- Branch: `task/1-transport-loop` (local-only) → `main` merge 대기.
- Gate: build 무경고 + test 67/67 확인됨.
- [ ] director merge approval → `git push nova main` → issue #1 close.
