# Task 10: Slave float32 입력 UI

Issue: #10

## Goal

특정 주소에 float32 쓰기 (오더 4종 선택).

## Non-goals

- float 읽기 표시, float64

## Steps (3)

1. **Float32Codec** — 4 오더 인코딩/디코딩 + 테스트 (1.0 벡터).
2. **Slave Float 탭** — 주소·값·오더 + 쓰기 + 상태.
3. **게이트 + 보고** — 관문·보고·merge·push·close.

## Quality gates

build / test / regression.

## Approval

- [ ] director approved (date)
