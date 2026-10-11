# AGENTS.md — NovaModbus

Windows .NET 10 기반 Modbus RTU 솔루션용 에이전트 규칙.

## 0. Agent Bootstrap (read order)

1. `AGENTS.md` (this file)
2. `rules/hyper-waterfall.md` — Hyper-Waterfall 방법론 (NovaModbus 커스터마이즈)
3. `rules/docs-and-git-workflow.md` — 문서·Git·gh 운영 절차
4. 작업 시: `docs/orders/<today>.md` → 최신 `docs/working/*` → `docs/plans/*` → 관련 `docs/feedback/*`·`docs/tech/*`

구체 문서가 요약과 다르면 구체 문서를 따른다.

## 1. 솔루션 개요

- `NovaModbus.sln`
- `src/NovaModbus.Rtu/` — Modbus RTU 라이브러리, `net10.0`, UI 의존성 없음
- `src/NovaModbus.Client/` — Modbus Client, WPF, `net10.0-windows`
- `src/NovaModbus.Slave/` — Modbus Slave 시뮬레이터 (멀티 ID), WPF, `net10.0-windows`
- `tests/NovaModbus.Rtu.Tests/` — MSTest, `net10.0`
- 언어: 코드·주석 영어, 문서 한국어 가능

## 2. Hard Constraints (사용자 확정)

1. Serial / Modbus 관련 서드파티 NuGet 금지 (`NModbus`, `EasyModbus`, `FluentModbus`, `RJCP.SerialPortStream` 등).
   - 허용: Microsoft 1st-party `System.IO.Ports` (시리얼 I/O용). 버전은 `dotnet add package` 최신 10.x 고정, `Directory.Packages.props` 미사용 시 각 csproj에 명시.
   - Modbus 프레임·CRC·펑션코덱은 직접 구현. `System.IO.Ports` 외 시리얼 래퍼 금지.
2. UI·UX 관련 외부 NuGet 금지 (`MahApps`, `MaterialDesignInXaml`, `CommunityToolkit.Mvvm` 포함).
   - WPF 기본 컨트롤 + XAML만 사용. MVVM은 `INotifyPropertyChanged` 수동 구현.
   - WinForms는 2차 옵션. 추가 시 별도 프로젝트로 분리하고 WPF 코드와 공유 금지.
3. `NovaModbus.Rtu`는 `net10.0` 유지, WPF·Windows 전용 API 참조 금지 (`-windows` 타깃 금지).
   - 시리얼은 `ISerialTransport`로 추상화. `System.IO.Ports.SerialPort` 어댑터는 Rtu 내부에 두되 `net10.0` 호환 범위만 사용.
4. Slave는 멀티 ID 시뮬레이션 지원. `SlaveId → DataStore` 딕셔너리 구조, ID별 독립 데이터·카운터.

## 3. Build / Test / Run

```powershell
dotnet build NovaModbus.slnx -c Release
dotnet test NovaModbus.slnx -c Release
dotnet run --project src/NovaModbus.Client -c Debug
dotnet run --project src/NovaModbus.Slave -c Debug
```

- 단일 테스트: `dotnet test tests/NovaModbus.Rtu.Tests --filter "FullyQualifiedName~Crc16"`
- NuGet 추가 전 사용자 승인 필수. 승인 없이 `dotnet add package` 금지(단, `System.IO.Ports`는 승인済).

## 4. 프로젝트 규칙

### 4.1 공통 MSBuild

- `Nullable enable`, `ImplicitUsings enable`, `TreatWarningsAsErrors=true`
- `LangVersion latest`, `AnalysisMode Recommended`
- C# 14 문법 허용. 파일당 1 public 타입 원칙.

### 4.2 NovaModbus.Rtu 구조

```
Enums/ (ModbusFunction, ModbusExceptionCode)
Crc16.cs — Modbus CRC-16/ARC 직접 구현
ModbusFrame.cs — [Addr(1)][Func(1)][Data(N)][CRC LE(2)] 인코딩/디코딩
Codecs/ — Fc01,02,03,04,05,06,15,16 요청/응답 파서
Transport/
  ISerialTransport.cs — interface (테스트용 Fake 주입)
  SystemPortsSerialTransport.cs — System.IO.Ports 어댑터
  RtuFramer.cs — 3.5char silent interval 기반 프레임 분리
ModbusRtuClient.cs — timeout/retry, async + CancellationToken
ModbusRtuSlave.cs — SlaveId 필터, Exception 응답 생성
ModbusDataStore.cs — Coils/Discrete/Holding/Input, lock 기반 thread-safe
ModbusSlaveEngine.cs — 멀티 ID 딕셔너리 + per-ID 카운터
Errors/ — ModbusException, CrcMismatchException 등
Logging/ — IModbusLogger (Action<string> 기반, 외부 로거 의존 없음)
```

