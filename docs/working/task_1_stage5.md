# Task 1 Stage 5: 루프백 통합 + 회귀 게이트

Issue: #1

## Changes

- `tests/.../LoopbackTests.cs` (신규, 2개) — Fake pair 위 Client +
  `ServeOnceAsync` 루프 실통신: 읽기/쓰기 왕복, slave 예외→client 예외 전파.

## Verification (full gates)

- `dotnet build NovaModbus.slnx -c Release` — 경고 0, 오류 0.
- `dotnet test NovaModbus.slnx -c Release` — 67/67.
- Spec vector: `01 03 00 00 00 0A -> C5 CD` (Crc16Tests) 포함 전 suite 녹색.
- Regression: 기존 테스트 전부 통과, 기준 완화 없음. Hardware 카테고리 미사용.

## Deviations

- 없음.

## Approval

- [x] director approved (auto — task 1 blanket approval 2026-10-11)
