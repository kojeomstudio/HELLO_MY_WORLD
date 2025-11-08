# HELLO_MY_WORLD Project PDD

## 1. Purpose
- Minecraft 스타일의 오픈 월드 게임을 Unity3D로 구현한다.
- 자체 제작 네트워크 라이브러리를 통해 확장 가능한 멀티플레이 환경을 제공한다.

## 2. Development Environment
- **Unity Engine**: 2020.3.24f1 (LTS)
- **C#/.NET**: Unity 런타임은 .NET Framework 4.5 기반이지만, 서버 측 구성 요소는 **.NET 6.0**을 사용한다.
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
- **SharedProtocol** 프로젝트가 `game.proto`에 정의된 패킷을 ProtoBuf로 직렬화하고, 공용 `Session`과 `MessageDispatcher`를 제공한다.
- 게임 서버(`GameServer`)는 TCP 기반으로 클라이언트를 수용하며, 수신한 패킷을 등록된 핸들러로 라우팅한다.
- `SessionManager`는 로그인한 클라이언트를 추적하고, `DatabaseHelper`는 SQLite를 이용해 캐릭터와 맵 데이터를 안전하게 저장한다.
- 현재는 `LoginHandler`와 `MovementHandler`가 포함되어 있으며, 이 구조를 통해 추가 패킷 핸들러를 쉽게 확장할 수 있다.

## 5. Testing & Build
- .NET SDK가 설치된 환경에서 다음 명령으로 프로토콜 라이브러리와 게임 서버를 빌드한다.
  ```bash
  dotnet build SharedProtocol/SharedProtocol.csproj
  dotnet build GameServer/GameServer.csproj
  ```
- 추후 필요 시 Unity Test Runner 또는 별도 테스트 프로젝트를 통해 기능 검증.

## 6. Future Improvements
- NAT 트래버설, 인증 및 암호화 기능 보강.
- 대규모 세션 관리를 위한 메시지 라우팅 최적화.
- Unity용 고수준 API로 래핑하여 게임 오브젝트와의 연동 강화.
