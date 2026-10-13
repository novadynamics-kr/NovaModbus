# Task 27: WinUI 3 NovaModbus.Client (장기 로드맵)
Issue: #27
Goal: WinUI 3 기반 Client 신규 앱. WPF Client와 기능 동등.
Non-goals: WPF Client 제거/교체 (병존). 커버리지 수치 게이트 (별도 태스크).

## 확정 사항 (director, 2026-10-13)
1. **명명 B (개명 + 계승)**: 기존 WPF → `NovaModbus.Client.Wpf` /
   `NovaModbus.Slave.Wpf`로 개명 (폴더·csproj·네임스페이스·AGENTS/README 전수 수정),
   신규 WinUI가 `NovaModbus.Client` / `NovaModbus.Slave` 계승.
2. **Hard Constraints 예외 승인**: Microsoft 유지·게시 NuGet 허용
   (`Microsoft.WindowsAppSDK`, `CommunityToolkit.*` 등). 서드파티 UI
   (`MahApps` 등)·Serial/Modbus 금지 유지. AGENTS.md §2 반영됨.
   MVVM 방식(수동 INPC vs Toolkit)은 구현 단계에서 확정.

## Phases (4)
1. 스캐폴드: WinUI 3 프로젝트 + Rtu 참조 + 빌드/실행 확인. TFM은
   `net10.0-windows10.0.26100` 이상, 패키징 방식(패키지/언패키지) 확정.
2. 설정·Read/Write 탭 이식: SerialSettings, 18종 FC, Raw Hex.
3. Polling·Watch·Log 탭 이식: 다중 선택/드래그 동작은 WinUI 3 컨트롤에 맞게 재구현
   (WPF `DataGridDragSelect` 직접 이식 불가 — WinUI 컨트롤 체계 상이).
4. Gate: WPF Client 대비 기능 체크리스트 대조 + `build` 무경고 +
   신규 앱용 테스트 프로젝트 (순수 로직) + 커버리지 편입.

## Quality gates
- build 경고 0 / WPF 기능 체크리스트 전 항목 대조 / 회귀 없음.

## gh 기록
- Issue #27 생성 (2026-10-13).

Approval: [x] director approved (2026-10-13, 명명B·제약예외·순차착수)
