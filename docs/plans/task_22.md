# Task 22: 대량 등록 프리즈 해소

Issue: #22

## Goal

대량 범위 추가의 논블로킹 + 진행률.

## Non-goals

- 실물 COM 검증

## Steps (3)

1. **Slave 벌크화** — Dictionary 인덱스, 일괄 교체, 백그라운드 + ProgressBar.
2. **Client 벌크화** — 동일 패턴 (HashSet 인덱스 + 일괄 교체 + 진행률).
3. **게이트 + 보고** — 관문·보고·merge·push·close.

## Quality gates

build / test / regression.

## Approval

- [x] director approved — blanket (auto-proceed + termination), 2026-10-11
