# Task 3: Client 기능 UI + 가상 루프백(안 A)

Issue: #3

## Goal

실 하드웨어 없이 Client UI 전수 테스트가 가능하게 한다. FC 읽기/쓰기,
주기 폴링, Raw Hex 뷰어, 로그 + 동일 프로세스 가상 루프백.

## Non-goals

- 프로세스 간 가상 COM (Named Pipe)
- Slave.exe UI (후속 Task)
- 실물 COM 검증 (하드웨어 단계)

## Steps (5)

1. **VirtualSerialBus + transport 팩토리** — 이름付き 인메모리 pair 허브,
   `Serial`/`Virtual` 선택 추상화, 단위테스트.
2. **연결 관리** — 포트 선택(COM/VIRTUAL) → 연결/해제, MainViewModel 배선,
   상태 표시.
3. **FC 읽기/쓰기 탭** — 8종 요청/응답 UI + ViewModel, 가상 루프백 동작 확인.
4. **폴링 + Raw Hex + 로그** — 주기 폴링(중복 실행 가드), 송수신 Hex 뷰어,
   통신 로그 ListBox.
5. **내장 가상 슬레이브 + 게이트** — 데모 토글, build/test/회귀, 최종 보고.

## Quality gates

build / test / spec-vector / regression (§5 rules/hyper-waterfall.md).

## Approval

- [ ] director approved (date)
