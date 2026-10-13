# Task 27 Stage 5: WinUI 테스트 + 커버리지 + 최종 게이트
Changes:
- `tests/NovaModbus.Client.Tests/` (신규, `net10.0-windows10.0.26100.0` MSTest):
  ValueFormatter (7) + WatchEntry (4) + WatchListStore (3) +
  WatchViewModel (9: Add/중복/무효/삭제/전체선택/해제/Clear/SaveLoad/AddRange/ReadAll) +
  ReadWrite (3) + Polling/Settings (3) = 29개.
  MainViewModel은 제외 (DispatcherQueue가 UI 스레드 필수).
- `NovaModbus.slnx` 등록, `.gitignore` bin/obj (ignore 선행 — 재발 방지),
  `coverage.ps1` 4입력 병합.
  runsettings는 `Client.Tests` Exclude·`Client` Include가 개명 전부터 준비되어 있어 변경 없음.
- `README.md` (구조·커버리지 4어셈블리), `AGENTS.md` (§1 프로젝트 목록) 갱신.
Verification: `dotnet build` 경고 0·오류 0, `dotnet test` 205/205
  (159+14+29+3). `coverage.ps1` 종단 → HTML 4패키지
  (Rtu 86.5% / Client 19.5% / Client.Wpf 4.4% / Slave 1.2%), "Tests" 0건.
  WPF 기능 체크리스트 대조: 설정·Read/Write(18종 FC)·Polling·Watch·Log·가상슬레이브
  전 항목 이식됨 (드래그 선택만 단축키 동등 조작으로 대체).
  실행 확인은 지시자 몫 (`dotnet run --project src/NovaModbus.Client`).
Deviations: Stage 5에서 테스트 프로젝트 1개 추가 (계획의 "신규 앱용 테스트" 해당).
Approval: 전체 stage 승인으로 갈음 (2026-10-13).
