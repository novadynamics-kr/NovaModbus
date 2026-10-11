# Task 1 Stage 3: ModbusRtuClient 본구현

Issue: #1

## Changes

- `src/NovaModbus.Rtu/ModbusRtuClient.cs` (재작성) — FC 8종 메서드,
  `ModbusTransaction` 위임, echo 검증(05/06/15/16), `BaudRate`/`Timeout`/
  `Retries` 속성. 기존 `ReadHoldingRegistersAsync` 시그니처 유지.
- `tests/.../Testing/FakeSerialTransport.cs` — `DisposeAsync` 멱등화
  (client+transport 이중 dispose 허용).
- `tests/.../ClientTests.cs` (신규, 10개) — FC별 스크립트 응답 round-trip +
  송신 와이어 검증, echo 불일치·타임아웃 예외.

## Verification

- build 경고 0, test 52/52.

## Deviations

- 없음.

## Approval

- [x] director approved (auto — task 1 blanket approval 2026-10-11)
