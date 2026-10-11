# Task 9: Slave 시뮬레이터 UI

Issue: #9

## Goal

Slave.exe UI (Client 패턴 참조): 연결·멀티 ID·편집·카운터·주입·로그.

## Non-goals

- 가상 publish (프로세스 경계), 실물 COM 검증

## Steps (3)

1. **연결 + ID 관리** — 시리얼 설정·Start/Stop, ID 추가/삭제/선택, 주입 체크.
2. **편집 + 계측** — Holding/Coils DataGrid (스토어 영속), 카운터·로그 탭.
3. **게이트 + 보고** — 관문·보고·merge·push·close.

## Quality gates

build / test / regression.

## Approval

- [x] director approved — blanket (auto-proceed + termination), 2026-10-11
