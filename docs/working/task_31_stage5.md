# Task 31 Stage 5: WinForms 테스트 + 커버리지 + 최종 게이트
Changes:
- `tests/NovaModbus.Slave.WinForms.Tests/` (신규, `net10.0-windows` MSTest):
  RegisterRow (4) + CoilRow (3) + RegisterMapStore (3) + SlaveStore (12) +
  MainFormLayout (1) = 24개. Form·그리드 제외, 순수 로직만
  (레이아웃 회귀 1종은 `UseWindowsForms`).
- `NovaModbus.slnx` 등록, `coverage.ps1` 5입력 병합,
  runsettings Exclude에 Slave.WinForms.Tests 추가
  (Include `[NovaModbus.Slave]*`·`[NovaModbus.SlaveWPF]*`는 Stage 1 선행).
- `README.md` (구조·커버리지 5어셈블리), `AGENTS.md` (§1 프로젝트 목록) 갱신.
- `coverage.ps1` 파서 오류 수정: 배열 리터럴末尾 콤마 제거 (PS 5.1 미지원).
Verification: `dotnet build` 경고 0·오류 0, `dotnet test` 233/233
  (159+14+33+3+24). `coverage.ps1` 종단 → HTML 5패키지
  (Rtu 86.7% / Client 55.6% / ClientWPF 4.4% / Slave 62.5% / SlaveWPF 1.2%),
  "Tests" 0건.
  WPF 기능 체크리스트 대조: 설정·Start/Stop·멀티 ID·Holding/Coils 편집·
  범위 추가·삭제/전체선택·저장/불러오기·카운터·에러 주입·로그·Float 탭·
  우클릭 float 쓰기 전 항목 이식됨 (드래그 선택만 제외 — 네이티브 다중선택).
  실행 확인은 지시자 몫 (`dotnet run --project src/NovaModbus.Slave`).
Deviations: Stage 5에서 테스트 프로젝트 1개 추가 (계획 내 "신규 앱용 테스트" 해당).
Notes:
- MSTEST0065: 배열 비교는 `Assert.AreSequenceEqual` 사용 (analyzer가 오류로 강제).
Approval: 전체 승인으로 갈음 (2026-10-16).
