# Task 31 Stage 1: WPF Slave → SlaveWPF 개명
Changes:
- `src/NovaModbus.Slave/` → `src/NovaModbus.SlaveWPF/` (csproj 포함 17 파일).
  네임스페이스 `NovaModbus.Slave.*` → `NovaModbus.SlaveWPF.*` (XAML 포함).
- `tests/NovaModbus.Slave.Tests/` → `tests/NovaModbus.SlaveWPF.Tests/` (2 파일).
- `NovaModbus.slnx`, `.gitignore`, `coverage.ps1`, `CodeCoverage.runsettings`
  (Include에 `SlaveWPF` 추가, Exclude에 `SlaveWPF.Tests`·`Slave.WinForms.Tests` 추가,
  `[NovaModbus.Slave]*`는 WinForms先 대비 유지),
  `README.md`, `AGENTS.md` (§1·§5·실행 명령) 갱신.
- 이력 문서(`docs/orders/2026101*.md`, `task_3*`)는 보존.
Verification: `dotnet build -c Release` 경고 0·오류 0,
  `dotnet test` 209/209 (Rtu 159 + ClientWPF 14 + Client.WinForms 33 + SlaveWPF 3).
Deviations from plan: none (지시 명명 그대로).
Notes:
- 추적 파일 개별 `git mv` (VS Code 잠금 대비, 목적지 사전 생성).
  첫 시도에서 `-replace` 정규식 이스케이프 오류 (역슬래시 미매칭) →
  `String.Replace`로 재실행 성공.
- 구 디렉터리 잔재 (bin/obj만) 제거됨.
Approval: [ ] director approved (date)
