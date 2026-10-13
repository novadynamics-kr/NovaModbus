# Task 27 Stage 7: Watch 겹침 해소 + Baud 표시 (실측 기반)
Changes:
- Watch 탭 재구성: `Entries (n)` / `Results (n)` 섹션 헤더 + 컬럼 헤더 행,
  항목 StackPanel → Grid 컬럼 정렬, 템플릿 CheckBox 제거 (네이티브 다중선택
  체크박스와 중복), ListView 수평 스크롤 비활성 (내용 넘침 방지),
  행 비율 2*/3*.
- `TabViewItem IsClosable="False"` 전 탭 (WPF처럼 고정 탭), 좌측 컬럼 270→300
  (가상 포트명 잘림 해소).
- Baud Rate 표시 수정: WinUI 편집 ComboBox가 `Text` 바인딩 초기값을 렌더하지 않음
  (소스는 정상 — 진단 TextBlock으로 확인). `BaudRatePreset` 브리지 추가
  (`SelectedItem`이 표시 담당, null 대입 무시) + 테스트 1개.
Verification: 실측 스크린샷 4회 (빈 목록/3행/30행/수정 후) —
  섹션 분리·정렬·단일 체크박스·Baud "9600" 표시 확인.
  `dotnet build` 경고 0·오류 0, `dotnet test` 207/207.
  임시 시드·진단·탭 강제선택 코드는 전부 원복됨.
Deviations: 없음.
Notes:
- 증분 XAML 코드생성 flakiness 1회 (동일 소스 재빌드로 해소, WMC9999 연쇄).
Approval: 전체 stage 승인으로 갈음 (2026-10-13).
