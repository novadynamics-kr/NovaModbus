# NovaModbus

Windows .NET 10 기반 Modbus RTU 솔루션. 외부 Serial/Modbus/UI NuGet 없이
(`System.IO.Ports`만 허용) 직접 구현한 라이브러리 + WPF Client/Slave.

## 구성

```text
NovaModbus.slnx
├── src/NovaModbus.Rtu/ ...... 라이브러리 (net10.0, UI 의존성 없음)
├── src/NovaModbus.Client/ ... Modbus Client (WPF, net10.0-windows)
├── src/NovaModbus.Slave/ .... Slave 시뮬레이터, 멀티 ID (WPF)
└── tests/NovaModbus.Rtu.Tests/ (MSTest, test 119)
```

지원 Function: `01,02,03,04,05,06,07,08,0B,0C,0F,10,11,14,15,16,17,18` +
Exception 응답 (`01-08,0A,0B`). 제외: `2B`/MEI, 사용자 정의 FC.

## 요구 사항

- .NET 10 SDK, Windows (WPF는 `net10.0-windows`)
- NuGet 추가 금지 (서드파티 Serial/Modbus/UI 일체, `System.IO.Ports` 제외)

## 빌드·테스트·실행

```powershell
dotnet build NovaModbus.slnx -c Release
dotnet test NovaModbus.slnx -c Release
dotnet run --project src/NovaModbus.Client -c Debug
dotnet run --project src/NovaModbus.Slave -c Debug
```

단일 테스트:

```powershell
dotnet test tests/NovaModbus.Rtu.Tests --filter "FullyQualifiedName~Crc16"
```

실물 COM포트 테스트는 `[TestCategory("Hardware")]`로 분리, 기본 제외.

## 라이브러리 사용법

시리얼 연결:

```csharp
using NovaModbus.Rtu;
using NovaModbus.Rtu.Transport;

ISerialTransport port = await TransportFactory.OpenSerialAsync(
    new SerialSettings("COM3", BaudRate: 9600));
await using var client = new ModbusRtuClient(port, baudRate: 9600);

ushort[] regs = await client.ReadHoldingRegistersAsync(slaveId: 1, start: 0, count: 10);
await client.WriteSingleCoilAsync(1, address: 7, on: true);
```

가상 라인 (하드웨어 없이 Client↔Slave 연동 시험):

```csharp
var (left, right) = VirtualTransport.CreatePair();
await using var client = new ModbusRtuClient(left);
await using var slave = new ModbusRtuSlave(right);
slave.Engine.AddSlave(1);
await new SlaveServer(slave).RunAsync(ct);
```

슬레이브 직접 디스패치 (순수 함수, 테스트 용이):

```csharp
byte[]? response = slave.HandleRequest(requestWire); // null = 무응답
```

## Client / Slave 앱

- **Client**: 좌측 시리얼 설정 (COM 목록·속도 선택/직접입력) + 연결,
  Read/Write 탭 (18종 FC), Polling 탭, Raw Hex 로그 탭, Watch 탭
  (ID·주소 목록 등록 + 값 전시). 내장 가상 슬레이브
  (`VIRTUAL:nova-test-slave`)로 하드웨어 없이 전 기능 시험 가능.
- **Slave**: 시리얼 리스너 + 멀티 ID 관리, Holding/Coils 편집,
  요청 카운터, 에러 주입 (CRC 무시·Busy), 프레임 로그.
  편집 데이터는 재시작 후에도 유지.

## 문서

- `AGENTS.md` — 에이전트 작업 규칙 (빌드·컨벤션·제약)
- `docs/` — 작업 기록 (orders/plans/working/report/feedback/tech/troubleshootings)
