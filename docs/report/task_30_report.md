# Task 30 Report: WinForms NovaModbus.Client + WPF 개명
Issue: #30

## Plan vs Result
| Step | Plan | Result |
|------|------|--------|
| 1. 개명 | WPF → `ClientWPF` | 완료. 19+4 파일 + 설정·문서 6종. build 무경고·176/176. |
| 2. 스캐폴드 | WinForms 설정 패널 + 4탭 | 완료. NuGet 추가 없음. `#nullable disable` 관례. |
| 3. 설정·Read/Write | Options/Runner/Logger | 완료. Toolkit 미사용, 18종 FC 로직 동일. |
| 4. Polling·Watch·Log | DataGridView 2개 | 완료. CellFormatting·중첩 바인딩 대응, FileDialog. |
| 5. Gate | 테스트·커버리지·대조 | build 0/0, test 205/205, HTML 4패키지·Tests 0건. |

## WPF 기능 대조 (체크리스트)
- 설정 패널 (포트/보레이트/Parity/DataBits/Stop/가상·연결/해제/상태): ○
- Read/Write 18종 FC + 힌트 + 결과: ○ (파서 포함, 로직 동일)
- Polling (간격/시작/중지/이력): ○
- Watch (등록/범위/삭제/전체선택/해제/전체삭제/저장/불러오기/전체읽기/자동새로고침/진행률/상태): ○
- Log (TX/RX/INFO): ○
- 내장 가상 슬레이브 (`VIRTUAL:nova-test-slave`): ○
- 드래그 다중선택: △ (체크박스+Ctrl+A/Delete/Escape 동등 조작)

## Deltas
- 산출물 혼입 2회 정정 (VS Code 잔재 + `add -A` 경합) → 경로 지정 스테이징 규칙화,
  사멸 경로 방패 ignore.
- `NovaModbus.Client.WinForms.Tests` 1개 추가 (계획 내).
- AGENTS.md §1, README 구조·커버리지 갱신.

## Unresolved / Follow-ups
- 실행 확인 (`dotnet run --project src/NovaModbus.Client`) — 지시자.
- `git push nova main` (task 브랜치 절대 푸시 금지) + #30 close는 별도 승인 필요.

## gh 기록
- Issue #30 생성 + self-assign (2026-10-16). merge/close 미실시.
