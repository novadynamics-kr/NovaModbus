# Task 9 Report: Slave 시뮬레이터 UI

Issue: #9

## Changes

- 좌측: 시리얼 설정 + Start/Stop + 상태, Slave ID 목록 (Add/Del/선택),
  에러 주입 체크 2종 (live 적용).
- 우측 탭: Holding 편집 (10진/0x 입력·무효 거부), Coils 체크,
  카운터 (1s 타이머), 프레임 로그.
- `ModbusDataStore` 영속 (재시작해도 편집값 유지), ID hot-add/remove.
- `ObservableLog` (Client 로거와 동형, Slave 소속).

## Verification

- build 경고 0, test 119/119.
- 화면 동작은 director 실행 확인 필요 (COM 연결 후 Client에서 읽기).

## Deviations

- 가상 publish 제외 (프로세스 경계로 무의미 — 보고서 기록).
- CS0121 (`ToString(null)` 모호) → InvariantCulture 명시.

## Unresolved / Follow-ups

1. Slave 실행 후 눈 확인.
2. README, 실물 COM 검증.

## Merge readiness

- Branch: `task/9-slave-ui` (local-only) → `main` merge 대기.
- Gate: build 무경고 + test 119/119 확인됨.
- [x] director termination approval (blanket 2026-10-11) → push + close #9.