- 지원 Function 1차: `01,02,03,04,05,06,15,16` + Exception 응답. `08,11,17,22,23`은 2차.
- CRC는 little-endian (`CrcLo` 먼저). Baudrate 연동 3.5char 계산식으로 타임아웃 산출.
- 모든 public API에 XML doc. 모든 codec에 round-trip 단위테스트.

### 4.3 Client / Slave WPF 규칙

- `net10.0-windows`, `UseWPF=true`, `Nullable enable`
- 폴더: `Views/ + ViewModels/ + Services/` (ViewModel은 수동 `INotifyPropertyChanged`, 베이스 클래스 직접 구현)
- 시리얼 I/O 블로킹 금지. `async/await` + `CancellationToken`, UI 갱신은 `Dispatcher` 경유.
- 폴링은 `DispatcherTimer` 또는 `PeriodicTimer` + `CancellationTokenSource`, 중복 실행 가드 필수.
- Client 기능: 포트/보레이트/Parity/DataBits/Stop/SlaveId/Timeout 설정, FC별 읽기·쓰기 탭, 주기 폴링, Raw Hex 뷰어, 로그 `ListBox`.
- Slave 기능: 멀티 ID 관리(추가·삭제·선택), ID별 `Holding/Coils DataGrid` 편집, 요청 카운터, 에러 주입(CRC 무시·Busy 응답), 수신 프레임 로그.

## 5. 코딩 컨벤션

- 네임스페이스: `NovaModbus.Rtu`, `NovaModbus.Client`, `NovaModbus.Slave` (폴더와 일치).
- 비동기 메서드는 `Async` 접미사 + `CancellationToken` 기본값. `ConfigureAwait(false)`는 라이브러리에서 사용, WPF ViewModel에서는 미사용.
- `SerialPort` 직접 노출 금지. 항상 `ISerialTransport` 경유.
- 매직넘버 금지: FC 코드·예외 코드·타이밍 상수는 enum/const.
- 로그에 원시 바이트는 대문자 Hex + 공백 구분 (`01 03 00 00 00 0A C5 CD`).

## 6. 테스트 규칙 (MSTest)

- 프레임워크: MSTest (`Microsoft.Testing.Platform` 불필요, 기본 `dotnet new mstest` 템플릿).
- 필수 테스트: CRC 벡터(공식 예제 포함), 각 FC encode/decode round-trip, CRC 불일치 거부, Exception 프레임 파싱, `ModbusDataStore` 경계 검사, 멀티 ID 라우팅, `FakeSerialTransport` 기반 Client↔Slave 루프백.
- 실물 COM포트 필요 테스트는 `[TestCategory("Hardware")]` + `Skip` 기본, CI에서 제외.

## 7. Hyper-Waterfall (methodology)

출처: <https://github.com/edwardkim/rhwp> — 1인 개발·.NET 단일 솔루션 규모에
맞게 축소 커스터마이즈. 정본은 `rules/`를 따른다.

- **거시 워터폴 + 미시 애자일**: 계획→설계→구현→검증→통합 흐름을 문서로
  통제하고, 단계 내 실행은 AI 빠른 반복으로 수 시간 사이클 완성.
- **3원칙**: (P1) 목표·규칙을 파일에 유지 (`AGENTS.md`+`rules/`+`docs/`),
  (P2) 통제권은 작업지시자 — AI가 종료 선언 금지, 아키텍처 변경은 승인 필수,
  (P3) 컨텍스트 유지 주기적 확인 — 결정사항은 채팅이 아닌 파일에 외부화.
- **사이클**: Issue 등록(중복 검색 우선) → AI 계획서(3-6단계) → 승인 →
  단계별 구현+보고서 → 단계별 승인 → 피드백(`docs/feedback/`, 사람이 작성) →
  최종 보고 → 승인 후 merge·close. 1단계 = 1커밋(코드+문서 함께).
- **품질 관문**: 계획 승인 → `build` 무경고 → `test` 전체 통과 →
  Modbus 스펙 벡터 대조 → 회귀 없음(기존 테스트 전부 녹색, 기준 완화 금지).
- **GitHub 접근**: API 작업(이슈)은 로컬 `gh` CLI, 파일 송수신은
  gh 인증 기반 git (`nova` 리모트). task 브랜치는 로컬 전용
  (원격 브랜치·PR 없음). 단계 승인은 로컬 커밋까지, merge·close는 별도 승인.
  승인 후 main에 merge → gate → `git push nova main` 직접 반영.
  상세 절차·템플릿은 `rules/docs-and-git-workflow.md`.
- 바이브 코딩 전락 조건: 계획 없는 구현, 미검증 수락, "좋은 것 같습니다" 승인.

## 8. Do Not

- `.git/`, `obj/`, `bin/` 직접 편집 금지.
- `src/*.csproj`에 UI·Modbus 서드파티 `PackageReference` 추가 금지.
- Rtu에서 `System.Windows`, `Microsoft.Win32.Registry` 등 Windows 전용 참조 금지.
- 미승인 파일 대량 생성 금지. 스캐폴드 외 신규 프로젝트 추가 시 먼저 논의.
