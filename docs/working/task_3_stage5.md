# Task 3 Stage 5: 내장 슬레이브 토글 + 게이트 + 보고

Issue: #3

## Changes

- `MainViewModel` — 테스트 슬레이브 시작/중지 재구성, `TestSlaveEnabled`
  토글 (종료 시 published endpoint 회수), XAML CheckBox.

## Verification (full gates)

- build 경고 0, test 75/75.
- Spec vector (C5 CD) 포함 전 suite 녹색, 회귀 없음.

## Deviations

- CA2012 (fire-and-forget ValueTask) → held transport 동기 dispose로 변경
  (VirtualTransport dispose는 await 없음, 데드락 불가 — 주석 명시).
- CA1859 → 필드 타입 `VirtualTransport?`로 구체화.

## Approval

- [x] director approved (auto — task 3 blanket approval 2026-10-11)
