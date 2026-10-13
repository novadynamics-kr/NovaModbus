# Task 29: Native AOT (장기 로드맵)
Issue: #29
Goal: AOT 가능 대상 전부에 Native AOT 게시 지원.
Non-goals: "예외 없이 전부" (기술적으로 불가 영역 존재 — 아래 범위).

## 범위 제안 (착수 전 승인 필수)
| 대상 | 제안 | 근거 |
|------|------|------|
| `NovaModbus.Rtu` | trim/AOT-safe 감사 + 주석 | 라이브러리는 게시물이 아니라 호환성이 목표 |
| WinUI 3 앱 2종 (#27·#28) | `PublishAot=true` 게시 | WinUI 3는 AOT 공식 지원 |
| WPF 앱 2종 | **제외** | WPF NativeAOT 미지원 (XAML·리플렉션) |
| 테스트 3종 | **제외** | 테스트 호스트 AOT 불가 |

## Phases (4)
1. Rtu 감사: 리플렉션·동적 코드 전수 (`System.Text.Json`은 소스 생성기 전환 검토),
   트리밍 경고 기준선 확보.
2. WinUI 앱 1종(#27 산출물)에 `PublishAot` 적용 + 게시·실행 검증.
3. WinUI 앱 2종 + Rtu 잔여 경고 제거 (`TreatWarningsAsErrors` + `ILxxxx` 대응).
4. Gate: AOT 게시물 정상 실행 + 전체 `build`/`test` 녹색 + 문서화.

## 리스크
- WPF가 존속하는 한 "모든 프로젝트 AOT"는 성립 불가. WPF 제거 결정 시 본 계획 개정.

## 확정 사항 (director, 2026-10-13)
- **범위 확정**: Rtu 감사 + WinUI 2종만. WPF/테스트 제외.
- **순서**: Task 26 병합 → #27 → #28 → #29 (Rtu 감사만 단독 선행 가능).

## Quality gates
- AOT 게시·실행 / build 경고 0 / test 전체 통과.

## gh 기록
- Issue #29 생성 (2026-10-13).

Approval: [x] director approved (2026-10-13, 제안범위·순차착수)
