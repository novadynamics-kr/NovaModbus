# Task 5 Impl Plan: 파일별 변경 계획

Parent: `docs/plans/task_5.md` (blanket approved).

## Stage 1 — Watch 모델 + ViewModel

- `Client/ViewModels/WatchEntry.cs`? — 1 public 타입/파일 규칙:
  `WatchArea` enum + `WatchEntry` (INPC 아님, 불변 레코드) + 결과 holder
  `WatchResult`는 `WatchViewModel.cs`와 분리 파일.
  - `Client/ViewModels/WatchArea.cs` — Holding/Input/Coils/Discrete.
  - `Client/ViewModels/WatchEntry.cs` — record (SlaveId, Area, Start, Count).
  - `Client/ViewModels/WatchResult.cs` — 행 표시 모델 (INPC: ValuesText,
    Timestamp, Status).
- `Client/ViewModels/WatchViewModel.cs` — entries/results ObservableCollection,
  Add/Remove 커맨드, 입력 필드 (SlaveId/Area/Start/Count 텍스트).

## Stage 2 — Read All 실행

- `WatchViewModel.ReadAllAsync` — live client provider 주입 (ReadWrite와 동일
  패턴), 영역별 client 호출, 행별 성공/에러 기록.
- 결과 포맷은 ReadWriteViewModel 포맷 재사용 (internal static 승격 또는 중복?
  → `ValueFormatter` static으로 `Services`에 분리, 둘 다 사용).

## Stage 3 — 자동 새로고침 + 화면

- PollingViewModel 패턴 재사용 (PollingViewModel 자체를 목표 VM으로 재사용?
  Polling은 ReadWrite 전용 → Watch용 경량 타이머를 WatchViewModel 내장).
- MainWindow 4번째 탭 "Watch": 등록 그리드 + 결과 그리드 + Read All +
  자동 새로고침(간격+토글).

## Stage 4 — 게이트 + 보고

- build/test/회귀, ViewModel 단위테스트 불가(Windows 소속) → 수동 확인 항목,
  최종 보고.
