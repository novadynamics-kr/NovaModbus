# Task 21 Report: End(range) 0x HEX 확정 + 공용 주소 파서

Issue: #21

## Finding

3곳 End 파싱은 이미 0x 대응済 (Task 18/19) — 라벨에만 미표시.
공용 파서로 일원화 + 테스트로 확정.

## Changes

- `Rtu/ModbusAddress.Parse` (0x/dec + 용량 검사) + 테스트 3종.
- Client/Slave 중복 파서 제거, 공용 위임.
- End 라벨 3곳에 (dec/0x) 명시.
- 빈 0x 입력 시 `Convert` 내부 `ArgumentOutOfRangeException` 누수 수정.

## Verification

- build 경고 0, test 130/130.

## Deviations

- 없음.

## Merge readiness

- Branch: `task/21-hex-address` (local-only) → `main` merge 대기.
- Gate: build 무경고 + test 130/130 확인됨.
- [x] director termination approval (blanket 2026-10-11) → push + close #21.
