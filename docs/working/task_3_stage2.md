# Task 3 Stage 2: 연결 관리 (Client)

Issue: #3

## Changes

- `Rtu/ModbusConnection.cs` (신규) — Serial/Virtual 연결·해제, Client 보유,
  `ConnectionChanged` 이벤트.
- `Rtu/SlaveServer.cs` (신규) — slave 구동 루프 (`LastError` 포함).
- `Client/Services/AsyncRelayCommand.cs` (신규) — 비동기 커맨드, 재진입 가드,
  에러 라우팅.
- `SerialPortSettingsViewModel` — `VIRTUAL:` 항목, 가상 선택 판별,
  bus 주입 ctor.
- `MainViewModel` (재작성) — 내장 테스트 슬레이브 자동 기동, 연결/해제,
  상태 텍스트, 종료 시 정리.
- `MainWindow` — 연결 버튼 + 상태, Closed 정리.
- `tests/.../ConnectionTests.cs` (신규, 4개) — 가상 왕복·해제, 미등록 거부,
  미존재 COM 예외.

## Verification

- build 경고 0, test 75/75.
- UI 동작은 director 실행 확인 필요 (자동 테스트 없음 — ViewModel이
  net10.0-windows 소속이라 Rtu.Tests에서 참조 불가).

## Deviations

- `ConnectMissingSerialPortThrows`: 미존재 포트는 `FileNotFoundException`
  (IOException 파생) —基底 타입 단언으로 변경.

## Approval

- [x] director approved (auto — task 3 blanket approval 2026-10-11)
