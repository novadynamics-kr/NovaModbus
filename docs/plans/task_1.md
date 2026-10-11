# Task 1: Transport 수신 루프 + ModbusRtuClient/Slave 본구현

Issue: #1

## Goal

`ISerialTransport` 위에서 RTU 요청/응답 트랜잭션을 완성한다.
`ModbusRtuClient`(FC 01,02,03,04,05,06,15,16)와 `ModbusRtuSlave`
(멀티 ID 디스패치 + Exception 응답)를 2단계 코덱·DataStore와 연결해
동작하는 상태로 만든다.

## Non-goals

- WPF UI (후속 단계)
- FC 08/11/17/22/23 (2차)
- 실물 COM포트 검증 (`[TestCategory("Hardware")]`로 분리, 기본 제외)

## Steps (5)

1. **RtuFramer 수신 루프** — 3.5char silent interval 기반 프레임 분리
   (Baudrate 연동, 시간 추상화로 단위테스트 가능하게).
2. **트랜잭션 코어 + FakeSerialTransport** — 요청 전송→응답 대기→CRC·
   SlaveId·Function 검증 공통 경로, 테스트용 Fake transport.
3. **ModbusRtuClient 본구현** — FC 8종 + timeout/retry +
   CancellationToken, Fake 기반 round-trip 테스트.
4. **ModbusRtuSlave 본구현** — SlaveId 필터, DataStore 연동, Exception
   응답, IgnoreCrcErrors 유지, 멀티 ID 테스트.
5. **Client<->Slave 루프백 + 회귀 게이트** — Fake 양방향 연결 통합 테스트,
   build 무경고·전체 테스트·스펙 벡터 대조, 최종 보고.

## Quality gates

build / test / spec-vector / regression (§5 rules/hyper-waterfall.md).

## Approval

- [ ] director approved (date)
