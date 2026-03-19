# Session 194: 블록/아이템 ID 통합 체계 및 네트워크 계층 개선

## 작업 일자
2026-03-20

## 작업 목표
- `work/work.md` 지침에 따라 minetest `NodeDef`/`ItemDef` 구조를 기준으로 블록/아이템 ID 통합 체계를 설계한다.
- Unity 네트워크 계층(`GameNetworkManager`)의 protobuf 단일 경로 수렴을 위한 분석을 수행한다.
- `docs`, `plans` 경로의 오래되거나 정합성이 낮은 문서를 정리한다.
- minetest 서브모듈 기반 아키텍처/데이터 모델 개선 포인트를 문서화한다.

## 현재 상황 분석

### 최근 1주일 커밋 기록 (2026-03-13 ~ 2026-03-20)
- Session 193: 데이터 드리븐 인벤토리/제작 리팩터 완료
- `InventoryManager`, `CraftingManager` 중복 정의 제거
- `StreamingAssets` JSON 로딩 경로 추가

### 작업 시작 시 로컬 상태
- `git status --short` 기준 변경사항 없음(clean).

### 현재 이슈
1. **네트워크 계층 이원화**: `GameNetworkManager`가 기존 `CPacket` 중심 로직 유지, protobuf 검증 호출 포함하나 전송 계층이 이원화 상태
2. **아이템 ID 자료형 정합성**: 블록/아이템 통합 식별체계 미설계
3. **서버-클라 공통 데이터 모델**: minetest `ServerInventoryManager`/`NodeDefManager` 대응 체계 부재

---

## 작업 항목

### 1. minetest 기준 블록/아이템 ID 체계 분석
- [ ] `minetest_project/src/nodedef.h` ContentFeatures 구조 분석
- [ ] `minetest_project/src/itemdef.h` ItemDefinition 구조 분석
- [ ] `minetest_project/src/inventorymanager.h` InventoryLocation 패턴 분석
- [ ] Unity/Server 공통 ID 매핑 설계안 작성

### 2. 네트워크 계층 개선 분석
- [ ] `GameNetworkManager.cs` 현재 상태 분석
- [ ] protobuf 단일 경로 수렴을 위한 인터페이스 설계
- [ ] 서버-클라 프로토콜 정합성 점검

### 3. 아키텍처 문서 업데이트
- [ ] minetest NodeDef/ItemDef 매핑 문서 작성 (`docs/2026-03-20-session-194-architecture-and-code-flow.md`)
- [ ] design 문서 업데이트 (`design/2026-03-20-session-194-design-execution.md`)

### 4. 문서 정리
- [ ] `docs/` 경로의 2025년/초기 2026년 구형 문서 삭제
- [ ] `plans/` 경로의 완료된 세션 계획 문서 정리

### 5. 검증 및 Git 반영
- [ ] `dotnet build SharedProtocol/SharedProtocol.csproj`
- [ ] `dotnet build GameServer/GameServer.csproj`
- [ ] 변경사항 커밋
- [ ] `git push origin master`

---

## 완료 작업 기록

| 항목 | 커밋 해시 | 완료일자 |
|------|-----------|----------|
| (대기) | - | - |
