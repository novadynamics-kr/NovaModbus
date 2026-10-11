# Task 11: Holding HEX 병기 + word order Endian 표기

Issue: #11

## Goal

Holding 10진+HEX 병기, float 오더 Endian 표기.

## Non-goals

- Client float UI, 실물 COM 검증

## Steps (3)

1. **Rtu 표시 문자열** — `FloatWordOrder` Endian 표기 + 테스트.
2. **Slave 반영** — Float 콤보·상태 표기, Holding Hex 컬럼.
3. **게이트 + 보고** — 관문·보고·merge·push·close.

## Quality gates

build / test / regression.

## Approval

- [x] director approved — blanket (auto-proceed + termination), 2026-10-11
