# Task 3 Stage 4: 폴링 + Raw Hex + 로그

Issue: #3

## Changes

- `Client/Services/ObservableLogger.cs` (신규) — IModbusLogger → capped
  ListBox 바인딩 (Dispatcher 마샬, 500줄).
- `Rtu/ModbusConnection` — logger 주입 (트래픽 자동 기록).
- `ReadWriteViewModel.RunOnceAsync` 추출 (폴링 재사용).
- `Client/ViewModels/PollingViewModel.cs` (신규) — PeriodicTimer 루프,
  중복 가드, 디코딩 히스토리 200줄 (UI 어피니티 의도적 유지).
- MainWindow 탭 3종: Read/Write, Polling, Log.

## Verification

- build 경고 0, test 75/75.
- 탭·폴링 동작은 director 실행 확인 필요 (VIRTUAL:nova-test-slave 연결 →
  Execute/Polling Start).

## Deviations

- 없음.

## Approval

- [x] director approved (auto — task 3 blanket approval 2026-10-11)
