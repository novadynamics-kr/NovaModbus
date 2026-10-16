# Task 30 Stage 5: WinForms 테스트 + 커버리지 + 최종 게이트
Changes:
- `tests/NovaModbus.Client.WinForms.Tests/` (신규, `net10.0-windows` MSTest):
  ValueFormatter (7) + WatchEntry (4) + WatchListStore (3) +
  OptionsRunner (6) + WatchList (9) = 29개. Form·그리드 제외, 순수 로직만.
- `NovaModbus.slnx` 등록, `coverage.ps1` 4입력 병합 (기존 3 + 신규 1),
  runsettings Exclude에 WinForms.Tests 추가
  (Include `[NovaModbus.Client]*`는 WinForms 선행 준비되어 있어 변경 없음).
- `README.md` (구조·커버리지 4어셈블리), `AGENTS.md` (§1 프로젝트 목록) 갱신.
Verification: `dotnet build` 경고 0·오류 0, `dotnet test` 205/205
  (159+14+29+3). `coverage.ps1` 종단 → HTML 4패키지
  (Rtu 86.5% / Client 24.7% / ClientWPF 4.4% / Slave 1.2%), "Tests" 0건.
  WPF 기능 체크리스트 대조: 설정·Read/Write(18종 FC)·Polling·Watch·Log·가상슬레이브
  전 항목 이식됨 (드래그 선택만 제외 — 체크박스+단축키 동등 조작).
  실행 확인은 지시자 몫 (`dotnet run --project src/NovaModbus.Client`).
Deviations: Stage 5에서 테스트 프로젝트 1개 추가 (계획 내 "신규 앱용 테스트" 해당).
Approval: 전체 승인으로 갈음 (2026-10-16).
