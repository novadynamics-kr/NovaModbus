# Task 28: WinUI 3 NovaModbus.Slave (장기 로드맵)
Issue: #28
Goal: WinUI 3 기반 Slave 신규 앱. WPF Slave와 기능 동등 (멀티 ID 시뮬레이션 유지).
Non-goals: WPF Slave 제거/교체 (병존). 커버리지 수치 게이트 (별도 태스크).

## 확정 사항 (director, 2026-10-13)
1. **명명 B**: Task 27과 동일 (기존 WPF → `*.Wpf` 개명, 신규 WinUI가 기존명 계승).
2. **Hard Constraints 예외 승인** (Task 27과 공동): Microsoft 유지·게시 패키지 허용.
3. **순서**: Task 26 병합 후 #27 → #28 → #29 순차 착수.

## Phases (4)
1. 스캐폴드: Task 27 빌드 기반 재사용 + Rtu 참조 + 실행 확인.
2. 멀티 ID 관리·Holding/Coils 편집 이식: `SlaveId → DataStore` 구조 유지
   (AGENTS.md §2.4).
3. Float 쓰기·카운터·에러 주입·프레임 로그·맵 저장/불러오기 이식.
4. Gate: WPF Slave 대비 기능 체크리스트 대조 + `build` 무경고 +
   신규 앱용 테스트 프로젝트 (순수 로직) + 커버리지 편입.

## Quality gates
- build 경고 0 / WPF 기능 체크리스트 전 항목 대조 / 멀티 ID 라우팅 회귀 없음.

## gh 기록
- Issue #28 생성 (2026-10-13).

Approval: [x] director approved (2026-10-13, 명명B·제약예외·순차착수)
