# Task 4: 스레드 안전 리팩토링 + 오류 처리

Issue: #4

## Goal

Client 전 경로 스레드 안전 확보. `CanExecuteChanged`·콜백 마샬,
회귀 테스트, 규칙 문서화.

## Non-goals

- Slave.exe UI, 실물 COM 검증

## Steps (3)

1. **Commands 이동 + 마샬** — Rtu `Commands/`로 이동, 생성 시점
   `SynchronizationContext` 캡처, 이벤트·에러콜백 마샬, `CommandTests`.
2. **Client 마이그레이션 + 감사** — 네임스페이스 전환, 전 경로 점검
   (INPC 안전 / 컬렉션 UI 어피니티 / logger 마샬 확인).
3. **규칙 + 게이트 + 보고** — 스레드 규칙 문서 반영, 트러블슈팅 기록,
   전 관문, 최종 보고, merge·push·close.

## Quality gates

build / test / spec-vector / regression.

## Approval

- [x] director approved — blanket (auto-proceed + termination), 2026-10-11
