# 마인크래프트 기능 목록

이 문서는 프로젝트의 마인크래프트 스타일 기능에 대한 클라이언트, 서버 및 프로토콜 요구 사항을 요약합니다.

## 핵심 게임플레이
| 기능 | 클라이언트 (Client) | 서버 (Server) | 프로토콜 (Protocol) | 상태 |
|---|---|---|---|---|
| 플레이어 이동 | 이동 입력 및 예측 | 권위 있는 이동 유효성 검사 | `MoveRequest`, `PositionUpdate` | 구현됨 |
| 블록 상호작용 | 블록 파괴/설치 액션 트리거 | 블록 변경 승인 및 전파 | `BlockChangeRequest`, `BlockChangeBroadcast` | 구현됨 |
| 아이템 드랍 | 드랍된 아이템 렌더링 | 아이템 드랍 생성 및 관리 | `ItemDropBroadcast` | 구현됨 |

## 월드 및 환경
| 기능 | 클라이언트 (Client) | 서버 (Server) | 프로토콜 (Protocol) | 상태 |
|---|---|---|---|---|
| 청크 스트리밍 | 청크 요청 및 메시 생성 | 청크 데이터 제공 및 캐싱 | `ChunkRequest`, `ChunkResponse` | 구현됨 |
| 월드 생성 | (서버 데이터 시각화) | 절차적 지형 생성 (동굴, 강, 호수 등) | (청크 데이터) | 향상됨 |
| 시간 및 날씨 | 조명 및 효과 업데이트 | 시간 흐름 및 날씨 상태 시뮬레이션 | `TimeUpdateBroadcast`, `WeatherUpdateBroadcast` | 구현됨 |
| 구조물 생성 | (서버 데이터 시각화) | 나무, 던전 등 구조물 생성 | (청크 데이터) | 진행 중 |

## 플레이어 및 아이템
| 기능 | 클라이언트 (Client) | 서버 (Server) | 프로토콜 (Protocol) | 상태 |
|---|---|---|---|---|
| 인벤토리 관리 | 인벤토리 UI 및 상호작용 | 인벤토리 상태 지속 및 동기화 | `InventoryUpdateRequest`, `InventorySnapshot` | 진행 중 |
| 제작 | 제작 UI 및 레시피 확인 | 레시피 유효성 검사 및 아이템 생성 | `CraftingRequest`, `CraftingResult` | 진행 중 |
| 전투 | 공격 애니메이션 및 피드백 | 피해 계산 및 상태 업데이트 | `CombatEvent`, `DamageRequest` | 진행 중 |
| 죽음 및 리스폰 | 사망 화면 및 리스폰 UI | 플레이어 상태 리셋 및 리스폰 위치 관리 | `PlayerDeathBroadcast`, `PlayerRespawnBroadcast` | 진행 중 |

## 네트워킹 및 서버
| 기능 | 클라이언트 (Client) | 서버 (Server) | 프로토콜 (Protocol) | 상태 |
|---|---|---|---|---|
| 인증 및 세션 | 로그인 UI 및 세션 토큰 관리 | 자격 증명 유효성 검사 및 세션 추적 | `LoginRequest`, `LoginResponse`, `SessionHeartbeat` | 구현됨 |
| 데이터 지속성 | (서버에 의존) | 월드, 플레이어 데이터 저장 및 로드 | (내부 처리) | 구현됨 |
| 서버 원격 분석 | HUD 및 메뉴에 메트릭 표시 | 성능 및 사용량 메트릭 집계 | `ServerStatusRequest`, `ServerStatusResponse` | 진행 중 |

## UI 및 클라이언트
| 기능 | 클라이언트 (Client) | 서버 (Server) | 프로토콜 (Protocol) | 상태 |
|---|---|---|---|---|
| HUD | 플레이어 상태, 핫바, 서버 정보 표시 | (데이터 제공) | (다양한 브로드캐스트) | 구현됨 |
| 메뉴 시스템 | 메인 메뉴, 설정, 일시정지 메뉴 | (설정 동기화) | `PlayerSettingsUpdate` | 계획됨 |
| 채팅 | 채팅 메시지 입력 및 표시 | 채팅 메시지 릴레이 및 관리 | `ChatMessage` | 구현됨 |
