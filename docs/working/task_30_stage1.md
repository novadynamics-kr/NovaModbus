# Task 30 Stage 1: WPF Client → ClientWPF 개명
Changes:
- `src/NovaModbus.Client/` → `src/NovaModbus.ClientWPF/` (csproj 포함 19 파일).
  네임스페이스 `NovaModbus.Client.*` → `NovaModbus.ClientWPF.*` (XAML 포함).
- `tests/NovaModbus.Client.Tests/` → `tests/NovaModbus.ClientWPF.Tests/` (4 파일).
- `NovaModbus.slnx`, `.gitignore`, `coverage.ps1`, `CodeCoverage.runsettings`
  (Include/Exclude에 `ClientWPF`/`ClientWPF.Tests` 추가,
  `[NovaModbus.Client]*`는 WinForms先 대비 유지),
  `README.md`, `AGENTS.md` (§1·§5·실행 명령) 갱신.
- 이력 문서(`docs/orders/2026101*.md`, `task_2*`)는 보존.
Verification: `dotnet build -c Release` 경고 0·오류 0,
  `dotnet test` 176/176 (Rtu 159 + ClientWPF 14 + Slave 3).
Deviations from plan: none (지시 명명 그대로).
Notes:
- 추적 파일 개별 `git mv` (VS Code 잠금 대비, 목적지 사전 생성).
- 구 디렉터리 잔재 (bin/obj만) 제거됨.
- 삭제된 프로젝트의 obj 5건이 VS Code 디자인타임 빌드로 재생성되어 커밋 혼입 →
  적발 후 `rm --cached`+디스크 삭제·amend 정정.
Approval: [ ] director approved (date)
