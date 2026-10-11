# Task 7 Report: Client Read/Write 탭에 전 FC 노출

Issue: #7

## Changes

- `ReadWriteViewModel`: 기능 18종, `HintText` 동적 힌트, 파서
  (0x hex·파일 triple·마스크 쌍·RW17 시작값), 결과 포맷 10종.
- `ValueFormatter` 재사용 (Watch와 공유).
- `MainWindow`: 정적 라벨 → `HintText` 바인딩.

## Verification

- build 경고 0, test 119/119.
- 탭 동작은 director 실행 확인 필요 (가상 슬레이브: 파일 EnsureFile·FIFO
  미시딩 시 IllegalDataAddress 확인 가능).

## Deviations

- CA1822 5건 → 인스턴스 미사용 헬퍼 static화.
- FC11 additional 분리 UI 미노출 (단순 버전만, ID 길이 기지 필요).

## Unresolved / Follow-ups

1. Client 실행 후 눈 확인 (전 FC Execute).
2. Slave.exe UI, README, 실물 COM 검증.

## Merge readiness

- Branch: `task/7-client-fc` (local-only) → `main` merge 대기.
- Gate: build 무경고 + test 119/119 확인됨.
- [x] director termination approval (blanket 2026-10-11) → push + close #7.
