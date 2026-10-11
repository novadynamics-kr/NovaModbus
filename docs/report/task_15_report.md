# Task 15 Report: Client Watch UX

Issue: #15

## Changes

- 선택 삭제 (Del 버튼 + Delete 키 + 코드비하인드), 기존 중복 선택 정리.
- 항목 Start→Address + AddressHex 컬럼, 결과 Values→DEC/HEX 분할.
- `ValueFormatter` Dec/Hex/Btyes 포맷 4종, Watch 읽기 전종 분할 반환.
- `WatchListStore` JSON 저장/불러오기 (enum 문자열), Save/Load 버튼.

## Verification

- build 경고 0, test 127/127.

## Deviations

- 없음.

## Unresolved / Follow-ups

1. Client 실행 후 눈 확인.
2. 실물 COM 검증.

## Merge readiness

- Branch: `task/15-watch-ux` (local-only) → `main` merge 대기.
- Gate: build 무경고 + test 127/127 확인됨.
- [x] director termination approval (blanket 2026-10-11) → push + close #15.
