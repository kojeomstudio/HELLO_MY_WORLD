# HELLO_MY_WORLD Project PDD

## 1. Purpose
- Minecraft 스타일의 오픈 월드 게임을 Unity3D로 구현한다.
- 자체 제작 네트워크 라이브러리를 통해 확장 가능한 멀티플레이 환경을 제공한다.

## 2. Development Environment
- **Unity Engine**: 2020.3.24f1 (LTS)
- **C#/.NET**: .NET Framework 4.5 기반, C# 5 호환
- **IDE**: Visual Studio, Rider, VSCode 등

## 3. Dependencies
Unity 패키지 매니페스트에 정의된 주요 패키지 버전:
- com.unity.2d.sprite 1.0.0
- com.unity.2d.tilemap 1.0.0
- com.unity.collab-proxy 1.15.4
- com.unity.ext.nunit 1.0.6
- com.unity.ide.rider 2.0.7
- com.unity.ide.visualstudio 2.0.12
- com.unity.ide.vscode 1.2.4
- com.unity.multiplayer-hlapi 1.0.8
- com.unity.postprocessing 3.1.1
- com.unity.quicksearch 2.0.2
- com.unity.recorder 2.5.7
- com.unity.render-pipelines.core 10.7.0
- com.unity.shadergraph 10.7.0
- com.unity.test-framework 1.1.29
- com.unity.textmeshpro 3.0.6
- com.unity.timeline 1.4.8
- com.unity.ugui 1.0.0
- com.unity.xr.legacyinputhelpers 2.1.8

## 4. Network Architecture
- **KojeomNet** 라이브러리를 이용해 클라이언트/서버 구조를 구현.
- 새로 추가된 `PeerToPeerNetwork` 클래스로 P2P 직접 통신 지원.
- 각 피어는 서버와 클라이언트 역할을 동시에 수행하며, 간단한 브로드캐스트 기능을 제공.
- 연결 시 간단한 핸드셰이크를 통해 피어 식별 정보를 교환하여 기본 인증 구조를 마련.

## 5. Testing & Build
- `dotnet build KojeomNetWorkSpace/KojeomNet/KojeomNet.csproj` 로 네트워크 라이브러리를 빌드.
- 추후 필요 시 Unity Test Runner 또는 별도 테스트 프로젝트를 통해 기능 검증.

## 6. Future Improvements
- NAT 트래버설, 인증 및 암호화 기능 보강.
- 대규모 세션 관리를 위한 메시지 라우팅 최적화.
- Unity용 고수준 API로 래핑하여 게임 오브젝트와의 연동 강화.
