# Task 29 Report: Native AOT (Rtu 감사로 축소 완료)
Issue: #29

## Plan vs Result
| Phase | Plan (2026-10-13) | Result (2026-10-16) |
|-------|-------------------|---------------------|
| 1. Rtu 감사 | 리플렉션 전수 + 트리밍 경고 기준선 | 완료. 위험 API 0건, `IsTrimmable` 상시 게이트. |
| 2·3. WinUI 2종 `PublishAot` | #27·#28 산출물에 적용 | **Void**: #27 FAILED, #28 CANCELLED. 대상 부재. |
| 4. Gate | AOT 게시물 실행 + build/test 녹색 | 대체 게이트: 트림 경고 0 + build 무경고·233/233. |

## 제외 확정 (불변)
- WPF 2종·WinForms 2종: 프레임워크가 NativeAOT 미지원 (+ WinForms 앱의 STJ 리플렉션 직렬화).
- 테스트 5종: 테스트 호스트 AOT 불가.

## 전 프로젝트 적용 가능성 실측 (2026-10-16, SDK 10.0.400)
| 프로젝트 | 결과 |
|---|---|
| `NovaModbus.Rtu` | ✅ `IsTrimmable` clean (유일 적용 대상·완료) |
| WPF 2종 | ❌ `NETSDK1168` (트리밍 사용 시 WPF 미지원, 게시 차단) |
| WinForms 2종 | ❌ `NETSDK1175` (트리밍 사용 시 WinForms 미지원, 게시 차단) |
| 테스트 5종 | ❌ 컴파일은 성공하나 실행 불가 (게시물 내 `testhost.exe` 동적 로딩 구조) |

## Deltas
- `NovaModbus.Rtu.csproj`에 `IsTrimmable` 1줄 (유일 코드 변경).
- `docs/plans/task_29.md` Replan 섹션.

## Unresolved / Follow-ups
- WinUI 재시도 또는 WPF 제거 결정 시 본 계획 개정 (리스크 항목 유지).
- `git push nova main` (task 브랜치 절대 푸시 금지) + #29 close는 별도 승인 필요.

## gh 기록
- Issue #29 self-assign (2026-10-16). merge/close 미실시.
