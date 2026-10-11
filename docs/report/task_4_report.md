# Task 4 Report: 스레드 안전 리팩토링 + 오류 처리

Issue: #4

## Cause

`AsyncRelayCommand.Execute`의 `ConfigureAwait(false)` 체인 이후 스레드풀에서
`CanExecuteChanged` 발생 → WPF Button에 백그라운드 접근 → 크래시.
`INPC`는 바인딩 엔진이 마샬하지만 command 경로는 마샬하지 않음.

## Changes

- `Rtu/Commands/` 신설: `RelayCommand`·`AsyncRelayCommand` 이동 +
  생성 시점 `SynchronizationContext` 캡처, `CanExecuteChanged`·에러콜백 마샬.
- Client 4개 VM 네임스페이스 전환 (`Services` commands 삭제).
- `tests/.../CommandTests.cs` (4개): 백그라운드 raise 마샬, 에러 라우팅,
  재진입 가드, predicate.
- `AGENTS.md`: UI 스레드 규칙 + Rtu 구조 현행화.
- `docs/troubleshootings/cross-thread-command.md`: 원인·수정·재발방지.

## Verification

- build 경고 0, test 79/79.

## Deviations

- 자동수행이라 단계별 커밋 대신 단일 커밋 (속도 우선, 보고서로 단계 기록 대체).
- 테스트 데드락 1건 수정: 커스텀 컨텍스트 하 `await`는 continuation이
  Drain을 기다려 교착 → 블로킹 대기 + 동기 테스트로 변경 (규칙에 반영).

## Unresolved / Follow-ups

1. Client 실행 후 크래시 재현 경로(연결→Execute→Polling)로 수정 확인 필요.
2. Slave.exe UI (후속 Task).

## Merge readiness

- Branch: `task/4-thread-safety` (local-only) → `main` merge 대기.
- Gate: build 무경고 + test 79/79 확인됨.
- [x] director termination approval (blanket 2026-10-11) → push + close #4.
