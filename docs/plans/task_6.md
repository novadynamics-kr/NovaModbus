# Task 6: 전 function·exception 코드 구현

Issue: #6

## Goal

Function 07,08,0B,0C,11,14,15,16,17,18 + Exception 07의 코덱·Client·
Slave·테스트 완성.

## Non-goals

- 2B/MEI, 사용자 정의 FC, UI 노출, 실물 COM 검증

## Steps (4)

1. **Enum·Limits·코덱** — function/exception 추가, 코덱 9종 + 코덱 테스트.
2. **스토어 확장** — 진단 카운터·이벤트 로그·파일·FIFO·서버 ID·status.
3. **Client + Slave** — 메서드 11종·디스패치 10종 + 스크립트/디스패치 테스트.
4. **게이트 + 보고** — 전 관문, AGENTS 현행화, 최종 보고, merge·push·close.

## Quality gates

build / test / spec-vector / regression.

## Approval

- [x] director approved — blanket (auto-proceed + termination), 2026-10-11
