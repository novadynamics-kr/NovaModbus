# Task 5: Client Watch 화면

Issue: #5

## Goal

(Slave ID, 영역, 주소) 목록을 등록하면 일괄 읽어 값을 전시하는 Watch 탭.

## Non-goals

- 쓰기, Slave.exe UI, 실물 COM 검증

## Steps (4)

1. **Watch 항목 + ViewModel** — 모델(SlaveId·Area·Start·Count),
   추가/수정/삭제, 결과 바인딩 (값·시간·상태).
2. **Read All 실행** — live client 일괄 읽기, 행별 에러 표시.
3. **자동 새로고침** — 폴링 패턴 재사용 토글.
4. **게이트 + 보고** — build/test/회귀, 최종 보고, merge·push·close.

## Quality gates

build / test / spec-vector / regression (§5 rules/hyper-waterfall.md).

## Approval

- [ ] director approved (date)
