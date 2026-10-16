# Task 31 Stage 4: 범위·저장·카운터·로그·Float 이식
Changes:
- `MainForm(.cs)`: `AddHoldingRangeAsync`/`AddCoilRangeAsync` + `RunWithProgress`
  (200ms 타이머 → 진행률바), WinForms Save/Load 다이얼로그, 1s 카운터 타이머
  (`BuildCountersText`), 로그 바인딩, Float 탭 (주소·값·워드오더·쓰기),
  Holding 우클릭 → `FloatInputDialog` (모달, 주소·값·오더).
- `FloatInputDialog.cs` (신규): XAML 다이얼로그의 WinForms 대체.
  값 파싱 실패 시 경고 + `DialogResult.None`으로 유지.
- `MainForm.Designer.cs`: Holding 툴바에 Add Range/Save/Load,
  Coils 툴바에 Add Range, Counters/Log/Float 페이지 구현.
Verification: `dotnet build` 경고 0·오류 0.
  실행 확인은 지시자 몫 (`dotnet run --project src/NovaModbus.Slave`).
Deviations: 없음.
Notes:
- `FloatOrderOption.Display`를 콤보 `DisplayMember`로 사용 (WPF `DisplayMemberPath` 대응).
- 실측 스모크: 설정 패널·Holding 툴바·그리드 헤더 전부 표시.
  `Labeled` 패널이 라벨보다 좁아 잘림 발견 → 패널 폭을
  `max(입력, TextRenderer.MeasureText)`로 수정 후 재확인.
Approval: 전체 승인으로 갈음 (2026-10-16).
