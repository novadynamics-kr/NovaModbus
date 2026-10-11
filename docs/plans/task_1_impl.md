# Task 1 Impl Plan: 파일별 변경 계획

Parent: `docs/plans/task_1.md` (approved).

## Stage 1 — RtuFramer 수신 루프

- `src/NovaModbus.Rtu/Transport/RtuFramer.cs` (수정)
  - `ReadFrameAsync(transport, baudRate, timeout, ct, silentIntervalOverride?)`
  - 첫 바이트까지 `timeout` 대기 → 없으면 `null` 반환.
  - 첫 바이트 후 무음(silent interval) 동안 데이터 없으면 프레임 확정.
  - 사용자 취소(`ct`)는 `OperationCanceledException`, 타임아웃 취소는
    `null`/완성 프레임으로 구분 (linked CTS + `ct.IsCancellationRequested` 판별).
  - Framer는 CRC를 검증하지 않는다 (구분만, 검증은 트랜잭션층).
- `tests/.../Testing/FakeSerialTransport.cs` (신규, 테스트 프로젝트 내)
  - `ISerialTransport` 메모리 구현. `EnqueueInbound()`, `TakeWritten()`,
    `CreatePair()` (client<->slave 양방향 연결).
- `tests/.../RtuFramerTests.cs` (신규)
  - 단일 프레임 수신, 청크 분할 도착 결합, 무음 간격 분리 2프레임,
    무응답 `null`, CRC 포함 프레임 그대로 통과.

## Stage 2 — 트랜잭션 코어 + Fake

- `src/NovaModbus.Rtu/ModbusTransaction.cs` (신규)
  - `ExchangeAsync(slaveId, function, requestBody, baudRate, retries, ct)`:
    encode → write → `RtuFramer.ReadFrameAsync` → SlaveId/Function/CRC 검증 →
    Exception 프레임이면 `ModbusSlaveException`. 재시도 후 `TimeoutException`.
- `ModbusRtuClient`는 이 코어의 얇은 래퍼가 된다.

## Stage 3 — ModbusRtuClient 본구현

- `src/NovaModbus.Rtu/ModbusRtuClient.cs` (재작성)
  - FC 8종 메서드 (`ReadCoilsAsync` ... `WriteMultipleRegistersAsync`),
    기존 시그니처(`ReadHoldingRegistersAsync`) 유지·확장.
- `tests/.../ClientLoopbackTests.cs` — Fake pair + 에코 slave 스텁으로 round-trip.

## Stage 4 — ModbusRtuSlave 본구현

- `src/NovaModbus.Rtu/ModbusRtuSlave.cs` (재작성)
  - `HandleRequestAsync(requestBytes, ct) -> responseBytes?` (순수 디스패치,
    단위테스트 용이): SlaveId 필터(미보유 ID·브로드캐스트 무응답),
    코덱 decode → DataStore 접근 → 응답 encode, 오류는 Exception 프레임.
  - `RunAsync` 수신 루프는 Stage 5로 (선택).
- `tests/.../SlaveDispatchTests.cs` — FC별 정상/예외/경계 케이스.

## Stage 5 — 루프백 + 게이트 + 보고

- 양방향 루프백 통합 테스트, `build`/`test`/스펙벡터/회귀 확인,
  `docs/report/task_1_report.md`, main merge + issue #1 close 요청.
