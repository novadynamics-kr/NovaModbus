# Task 14: 주소 범위 0x0000~0xFFFF 전역 반영

Issue: #14

## Goal

DataStore 기본 전 범위화. Client/Slave는 기존 설계로 충족 (확인).

## Non-goals

- 벌크 1000 상한 유지

## Steps (2)

1. **스토어 + 테스트** — 기본 65536, 경계 테스트.
2. **게이트 + 보고** — 관문·보고·merge·push·close.

## Quality gates

build / test / regression.

## Approval

- [x] director approved — blanket (auto-proceed + termination), 2026-10-11
