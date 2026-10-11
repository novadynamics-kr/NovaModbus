# Task 21: End(range) 0x HEX 확정 + 공용 주소 파서

Issue: #21

## Goal

3곳 End 파싱을 테스트된 공용 파서로 일원화 + 라벨 명시.

## Non-goals

- 실물 COM 검증

## Steps (2)

1. **공용 파서 + 리팩토링** — `ModbusAddress.Parse` + 테스트, 3곳 전환, 라벨.
2. **게이트 + 보고** — 관문·보고·merge·push·close.

## Quality gates

build / test / regression.

## Approval

- [x] director approved — blanket (auto-proceed + termination), 2026-10-11
