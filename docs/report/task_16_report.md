# Task 16 Report: Client Watch 다중 선택 삭제

Issue: #16

## Changes

- `WatchViewModel.RemoveEntries` 일괄 제거 (Slave 패턴 통일).
- 단일 선택 커맨드/속성 제거, Del 버튼 + Delete 키 코드비하인드.
- 그리드 Extended 선택.

## Verification

- build 경고 0, test 127/127.

## Deviations

- 없음.

## Unresolved / Follow-ups

1. Client 실행 후 눈 확인.
2. 실물 COM 검증.

## Merge readiness

- Branch: `task/16-watch-multidelete` (local-only) → `main` merge 대기.
- Gate: build 무경고 + test 127/127 확인됨.
- [x] director termination approval (blanket 2026-10-11) → push + close #16.
