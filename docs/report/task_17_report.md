# Task 17 Report: Watch 선택 삭제 범위 오류 수정

Issue: #17

## Cause

- `WatchEntry` record 값 동등성 + Add 중복 검사 없음 → 동값 항목이
  선택 삭제 시 함께 제거됨.
- DataGrid 내장 삭제 기본값 True → 커스텀 핸들러와 중복 동작 가능.

## Changes

- `RemoveEntries` 참조 동등성 (`ReferenceEqualityComparer`) 전환.
- Add 중복 거부 ("Already in the list.").
- 양 그리드 `CanUserDeleteRows="False"`.

## Verification

- build 경고 0, test 127/127.

## Deviations

- 없음.

## Merge readiness

- Branch: `task/17-watch-delete-fix` (local-only) → `main` merge 대기.
- Gate: build 무경고 + test 127/127 확인됨.
- [ ] director merge approval → `git push nova main` → issue #17 close.
