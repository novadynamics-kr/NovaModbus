# Task 3 Stage 1: VirtualSerialBus + transport 팩토리

Issue: #3

## Changes

- `Rtu/Transport/VirtualTransport.cs` (신규) — 인메모리 endpoint pair
  (`CreatePair`), open/close·멱등 dispose.
- `Rtu/Transport/VirtualSerialBus.cs` (신규) — 이름付き handoff
  (Publish/Connect/Unpublish/List) + `Shared` 싱글톤.
- `Rtu/Transport/TransportFactory.cs` (신규) — `OpenSerialAsync`/
  `OpenVirtual` 단일 호출점.
- `tests/.../VirtualBusTests.cs` (신규, 4개) — pair 왕복, 명명 handoff
  독점성, 미등록 거부, framer 경유 전체 프레임 왕복.

## Verification

- build 경고 0, test 71/71.

## Deviations

- 없음. Fake(테스트 전용)와 달리 Rtu 소속 구현으로 분리 — Client/Slave
  제품 코드에서 사용 가능.

## Approval

- [ ] director approved (date)
