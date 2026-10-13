# Task 26 Stage 4: 다중 Cobertura 병합 + 문서화
Changes:
- `coverage.ps1`: 3개 테스트 프로젝트의 최신 Cobertura를 수집해 `;`로 연결, ReportGenerator에 병합 입력. (Microsoft `.coverage` 이중 수집 유지.)
- `README.md`: 구조 블록에 신규 테스트 프로젝트 2개 추가, 커버리지 절을 3개 제품 어셈블리 기준으로 갱신.
- `AGENTS.md` §1: 테스트 프로젝트 목록 갱신 (규칙 파일의 구조 기술 정확도 유지).
Verification: `coverage.ps1` 종단 실행 — 176/176 통과 → `coverage-report/index.html`에 `NovaModbus.Client`(line 4.4%)·`NovaModbus.Rtu`(86.5%)·`NovaModbus.Slave`(1.2%) 3패키지, "Tests" 0건.
Deviations from plan: none.
Approval: [ ] director approved (date)
