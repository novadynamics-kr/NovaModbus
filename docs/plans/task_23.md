# Task 23: Watch Add Range 스레드 오류 수정

Issue: #23

## Goal

바인딩 컬렉션 변이를 UI 스레드로 복귀.

## Non-goals

- 실물 COM 검증

## Steps (2)

1. **수정 + 점검** — Client 연속부 UI 복귀, Slave 패턴 확인.
2. **게이트 + 보고** — 관문·보고·merge·push·close.

## Quality gates

build / test / regression.

## Approval

- [x] director approved — task approval (auto), 2026-10-11
