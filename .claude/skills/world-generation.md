# World Generation Enhancement Skill

당신은 HELLO_MY_WORLD의 지형 생성 시스템을 개선하는 전문가입니다.

## 지형 생성 파이프라인

WorldManager의 TerrainGenerationPipeline은 다음 단계로 구성됩니다:

1. **BaseTerrainStage**: 대륙성, 침식, 생물군, 열, 습도
2. **OreGenerationStage**: 광물 배치 (석탄, 철, 금, 다이아몬드 등)
3. **CaveGenerationStage**: 동굴 생성 (Simplex 3D 노이즈)
4. **DungeonGenerationStage**: 던전 생성
5. **RiverGenerationStage**: 강 생성 (catchment-weighted)
6. **LakeGenerationStage**: 호수 생성 (basin-aware)
7. **VegetationGenerationStage**: 나무, 풀 배치
8. **CloudGenerationStage**: 구름 생성

## 주요 파라미터

### 전역 상수 (WorldManager.cs)
```csharp
GlobalWaterLevel = 62
RiverCenterThreshold = 0.0125
RiverBankThreshold = 0.028
OceanThreshold = 0.36
BeachThreshold = 0.42
CliffThreshold = 0.55
CloudBaseAltitude = 200
```

### 생물군 타입 (BiomeType enum)
- Ocean: 바다
- Plains: 평원
- Forest: 숲
- Desert: 사막
- Mountains: 산
- Hills: 언덕
- Beach: 해변
- Cliffs: 절벽

## 지형 프로필 구조

```csharp
struct TerrainProfile {
    int SurfaceHeight;          // 지표면 높이
    bool HasWater;              // 물이 있는지
    int WaterLevel;             // 물 높이
    BiomeType Biome;            // 생물군
    BlockType SurfaceBlock;     // 표면 블록 (Grass, Sand, Cobblestone)
    BlockType SubSurfaceBlock;  // 하위 블록
    BlockType FillerBlock;      // 충전 블록
    bool UseCliffFace;          // 절벽 면 사용 여부
}
```

## 최근 개선사항 (2025-11-07)

### 흐름 가중 강 생성
- Catchment-aware river generation
- 강의 흐름 방향 고려
- 자연스러운 강 네트워크 형성

### Basin-aware 호수 생성
- 지형의 분지(basin) 인식
- 호수의 자연스러운 경계 형성
- 수위 시뮬레이션

### 청크 메타데이터
- 생성 타임스탬프
- 생물군 정보
- ChunkPayloadBuilder를 통한 메타데이터 인코딩

## MapGeneratorLib 활용

독립형 지형 생성 라이브러리 (`MapGeneratorLib/`):

### 노이즈 생성
- Simplex Noise
- Perlin Noise
- 도메인 워핑 (Domain Warping)

### 알고리즘
- WorldGenAlgorithms.cs: 메인 생성 로직
- EnviromentGenAlgorithms.cs: 환경 요소 생성
- WorldGenerateUtils.cs: 유틸리티 함수

## 지형 생성 개선 작업

### 1. 노이즈 파라미터 튜닝
```csharp
// 예시: 산의 고도 조정
mountainNoise.frequency = 0.005f;
mountainNoise.octaves = 6;
mountainNoise.persistence = 0.5f;
```

### 2. 새로운 생물군 추가
1. BiomeType enum에 새 타입 추가
2. TerrainProfile 생성 로직 수정
3. 블록 타입 매핑 정의
4. 식생 규칙 추가

### 3. 구조물 생성
```csharp
// 마을, 사원, 요새 등
public class StructureGenerationStage : ITerrainStage {
    public void Generate(ChunkData chunk, TerrainContext context) {
        // 구조물 생성 로직
    }
}
```

### 4. 동굴 시스템 개선
- 3D Worley Noise 활용
- 동굴 크기 변화
- 지하 호수 및 용암 호수

## 디버깅 및 시각화

### CustomToolSet/MapTool
- 맵 프리뷰 및 시각화
- 생물군 분포 확인
- 고도 맵 생성

### 서버 로그
- 청크 생성 시간 측정
- 각 스테이지별 성능 프로파일링

## 성능 최적화

### 청크 생성 병렬화
```csharp
// ServerConfig.cs
Performance.MaxConcurrentChunkGenerations = 4
```

### 청크 캐싱
- 생성된 청크를 SQLite에 저장
- 재시작 시 빠른 로드
- WorldManager의 DatabaseHelper 활용

### 메모리 관리
- 청크 데이터 압축
- 사용하지 않는 청크 언로드
- ChunkSaveIntervalMinutes 설정

## 테스트 및 검증

### 지형 일관성 테스트
```csharp
// 청크 경계에서 블록 연속성 확인
// 동일한 시드로 재생성 시 동일한 결과 보장
```

### 생물군 전환 테스트
- 생물군 경계의 부드러운 전환
- 블록 타입 혼합

### 강/호수 테스트
- 물의 흐름 방향
- 호수의 수위 일관성
- 강과 바다의 연결

## 문제 해결

### 생성 속도 느림
- 노이즈 계산 최적화
- 불필요한 스테이지 비활성화
- 병렬 생성 스레드 증가

### 지형 불연속
- 청크 경계 블록 처리 개선
- 이웃 청크 정보 활용

### 메모리 부족
- 청크 크기 조정
- 압축 알고리즘 개선
- 메모리 프로파일링

## 향후 개선 계획

1. **절차적 구조물 생성**: 마을, 사원, 던전
2. **생물군 다양화**: 정글, 타이가, 사바나 등
3. **동적 지형 변화**: 화산, 지진, 침식 시뮬레이션
4. **3D 바이옴**: 지하 생물군 (버섯 동굴 등)
5. **사용자 정의 월드 타입**: WorldType.CUSTOM 지원
