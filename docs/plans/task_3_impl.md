# Task 3 Impl Plan: 파일별 변경 계획

Parent: `docs/plans/task_3.md` (approved).

## Stage 1 — VirtualSerialBus + transport 팩토리

- `src/NovaModbus.Rtu/Transport/VirtualSerialBus.cs` (신규)
  - 이름 → pair 등록 허브. `CreateEndpoint(name)` / `RemoveEndpoint(name)`.
  - 양 끝은 `ISerialTransport` (Fake 재사용이 아닌 전용 경량 구현 —
    Fake는 테스트 프로젝트 소속이라 Rtu에서 사용 불가).
  - 스레드 안전 (lock), 미등록 이름 연결 시 `InvalidOperationException`.
- `src/NovaModbus.Rtu/Transport/TransportFactory.cs` (신규, static)
  - `OpenSerial(SerialSettings)` → `SystemPortsSerialTransport` (open 포함).
  - `OpenVirtual(string endpoint)` → bus endpoint.
- `tests/.../VirtualBusTests.cs` — 양방향 왕복, 미등록 거부, endpoint 제거 후
  거부, RtuFramer 경유 프레임 왕복 1건.

## Stage 2 — 연결 관리 (Client)

- `Client/Services/ModbusConnectionService.cs` — transport/client 생명주기,
  Serial-vs-Virtual 분기, 연결 상태 이벤트 (INPC 아님, 콜백/Action).
- `SerialPortSettingsViewModel` 확장 — `VIRTUAL:*` 목록 항목 + 선택.
- `MainViewModel` — 연결/해제 커맨드, 상태 텍스트.
- MainWindow 좌측 하단에 연결 버튼 + 상태.

## Stage 3 — FC 읽기/쓰기 탭

- `Client/ViewModels/ReadWriteViewModel.cs` + XAML TabControl —
  FC 선택, 주소/수량/값 입력, 실행, 결과 표시. 가상 루프백으로 동작 확인.

## Stage 4 — 폴링 + Hex + 로그

- `PollingViewModel` (PeriodicTimer + CTS + 중복 가드),
  Raw Hex 뷰어 (IModbusLogger 구현 → ObservableCollection),
  로그 ListBox.

## Stage 5 — 내장 슬레이브 + 게이트 + 보고

- `VirtualTestSlave` 호스트 (Client 내, 토글), 전 관문, 최종 보고.
