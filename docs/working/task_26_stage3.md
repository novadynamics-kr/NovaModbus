# Task 26 Stage 3: runsettings Include 명시
Changes:
- `CodeCoverage.runsettings`: XPlat collector에 `<Include>[NovaModbus.Rtu]*,[NovaModbus.Client]*,[NovaModbus.Slave]*</Include>` 추가. `Exclude`를 3개 테스트 어셈블리로 확대 (`ExcludeByFile **/tests/**` 유지). Microsoft collector의 `*Tests.dll` 제외는 그대로.
Verification: Stage 4 종단 실행에서 3개 패키지 수집 확인으로 간접 검증 (단독 실행 시 기존 스크립트가 단일 Cobertura만 참조하므로 병합 스크립트와 함께 검증).
Deviations from plan: none.
Approval: [ ] director approved (date)
