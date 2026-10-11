# Task 3 Stage 3: FC 읽기/쓰기 탭

Issue: #3

## Changes

- `Client/ViewModels/ReadWriteViewModel.cs` (신규) — 8종 FC 실행,
  입력 파싱(slave/address/count/values, 0x hex·0/1·콤팩트 지원),
  결과 포맷, live client provider 주입.
- `MainViewModel` — `ReadWrite` 노출.
- `MainWindow` — 우측 TabControl Read/Write 탭 (실행·결과 Consolas).

## Verification

- build 경고 0, test 75/75.
- 탭 동작은 director 실행 확인 필요 (가상 슬레이브 연결 후 Execute).

## Deviations

- `Visibility` 대신 `IsEnabled`로 필드 on/off (bool 바인딩, 컨버터 불필요).

## Approval

- [x] director approved (auto — task 3 blanket approval 2026-10-11)
