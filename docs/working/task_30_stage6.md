# Task 30 Stage 6: Read/Write 미표시 수정 (레이아웃 회귀 테스트 포함)
Changes:
- `MainForm.Designer.cs` `Labeled()`: `AutoSize` Panel + `Dock` 자식 구조가
  폭 0으로 붕괴 (헤드리스 재현: `pref=0x38`) → 명시 크기+위치로 교체.
  Polling/Watch 상단부의 동일 패턴도 함께 해소.
- `tests/.../MainFormLayoutTests.cs` (신규, 상시 회귀): 헤드리스 폼 생성 후
  주요 입력 컨트롤 nonzero 단언. TabControl 페이지는 헤드리스에서 크기가
  없으므로 테스트에서 명시 지정 (런타임 한계가 아닌 테스트 환경 한계).
Verification: `dotnet build` 경고 0·오류 0, `dotnet test` 206/206.
  실측 스크린샷: Function/Slave/Start/Count/Execute/힌트/입력/결과창 전부 표시.
Deviations: 없음 (버그 수정 스테이지 추가).
Notes:
- 구 인스턴스 다중 실행이 진단을 오염시킴 (스샷≠최신 빌드). 이후 실행 검증은
  단일 인스턴스 확인 후 (`Get-Process`, Show+Spy 일치) 진행.
- WinForms `TabPage` 자식 핸들은 선택 탭만 생성 (정상 동작, 결함 아님).
Approval: 전체 승인으로 갈음 (2026-10-16).
