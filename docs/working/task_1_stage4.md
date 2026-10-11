# Task 1 Stage 4: ModbusRtuSlave 디스패치

Issue: #1

## Changes

- `src/NovaModbus.Rtu/ModbusRtuSlave.cs` (재작성) — 순수 `HandleRequest`
  (CRC 불량·미보유 ID·브로드캐스트 무응답), FC 8종 디스패치,
  범위 초과→IllegalDataAddress·값 오류→IllegalDataValue·미지원→IllegalFunction,
  `SimulateBusy` 에러 주입, `IgnoreCrcErrors=false` 시 `CrcMismatchException`,
  I/O용 `ServeOnceAsync` + `BaudRate`/`Timeout`.
- `tests/.../SlaveDispatchTests.cs` (신규, 13개) — 읽기/쓰기 왕복 5종,
  예외 4종, 무응답 3종, 멀티 ID 독립 라우팅+카운터.

## Verification

- build 경고 0, test 65/65.

## Deviations

- 없음. `CreateSlave` 미dispose에 대한 CA2000 미발생 (소유권 이전 인정).

## Approval

- [x] director approved (auto — task 1 blanket approval 2026-10-11)
