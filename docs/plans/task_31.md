# Task 31: WinForms NovaModbus.Slave + WPF 개명
Issue: #31
Goal: WPF Slave → `SlaveWPF` 개명 후 .NET 10 WinForms `NovaModbus.Slave` 신규 제작 (WPF 동등 기능).
Non-goals: WPF 제거/교체 (병존). 커버리지 수치 게이트.

## 확정 사항 (지시자, 2026-10-16)
- 개명: `src/NovaModbus.Slave` → `src/NovaModbus.SlaveWPF` (점 없음).
- 신규: `src/NovaModbus.Slave` (WinForms, `net10.0-windows` + `UseWindowsForms`).
- 새 NuGet 없음. WPF 코드 공유 금지 (AGENTS.md §2.2).
- Client 교훈 선적용 (Task 30 Stage 6·7): Designer `#nullable disable`,
  `Labeled` 명시 크기, VirtualMode + 네이티브 다중선택, 선택 체크박스 금지,
  셀 값 문자열 반환, `GetRowCount(Selected)`, `_syncingGrids` 억제,
  O(n) 인덱스 삭제, 레이아웃 회귀 테스트, 경로 지정 스테이징.

## Steps (5)
1. 개명: WPF Slave 폴더·csproj·네임스페이스 → `SlaveWPF`.
   테스트·slnx·커버리지 배선·문서 동반 갱신. Gate: build 무경고·test 전체.
2. WinForms 스캐폴드: `Program.cs` + `MainForm` (좌측 설정 + 탭 5종 뼈대:
   Holding/Coils/Counters/Log/Float) + Rtu 참조. Gate: build 무경고·실행 확인.
3. 리스너·ID·편집기: SerialSettings + Start/Stop + 멀티 ID + 에러 주입 +
   Holding/Coils VirtualMode 편집 그리드 (`CellValueNeeded`/`CellValuePushed`,
   Holding 값 편집, Coil On/Off 체크박스=도메인 데이터).
4. 부가 탭·벌크: Counters 타이머 + Log 바인딩 + Float 탭 + 우클릭 float 다이얼로그 +
   범위 추가 (진행률) + 삭제/전체선택/저장/불러오기 (WinForms 다이얼로그).
5. Gate: 테스트 (신규 순수 로직) + 커버리지 편입 + WPF 기능 체크리스트 대조 +
   최종 리포트. merge·push·close는 별도 승인.

## Quality gates
- build 경고 0·오류 0 / test 전체 통과·회귀 없음 / WPF 기능 대조 / `*Tests` 0건.

## gh 기록
- Issue #31 생성 + self-assign (2026-10-16).

Approval: [x] director approved (2026-10-16, 착수 + 개명·신설 + IsSelected 유지)
