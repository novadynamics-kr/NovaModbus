# Task 31 Report: WinForms NovaModbus.Slave + WPF 개명
Issue: #31

## Plan vs Result
| Step | Plan | Result |
|------|------|--------|
| 1. 개명 | WPF → `SlaveWPF` | 완료. 17+2 파일 + 설정·문서 6종. build 무경고·209/209. |
| 2. 스캐폴드 | WinForms 설정 패널 + 5탭 | 완료. NuGet 추가 없음. `#nullable disable` 관례. |
| 3. 리스너·ID·편집기 | Options/Store/가상 편집 그리드 | 완료. `CellValuePushed` 편집, 네이티브 선택. |
| 4. 부가 탭·벌크 | 범위·저장·카운터·로그·Float | 완료. 모달 float 다이얼로그, 진행률 타이머. |
| 5. Gate | 테스트·커버리지·대조 | build 0/0, test 233/233, HTML 5패키지·Tests 0건. |

## WPF 기능 대조 (체크리스트)
- 설정 패널 (포트/보레이트/Parity/DataBits/Stop/Start/Stop/상태): ○
- 멀티 ID (추가·삭제·선택, 정렬): ○
- 에러 주입 (CRC 무시·Busy, 라이브 적용): ○
- Holding 편집 (주소/값 추가·범위·삭제/전체선택/해제·값 편집·16진 표시): ○
- Coils 편집 (추가·범위·삭제/전체선택/해제·On/Off 체크): ○
- 저장/불러오기 (ID별 JSON 맵): ○
- 카운터 (1s 갱신, ID별+버스): ○
- 로그 (TX/RX/INFO): ○
- Float 탭 + 우클릭 float 쓰기: ○
- 드래그 다중선택: △ (네이티브 다중선택+단축키 동등 조작)

## Deltas
- `CoilRow.AddressHex` 추가 (WPF에 없음, Holding 그리드와 일관).
- `RegisterRow`/`CoilRow`의 `IsSelected` 유지 (지시자 결정, UI 미사용).
- `coverage.ps1`末尾 콤마 파서 오류 수정 (PS 5.1).
- AGENTS.md §1, README 구조·커버리지 갱신.

## Unresolved / Follow-ups
- 없음. 지시자 실측 확인 완료 (실행 스모크: 설정·툴바·그리드 전부 표시).
- 병합 `1dbed54` + 후속 기록 커밋, gate (build 무경고·233/233) 후
  `git push nova main`, #31 close. 로컬 task 브랜치 삭제.

## gh 기록
- Issue #31 생성 + self-assign (2026-10-16). merge/close 미실시.
