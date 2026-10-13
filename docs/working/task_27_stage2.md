# Task 27 Stage 2: WinUI 3 Client 스캐폴드 (언패키지)
Changes:
- `src/NovaModbus.Client/NovaModbus.Client.csproj` (신규): `net10.0-windows10.0.26100.0`,
  `UseWinUI`, `WindowsPackageType=None` (auto-initializer가 부트스트랩 처리),
  `WindowsAppSDKSelfContained=true` + `RuntimeIdentifier=win-x64` (self-contained 필수),
  Rtu 참조. NuGet: `Microsoft.WindowsAppSDK` 2.5.1 (MS 관리 패키지, 승인済).
- `App.xaml(.cs)` (Fluent 리소스 + OnLaunched), `MainWindow.xaml(.cs)` (placeholder).
- `NovaModbus.slnx`: 신규 프로젝트 등록.
Verification: `dotnet build` (프로젝트·솔루션) 경고 0·오류 0,
  `dotnet test` 176/176 회귀 없음.
  실행 확인은 생략 (GUI 팝업 — 지시자가 `dotnet run --project src/NovaModbus.Client`로 확인).
Deviations from plan: none (언패키지 확정대로).
Notes:
- Self-contained 빌드는 RID 필수 (`WindowsAppSDKSelfContained requires a supported
  Windows architecture` → `win-x64` 고정으로 해결).
- 신규 프로젝트 bin/obj가 `.gitignore`에 없어 산출물 혼입 → 적발 후 ignore 2줄 추가·
  `rm --cached`·amend 정정 (Stage 1과 동일 실수, 재발 방지: 신규 프로젝트 생성 시
  ignore 먼저).
- MVVM 방식(수동 INPC vs Toolkit)은 탭 이식 단계에서 확정.
Approval: [ ] director approved (date)
