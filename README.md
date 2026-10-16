# NovaModbus

Windows .NET 10 기반 Modbus RTU 솔루션. 외부 Serial/Modbus/UI NuGet 없이
(`System.IO.Ports`만 허용) 직접 구현한 라이브러리 + WPF Client/Slave.

## 구성

```text
NovaModbus.slnx
├── src/NovaModbus.Rtu/ ...... 라이브러리 (net10.0, UI 의존성 없음)
├── src/NovaModbus.ClientWPF/ ... Modbus Client (WPF, net10.0-windows)
├── src/NovaModbus.Slave/ .... Slave 시뮬레이터, 멀티 ID (WPF)
├── tests/NovaModbus.Rtu.Tests/ (MSTest, net10.0)
├── tests/NovaModbus.ClientWPF.Tests/ (MSTest, net10.0-windows, 순수 로직)
└── tests/NovaModbus.Slave.Tests/ (MSTest, net10.0-windows, 순수 로직)
```

지원 Function: `01,02,03,04,05,06,07,08,0B,0C,0F,10,11,14,15,16,17,18` +
Exception 응답 (`01-08,0A,0B`). 제외: `2B`/MEI, 사용자 정의 FC.
브로드캐스트(주소 0) 쓰기 지원 (실행 후 무응답).

## 규격 준수

- MODBUS over Serial Line V1.02 + Application Protocol V1.1b3 대조
  (`docs/tech/modbus-rtu-conformance.md` 매트릭스).
- RTU 프레이밍: t3.5 분리·t1.5 문자간격 폐기·ADU 256B 상한,
  19200 초과 시 고정 타이머 (1.75ms / 750µs).
- 시리얼 기본값 Even 패리티 + 8 데이터비트 (11bit 문자 규칙 강제,
  무패리티 시 2 stop bits). `SerialSettings.Validate()`로 검증.
- 진단: FC08 서브펑션 (NAK·overrun·listen-only 포함),
  FC11/12 상태·카운터·이벤트로그는 스펙 의미론 준수.

## 요구 사항

- .NET 10 SDK, Windows (WPF는 `net10.0-windows`)
- NuGet 추가 금지 (서드파티 Serial/Modbus/UI 일체, `System.IO.Ports` 제외)

## 빌드·테스트·실행

```powershell
dotnet build NovaModbus.slnx -c Release
dotnet test NovaModbus.slnx -c Release
dotnet run --project src/NovaModbus.ClientWPF -c Debug
dotnet run --project src/NovaModbus.Slave -c Debug
```

단일 테스트:

```powershell
dotnet test tests/NovaModbus.Rtu.Tests --filter "FullyQualifiedName~Crc16"
```

커버리지 (VS Code용 HTML 리포트):

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File coverage.ps1
```

또는 VS Code 명령 팔레트 → "Tasks: Run Task" → "coverage: test + html report".
흐름: MSTest 이중 수집 (`XPlat Code Coverage` coverlet → 라인+브랜치 Cobertura,
`Code Coverage` Microsoft → VS용 `.coverage`) → HTML 생성 (`reportgenerator`).
`CodeCoverage.runsettings`가 `*Tests.dll`을 측정에서 제외하므로
결과에는 3개 제품 어셈블리(`NovaModbus.Rtu`/`NovaModbus.ClientWPF`/`NovaModbus.Slave`)만
포함된다 (Client/Slave는 `net10.0-windows` 테스트 프로젝트가 순수 로직을 커버).
`coverage-report/index.html`을 열어 확인한다 (라인·브랜치·클래스별 상세).
필요 도구는 `dotnet-tools.json`에 버전 고정되어 스크립트가 자동 복원한다.
수집물(`TestResults/`, `coverage-report/`)은 커밋하지 않는다.

실물 COM포트 테스트는 `[TestCategory("Hardware")]`로 분리, 기본 제외.

## 라이브러리 사용법

시리얼 연결 (기본 Even/8/1, 규칙 위반 시 예외):

```csharp
using NovaModbus.Rtu;
using NovaModbus.Rtu.Transport;

ISerialTransport port = await TransportFactory.OpenSerialAsync(
    new SerialSettings("COM3", BaudRate: 9600));
await using var client = new ModbusRtuClient(port, baudRate: 9600);

ushort[] regs = await client.ReadHoldingRegistersAsync(slaveId: 1, start: 0, count: 10);
await client.WriteSingleCoilAsync(1, address: 7, on: true);
```

브로드캐스트 쓰기 (fire-and-forget, t3.5 quiet 자동 준수):

```csharp
await client.BroadcastWriteSingleRegisterAsync(address: 10, value: 0x55AA);
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
  Read/Write 탭 (18종 FC), Polling 탭, Log 탭, Watch 탭
  (등록 + 결과 2그리드, 체크박스·드래그·Ctrl+A 다중 선택, 전체삭제).
  내장 가상 슬레이브 (`VIRTUAL:nova-test-slave`)로 하드웨어 없이 전 기능 시험 가능.
- **Slave**: 시리얼 리스너 + 멀티 ID 관리, Holding/Coils 편집
  (체크박스·드래그·Ctrl+A 다중 선택), Float 쓰기, 요청 카운터,
  에러 주입 (CRC 무시·Busy), 프레임 로그, 맵 저장/불러오기.

## 문서

- `AGENTS.md` — 에이전트 작업 규칙 (빌드·컨벤션·제약)
- `docs/` — 작업 기록 (orders/plans/working/report/feedback/tech/troubleshootings)
- `docs/tech/modbus-rtu-conformance.md` — Modbus 규격 적합성 매트릭스
