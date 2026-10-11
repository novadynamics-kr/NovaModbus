# Task 19 Report: Slave Holding HEX 주소 컬럼 + 0x 입력

Issue: #19

## Changes

- `RegisterRow.AddressHex` + 그리드 AddressHex 컬럼.
- 값 컬럼 제목 Value(Dec)/Value(Hex).
- 주소 입력 0x HEX 파싱 (`ParseHexAddress` 공유: 추가·범위·float,
  일관성을 위해 coil도 동일 적용).

## Verification

- build 경고 0, test 127/127.

## Deviations

- coil 주소도 0x 허용으로 통일 (holding만 하면 혼란).

## Unresolved / Follow-ups

1. Slave 실행 후 눈 확인.
2. 실물 COM 검증.

## Merge readiness

- Branch: `task/19-slave-hex` (local-only) → `main` merge 대기.
- Gate: build 무경고 + test 127/127 확인됨.
- [x] director termination approval (blanket 2026-10-11) → push + close #19.
