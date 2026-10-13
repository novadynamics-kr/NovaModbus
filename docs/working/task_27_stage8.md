# Task 27 Stage 8: Watch 영역 분리 (Border + 배경)
Changes:
- Watch 탭을 Border 섹션 2개로 분리: Entries / Results 각자
  `LayerFillColorDefaultBrush` 배경 + `CardStrokeColorDefaultBrush` 테두리
  (테마 대응 브러시, 하드코딩 없음). 내부 Grid (타이틀/헤더/목록)로 행 혼선 원천 차단.
- 검증 과정에서 발견된 추가 수정은 없음 (Stage 7 구조가 이미 정렬·스크롤 정상).
Verification: 30행 시드 + 가상 연결 + 전체읽기 실측 스크린샷 —
  섹션 분리·내부 스크롤(밴드 내 5행 표시)·값/시간/OK 표시·연결 상태 확인.
  `dotnet build` 경고 0·오류 0, `dotnet test` 207/207.
  임시 시드·진단·탭 강제선택 코드는 전부 원복됨 (diff 확인).
Deviations: 없음.
Notes:
- 보고된 "투명 겹침 + 휠 오동작"은 Stage 6 레이아웃(무헤더·무제한 수평 StackPanel)
  시점의 현상으로 판단됨. Stage 7+8 구조에서는 30행 실측상 분리·스크롤 정상.
  사용자 환경에서 재발 시 행 수·창 크기와 함께 스크린샷 요청.
- 휠 입력 자체는 헤드리스 검증 불가 (구조상 각 리스트 자체 스크롤).
Approval: 전체 stage 승인으로 갈음 (2026-10-13).
