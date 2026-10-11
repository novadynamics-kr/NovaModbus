# Task 8 Report: Client Watch 읽기 FC 전종 확장

Issue: #8

## Changes

- `WatchArea` 7종 추가 (07,08,0B,0C,11,14,18), `WatchEntry.Extra`.
- `WatchViewModel`: 영역별 읽기 7종 + Extra 파서 (진단 바이트·파일 길이).
- `ValueFormatter.FormatDeviceId` 공유 (ReadWrite 중복 제거).
- XAML: Extra 입력 + 항목 Extra 컬럼.

## Verification

- build 경고 0, test 119/119.
- 화면 동작은 director 실행 확인 필요.

## Deviations

- 없음.

## Unresolved / Follow-ups

1. Client 실행 후 눈 확인.
2. Slave.exe UI, README, 실물 COM 검증.

## Merge readiness

- Branch: `task/8-watch-extended` (local-only) → `main` merge 대기.
- Gate: build 무경고 + test 119/119 확인됨.
- [x] director termination approval (blanket 2026-10-11) → push + close #8.
