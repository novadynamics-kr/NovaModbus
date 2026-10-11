# Task 23 Report: Watch Add Range 스레드 오류 수정

Issue: #23

## Cause

`AddRangeAsync`가 `ConfigureAwait(false)` 이후 백그라운드에서 바인딩된
`Entries`/`Results`를 직접 Add. INPC와 달리 컬렉션 변이는 마샬되지 않음.

## Changes

- Add Range 연속부 UI 복귀 (무거운 루프는 `Task.Run` 유지).
- 잠복 2건 선제 수정: Watch auto-refresh 루프 UI 어피니티
  (`Results.ToArray()` 읽기 포함 동일 위반).
- Slave 점검: 교체 방식(INPC)이라 안전 — 변경 없음.
- AGENTS.md 스레드 규칙에 읽기·오케스트레이션 항목 추가.

## Verification

- build 경고 0, test 130/130.

## Deviations

- 없음.

## Merge readiness

- Branch: `task/23-watch-range-thread` (local-only) → `main` merge 대기.
- Gate: build 무경고 + test 130/130 확인됨.
- [ ] director merge approval → `git push nova main` → issue #23 close.
