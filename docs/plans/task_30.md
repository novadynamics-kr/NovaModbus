# Task 30: WinForms NovaModbus.Client + WPF 개명
Issue: #30
Goal: WPF Client → `ClientWPF` 개명 후 .NET 10 WinForms `NovaModbus.Client` 신규 제작 (WPF 동등 기능).
Non-goals: WPF 제거/교체 (병존). WinUI 재시도. 커버리지 수치 게이트.

## 확정 사항 (지시자, 2026-10-16)
- 개명: `src/NovaModbus.Client` → `src/NovaModbus.ClientWPF` (점 없음).
- 신규: `src/NovaModbus.Client` (WinForms, `net10.0-windows` + `UseWindowsForms`).
- 새 NuGet 없음 (WinForms 프레임워크 내장). WPF 코드 공유 금지 (AGENTS.md §2.2).

## Steps (5)
1. 개명: WPF Client 폴더·csproj·네임스페이스 → `ClientWPF`.
   테스트· slnx·커버리지 배선·문서 동반 갱신. Gate: build 무경고·test 전체.
2. WinForms 스캐폴드: `Program.cs` + `MainForm` (좌측 설정 + TabControl 4탭 뼈대) +
   Rtu 참조 + 내장 가상 슬레이브. Gate: build 무경고·실행 확인.
3. 설정·Read/Write 이식: SerialSettings + 18종 FC 실행 + 결과 뷰어.
   스레딩은 `Control.Invoke` 마샬 (Rtu Commands 활용).
4. Polling·Log·Watch 이식: `DataGridView` 2개 (체크박스 컬럼 + 네이티브 다중선택),
   Ctrl+A/Delete 단축키, 저장/불러오기 (WinForms FileDialog), 진행률·상태.
5. Gate: 테스트 (신규 순수 로직) + 커버리지 편입 + WPF 기능 체크리스트 대조 +
   최종 리포트. merge·push·close는 별도 승인.

## Quality gates
- build 경고 0·오류 0 / test 전체 통과·회귀 없음 / WPF 기능 대조 / `*Tests` 0건.

## gh 기록
- Issue #30 생성 + self-assign (2026-10-16).

Approval: [x] director approved (2026-10-16, 착수 + 개명·신설)
