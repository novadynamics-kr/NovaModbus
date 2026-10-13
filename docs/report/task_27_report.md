# Task 27 Report: WinUI 3 NovaModbus.Client
Issue: #27

## Plan vs Result
| Step | Plan | Result |
|------|------|--------|
| 1. 개명 | 기존 → `*.Wpf` | 완료. Client 19+4 파일, 설정·문서 6종. VS Code obj 잠금 → 추적파일 개별 `git mv` (보고서 기록). `src/NovaModbus.Client/obj` 잔재는 잠금 해제 후 제거됨. |
| 2. 스캐폴드 | WinUI unpackaged | 완료. WindowsAppSDK 2.5.1, `WindowsPackageType=None` + self-contained + `win-x64` (RID 필수). build 무경고. |
| 3. 설정·Read/Write | Toolkit 이식 | 완료. MVVM=Toolkit 확정 (전체 stage 승인 시). partial properties (MVVMTK0045). UI-affinity await, DispatcherQueue 마샬. |
| 4. Polling·Watch·Log | 전 탭 이식 | 완료. ListView (DataGrid 미사용), KeyboardAccelerator, FilePicker+HWND, `Progress{T}` 마샬. 드래그 선택만 제외 (단축키 동등). |
| 5. Gate | build/test/리포트 | build 0/0, test 205/205, HTML 4패키지·Tests 0건. 실행 확인은 지시자 몫. |

## WPF 기능 대조 (체크리스트)
- 설정 패널 (포트/보레이트/Parity/DataBits/Stop/가상·연결/해제/상태): ○
- Read/Write 18종 FC + 힌트 + 결과: ○ (로직 동일, 파서 포함)
- Polling (간격/시작/중지/이력): ○
- Watch (등록/범위/삭제/전체선택/해제/전체삭제/저장/불러오기/전체읽기/자동새로고침/진행률/상태): ○
- Log (TX/RX/INFO): ○
- 내장 가상 슬레이브 (`VIRTUAL:nova-test-slave`): ○
- 드래그 다중선택: △ (체크박스+Ctrl+A/Delete/Escape 동등 조작)

## Deltas
- Stage 1·2에서 `git add -A` 산출물 혼입 2회 → 모두 적발·정정 (ignore 선행 규칙화).
- `NovaModbus.Client.Tests` 프로젝트 1개 추가 (계획 내 "신규 앱용 테스트").
- AGENTS.md §1·§2 개정 (프로젝트 목록, Toolkit 확정).

## Unresolved / Follow-ups
- 실행 확인 (`dotnet run --project src/NovaModbus.Client`) — 지시자.
- `git push nova main` (task 브랜치 절대 푸시 금지) + #27 close는 별도 승인 필요.

## gh 기록
- #27 OPEN (self-assign은 #26 때와 달리 미실시 — 필요 시 보완).
