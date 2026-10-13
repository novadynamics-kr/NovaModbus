# Task 26: Coverage report에 NovaModbus.Client, NovaModbus.Slave 포함
Issue: #26
Goal: VS Code HTML 리포트에 3개 제품 어셈블리(Rtu/Client/Slave)가 모두 표시되도록 한다. `*Tests` 제외 유지.
Non-goals: WPF 컨트롤·Dispatcher·실물 COM포트 테스트. 커버리지 수치 게이트 설정 안 함.

## 배경 (실측)
- 현재 Cobertura 패키지: `NovaModbus.Rtu` 단일, "Tests" 0건, branch 470/643 (73%).
- `NovaModbus.Rtu.Tests` (`net10.0`)는 Rtu만 참조하므로 Client/Slave 어셈블리가 테스트 프로세스에 로드되지 않음 → coverlet 수집 불가.
- Client/Slave는 `net10.0-windows` WPF. 순수 로직은 테스트 가능:
  - Client: `ValueFormatter` (static, 무의존), `WatchListStore` (JSON+파일), `WatchEntry` (INPC only, Dispatcher 없음).
  - Slave: `RegisterMapStore` (JSON+파일).
  - 제외: `ObservableLogger`/`ObservableLog` (Dispatcher 마샬), ViewModel 오케스트레이션, Helpers(`DataGridDragSelect`, UI 의존).

## Steps (5)
1. 신규 테스트 프로젝트 2개 생성 + slnx 등록: `tests/NovaModbus.Client.Tests`, `tests/NovaModbus.Slave.Tests` (`net10.0-windows`, MSTest). NuGet: `MSTest`, `coverlet.collector`, `Microsoft.CodeCoverage` (AGENTS.md §3 승인 필요). 각자 대응 제품 + Rtu 참조.
2. 순수 로직 단위테스트 (UI 스레드 규칙 준수 — 커스텀 컨텍스트 하 `await` 금지, 블로킹 대기): Client → `ValueFormatter` 포맷 벡터 + `WatchListStore` Save/Load round-trip + `WatchEntry` 동등성/선택 알림; Slave → `RegisterMapStore` Save/Load round-trip + 무효 파일 거부.
3. `CodeCoverage.runsettings`: XPlat collector에 `<Include>[NovaModbus.Rtu]*,[NovaModbus.Client]*,[NovaModbus.Slave]*</Include>` 명시. `Exclude`/`ExcludeByFile` (Tests) 유지.
4. `coverage.ps1`: 3개 테스트 프로젝트의 Cobertura 수집·병합 후 HTML 생성. `README.md` 커버리지 절 갱신.
5. Gate: `dotnet build -c Release` 무경고 → `dotnet test` 전체 통과 → HTML에 3개 패키지·"Tests" 0건 실측.

## 대안 (기각 제안)
- 기존 `Rtu.Tests`를 `net10.0-windows`로 전환 후 Client/Slave 참조 추가: 신규 프로젝트 없음. 단, Rtu 테스트가 Windows 전용이 되고 (현재 `net10.0` 크로스플랫폼), 어셈블리명(`Rtu.Tests`)과 내용 불일치. 채택 안 함.

## Quality gates
- build 경고 0·오류 0 / test 전체 통과(회귀 없음, 기준 완화 금지) / HTML 3패키지 실측 / `*Tests` 0건

## gh 기록
- Issue #26 생성 + self-assign (`gh issue edit 26 --add-assignee '@me'`).

Approval: [x] director approved (2026-10-13, 승인·착수 + NuGet 승인)
