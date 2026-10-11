# Task 20: 범위 일괄 추가 상한 65535로 확대

Issue: #20

## Goal

Client Watch + Slave Holding/Coils 범위 상한 65535.

## Non-goals

- 실물 COM 검증

## Steps (2)

1. **상한 교체** — `ModbusLimits.MaxBulkAdd` 신설 + 3곳 적용.
2. **게이트 + 보고** — 관문·보고·merge·push·close.

## Quality gates

build / test / regression.

## Approval

- [x] director approved — blanket (auto-proceed + termination), 2026-10-11
