# Minetest to Unity3D Porting Task List

## Overview
이 문서는 Minetest 서브모듈 프로젝트를 Unity3D 엔진의 C# 스크립트로 포팅하기 위한 작업 리스트입니다.

## 1. Core Infrastructure

### 1.1 Networking Layer
- [ ] Protocol Buffer 메시지 직렬화/역직렬화
- [ ] TCP 클라이언트/서버 연결 관리
- [ ] 세션 관리 (SessionManager)
- [ ] 패킷 핸들러 등록 시스템

### 1.2 World Generation
- [x] 청크 기반 월드 생성
- [x] Perlin Noise 기반 지형 생성
- [x] 동굴 생성 시스템
- [x] 수계 시스템 (강, 호수)
- [ ] 생물군계 (Biome) 시스템

### 1.3 Block System
- [x] 블록 타입 정의 (JSON 데이터 드리븐)
- [x] 블록 배치/파괴 처리
- [ ] 블록 물리 충돌
- [ ] 블록 상태 (Tile Entity)

## 2. Game Systems

### 2.1 Player System
- [x] 플레이어 이동 처리
- [x] 플레이어 상태 관리 (HP, Hunger 등)
- [x] 인벤토리 시스템
- [ ] 장비 시스템
- [ ] 스킬 시스템

### 2.2 Combat System
- [x] 플레이어 공격 처리
- [x] 몬스터 AI 시스템
- [x] 데미지 계산
- [ ] PvP 시스템

### 2.3 Crafting System
- [x] 조합 레시피 (JSON 데이터 드리븐)
- [x] 조합 처리 핸들러
- [ ] 조합 UI

### 2.4 Item System
- [x] 아이템 정의 (JSON 데이터 드리븐)
- [x] 아이템 드롭/픽업
- [ ] 아이템 사용 효과

## 3. Unity Client

### 3.1 Rendering
- [x] 청크 메시 생성
- [x] 블록 텍스처 적용
- [ ] LOD 시스템
- [ ] 조명 시스템

### 3.2 UI System
- [ ] 메인 메뉴
- [ ] 인벤토리 UI
- [ ] HUD (HP, Hunger 등)
- [ ] 채팅 시스템

### 3.3 Input System
- [x] 키보드/마우스 입력
- [ ] 컨트롤러 지원
- [ ] 입력 리매핑

## 4. Server Features

### 4.1 Multiplayer
- [x] 멀티플레이어 세션 관리
- [x] 청크 동기화
- [x] 플레이어 상태 브로드캐스트
- [ ] 서버 리스트

### 4.2 Persistence
- [x] SQLite 데이터베이스
- [x] 플레이어 데이터 저장/로드
- [ ] 월드 데이터 저장

### 4.3 Admin Tools
- [x] 명령어 시스템 (/help, /spawn, /tp 등)
- [x] 서버 설정 (JSON)
- [ ] 관리자 권한 시스템

## 5. Tools & Automation

### 5.1 Build Tools
- [x] Unity CI Commandlet
- [x] 컴파일 테스트 배치 스크립트
- [ ] 자동 빌드 파이프라인

### 5.2 Data Tools
- [x] JSON 데이터 검증
- [x] 프로토콜 패리티 검사
- [ ] 데이터 마이그레이션 도구

## 6. Documentation

- [x] 아키텍처 문서 (docs/)
- [x] 기획 문서 (design/)
- [x] 작업 리스트 (plans/)
- [ ] API 문서
- [ ] 사용자 가이드

## Platform Support

- [x] Windows
- [ ] Linux (서버 전용)
- [ ] macOS

## Notes

- 모든 상수/설정값은 JSON 데이터 드리븐 방식 사용
- - C# .NET .0 준수 ( 유니티 에디터 C# 스크립트 컴파일 테스트 성공 시 서버 셀프테스트 성공

 - plans/listup.md . .NET 버전 정보 업데이 (Unity 6000.0.23f1)
- minetest 서브모듈 초기화 확인
- Unity 6000.0.23f1 사용
