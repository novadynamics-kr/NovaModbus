# Task 11 Report: Holding HEX 병기 + word order Endian 표기

Issue: #11

## Changes

- `FloatWordOrderText.ToDisplayString`: Big Endian (ABCD), Big Endian Byte
  Swap (BADC), Little Endian Byte Swap (CDAB), Little Endian (DCBA).
- Slave Float 콤보를 표시 문자열로 (`FloatOrderOption`), 상태 메시지에 표기.
- Slave Holding 그리드에 Hex 읽기전용 컬럼 (`RegisterRow.HexText`).
- Client는 기존 `[i] 0xXXXX (n)` 병기로 요건 충족 (변경 없음).

## Verification

- build 경고 0, test 124/124.

## Deviations

- 없음.

## Unresolved / Follow-ups

1. Slave 실행 후 눈 확인.
2. 실물 COM 검증.

## Merge readiness

- Branch: `task/11-hex-wordorder` (local-only) → `main` merge 대기.
- Gate: build 무경고 + test 124/124 확인됨.
- [x] director termination approval (blanket 2026-10-11) → push + close #11.
