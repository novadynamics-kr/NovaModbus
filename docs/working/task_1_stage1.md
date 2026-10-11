# Task 1 Stage 1: RtuFramer 수신 루프

Issue: #1

## Changes

- `src/NovaModbus.Rtu/Transport/RtuFramer.cs` — `ReadFrameAsync` 추가.
  첫 바이트까지 `timeout` 대기 후 무음(silent interval) 구분으로 프레임 확정.
  사용자 취소는 throw, 타임아웃은 `null`/확정 프레임으로 구분.
  CRC·길이 검증 없음 (트랜잭션층 책임). `TimeoutException`을 보고하는
  transport(SerialPort류)도 무음으로 처리.
- `tests/NovaModbus.Rtu.Tests/Testing/FakeSerialTransport.cs` — 메모리
  transport. `EnqueueInbound`/`TakeWritten`/`CreatePair` 지원.
- `tests/NovaModbus.Rtu.Tests/RtuFramerTests.cs` — 6개 테스트.

## Verification

- `dotnet build NovaModbus.slnx -c Release` — 경고 0, 오류 0.
- `dotnet test` — 36/36 통과 (기존 30 + 신규 6).
- 무음 타이밍: 9600bps → 4.01ms 계산 확인, 115200bps → 2ms 하한 확인.

## Deviations from plan

- `ReadFrameAsync` 시그니처에서 `CancellationToken`을 마지막으로 이동
  (CA1068). `silentIntervalOverride` 테스트 훅 유지.
- 분석기 2건 대응: CA1068 (ct 순서), CA2016 (Fake의 Wait에 토큰 전달).

## Known risks (follow-up)

- 실물 `SerialPort.BaseStream.ReadAsync`의 취소 토큰 미지원 가능성 —
  Stage 5 하드웨어 검증 시 확인. `TimeoutException` 경로로 1차 방어済.

## Approval

- [x] director approved (2026-10-11)
