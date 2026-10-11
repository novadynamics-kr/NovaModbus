# Task 1 Stage 2: 트랜잭션 코어 + Fake

Issue: #1

## Changes

- `src/NovaModbus.Rtu/ModbusTransaction.cs` (신규) — `ExchangeAsync`:
  요청 전송 → 응답 대기 → SlaveId/Function/CRC 검증 → PDU 반환.
  쓰레기 바이트·타 slave·예상外 function은 무시 후 대기 지속,
  Exception 프레임은 `ModbusSlaveException`, 소진 시 `TimeoutException`.
- `tests/.../TransactionTests.cs` (신규, 6개) — 정상 왕복, 쓰레기 무시,
  타 slave/function 무시, Exception, 재시도 후 타임아웃(쓰기 2회 확인),
  slaveId 범위 거부.

## Verification

- build 경고 0, test 42/42.

## Deviations from plan

- 테스트 2건 실패 후 수정: 순차 enqueue는 framer에 한 프레임으로 합쳐지므로
  (무음 없음) exchange를 Task로 먼저 띄우고 지연 enqueue로 변경. 원인·수정
  `TransactionTests` 주석 대신 본 보고서에 기록.
- `TryDecode` out var nullability (CS8602) — `frame is null` 가드 추가.

## Approval

- [x] director approved (auto — task 1 blanket approval 2026-10-11)
