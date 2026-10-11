# Task 10 Report: Slave float32 입력 UI

Issue: #10

## Changes

- `Rtu/Codecs/Float32Codec.cs` — ABCD/CDAB/BADC/DCBA 인코딩/디코딩.
- `tests/.../Float32CodecTests.cs` (4개) — 1.0 벡터, 4 오더 왕복,
  오더 상이성, 길이 거부.
- Slave Float 탭: 주소·값·오더 + 쓰기 + 상태 (선택 ID 스토어, 2레지스터
  경계 검사, Holding 그리드 즉시 갱신).

## Verification

- build 경고 0, test 123/123.

## Deviations

- 없음.

## Unresolved / Follow-ups

1. Slave 실행 후 눈 확인.
2. 실물 COM 검증.

## Merge readiness

- Branch: `task/10-slave-float` (local-only) → `main` merge 대기.
- Gate: build 무경고 + test 123/123 확인됨.
- [x] director termination approval (blanket 2026-10-11) → push + close #10.
