# Task 8: Client Watch 읽기 FC 전종 확장

Issue: #8

## Goal

Watch 영역에 07,08,0B,0C,11,14,18 추가 (쓰기 제외).

## Non-goals

- 쓰기 편입, Slave.exe UI, 실물 COM 검증

## Steps (3)

1. **모델 확장** — WatchArea 7종 + WatchEntry Extra.
2. **실행 + 화면** — 영역별 읽기, Extra 입력, 항목 컬럼.
3. **게이트 + 보고** — 관문·보고·merge·push·close.

## Quality gates

build / test / regression.

## Approval

- [x] director approved — blanket (auto-proceed + termination), 2026-10-11
