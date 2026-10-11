# Task 12 Report: Slave 사용자 레지스터 목록 + 파일 저장/불러오기

Issue: #12

## Changes

- `Slave/Services/RegisterMapStore.cs` — JSON 저장/불러오기 (DTO 내장).
- `MainViewModel`: ID별 사용자 맵, 추가/삭제/저장/불러오기, 전체 그리드→
  목록 전환. 맵은 스토어와 함께 영속.
- XAML: Holding/Coils 탭에 입력 바 + Add/Del/Save/Load.

## Verification

- build 경고 0, test 124/124.

## Deviations

- WPF 프로젝트 ImplicitUsings에 `System.IO` 없음 확인 → 명시 using
  (Client도 동일하므로 `File` 사용 시 주의).
- 저장 시 현재값으로 스냅샷 (편집 후 저장이 최신 반영).

## Unresolved / Follow-ups

1. Slave 실행 후 눈 확인 (추가→저장→삭제→불러오기).
2. 실물 COM 검증.

## Merge readiness

- Branch: `task/12-slave-map` (local-only) → `main` merge 대기.
- Gate: build 무경고 + test 124/124 확인됨.
- [x] director termination approval (blanket 2026-10-11) → push + close #12.
