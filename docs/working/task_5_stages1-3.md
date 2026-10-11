# Task 5 Stages 1-3: Watch 목록 + Read All + 화면

Issue: #5

## Changes

- Stage 1: `WatchArea` enum, `WatchEntry` record, `WatchResult` 행 모델,
  `WatchViewModel` (등록/삭제·입력 파싱·ModbusLimits 검증).
- Stage 2: `Services/ValueFormatter` 공유 포맷터 (ReadWrite 전환),
  `WatchViewModel.ReadAllAsync` 영역별 일괄 읽기 + 행별 에러.
- Stage 3: 자동 새로고침 토글 (PeriodicTimer, 중복 가드, `IDisposable`),
  MainWindow Watch 탭 (등록 폼 + entries/results DataGrid).

## Verification

- build 경고 0, test 79/79.
- 화면 동작은 director 실행 확인 필요 (가상 연결 → Add → Read All → Auto-refresh).

## Deviations

- 동일 파일 진화로 stage 1-3 단일 커밋 (보고서로 단계 기록 대체).
- CA1001 → `WatchViewModel : IDisposable` + MainViewModel 정리 체인.

## Approval

- [x] director approved (auto — task 5 blanket approval 2026-10-11)
