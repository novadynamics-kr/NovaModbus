# Task 27 Stage 1: WPF Client → Client.Wpf 개명
Changes:
- `src/NovaModbus.Client/` → `src/NovaModbus.Client.Wpf/` (csproj 포함 19 파일).
  네임스페이스 `NovaModbus.Client.*` → `NovaModbus.Client.Wpf.*` (XAML `x:Class` 포함).
- `tests/NovaModbus.Client.Tests/` → `tests/NovaModbus.Client.Wpf.Tests/` (4 파일, 참조 경로 포함).
- `NovaModbus.slnx`, `.gitignore`, `coverage.ps1`, `CodeCoverage.runsettings`
  (Include/Exclude에 `Client.Wpf`/`Client.Wpf.Tests` 추가, `[NovaModbus.Client]*`는 WinUI先 대비 유지),
  `README.md`, `AGENTS.md` (§1·§5·실행 명령) 경로·네임스페이스 갱신.
- 이력 문서(`docs/orders/2026101*.md`, `task_26*`)는 보존.
Verification: `dotnet build -c Release` 경고 0·오류 0,
  `dotnet test` 176/176 (Rtu 159 + Client.Wpf 14 + Slave 3).
Deviations from plan: none (명명 B 확정대로).
Notes:
- VS Code C# Dev Kit이 `obj`를 잠그고 있어 디렉터리 `git mv` 불가 →
  추적 파일 개별 `git mv`로 수행 (목적지 사전 생성 필요).
- `src/NovaModbus.Client/obj` 잔재 1건: 잠금 중이라 삭제 보류 (ignore 처리됨, 무해).
Approval: [ ] director approved (date)
