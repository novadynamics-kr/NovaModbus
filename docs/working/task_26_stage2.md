# Task 26 Stage 2: 순수 로직 단위테스트
Changes:
- `tests/NovaModbus.Client.Tests/ValueFormatterTests.cs` (신규, 7): 비트/레지스터/바이트/DeviceId 포맷 벡터 + null 가드.
- `tests/NovaModbus.Client.Tests/WatchEntryTests.cs` (신규, 4): AddressHex, 값 동등성, 선택 알림/억제.
- `tests/NovaModbus.Client.Tests/WatchListStoreTests.cs` (신규, 3): Save/Load round-trip, 분실 파일, 인자 가드.
- `tests/NovaModbus.Slave.Tests/RegisterMapStoreTests.cs` (신규, 3): Save/Load round-trip, null JSON 거부, 인자 가드.
- 제외 (계획대로): Dispatcher 마샬(`ObservableLogger`), ViewModel 오케스트레이션, WPF Helpers.
Verification: `dotnet test NovaModbus.slnx -c Release` — Rtu 159 + Client 14 + Slave 3 = 176/176, 실패 0.
Deviations from plan: none.
Approval: [ ] director approved (date)
