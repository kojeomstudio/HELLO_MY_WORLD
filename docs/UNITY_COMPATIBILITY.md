# Unity 6 호환성 가이드

## 프로젝트 정보

- **Unity 버전**: 6000.0.23f1 (Unity 6)
- **API Compatibility Level**: .NET Standard 2.1
- **C# 버전**: 9.0
- **Scripting Backend**: Mono / IL2CPP

## GameCommon 라이브러리 타겟 프레임워크

### .NET Standard 2.1 선택 이유

GameCommon 라이브러리는 `.NET Standard 2.1`을 타겟으로 합니다. 이는 Unity 6와의 완벽한 호환성을 위한 최적의 선택입니다.

#### Unity 6의 .NET 지원

Unity 6는 두 가지 API 호환성 레벨을 지원합니다:

1. **.NET Standard 2.1** (기본값, 권장) ✅
   - 모든 Unity 플랫폼 지원 (Windows, macOS, Linux, iOS, Android, WebGL 등)
   - 더 작은 라이브러리 크기
   - 크로스 플랫폼 호환성 최적화
   - Unity 공식 권장사항

2. **.NET Framework 4.8** + .NET Standard 2.1 추가 API
   - Windows 전용 기능이 필요한 경우
   - 레거시 .NET Framework DLL 사용 시

#### .NET 6/7/8은?

Unity 6는 아직 .NET 6+ (CoreCLR)로 업그레이드되지 않았습니다. Unity의 .NET Modernization 로드맵에 따르면, CoreCLR 지원은 향후 Unity 버전에서 제공될 예정입니다.

현재 Unity 6에서 사용 가능한 최신 .NET 표준은 `.NET Standard 2.1`입니다.

### C# 언어 버전

GameCommon 라이브러리는 **C# 9.0**을 사용합니다:

- Unity 2021.2+ 및 Unity 6에서 완벽 지원
- Record types, init-only properties, pattern matching 개선 등 활용
- 코드 품질 및 개발자 경험 향상

## Unity 프로젝트 설정 확인

### 현재 설정

Unity 프로젝트의 API Compatibility Level을 확인하려면:

1. Unity Editor 열기
2. **Edit > Project Settings > Player**
3. **Other Settings** 섹션
4. **API Compatibility Level** 확인

현재 설정: **.NET Standard 2.1** (권장)

### 설정 변경이 필요한 경우

일반적으로 변경이 필요하지 않지만, 만약 .NET Framework가 필요한 경우:

```
Edit > Project Settings > Player > Other Settings
API Compatibility Level: .NET Framework
```

**주의**: .NET Framework로 변경하면 일부 플랫폼(iOS, Android, WebGL 등)에서 호환성 문제가 발생할 수 있습니다.

## GameCommon DLL 사용 방법

### Unity 프로젝트에 통합

1. **GameCommon 라이브러리 빌드**:
   ```bash
   dotnet build GameCommon/GameCommon.csproj --configuration Release
   ```

2. **DLL 복사**:
   ```bash
   cp GameCommon/bin/Release/netstandard2.1/GameCommon.dll Assets/Plugins/
   cp GameCommon/bin/Release/netstandard2.1/GameCommon.pdb Assets/Plugins/  # 디버깅용
   ```

3. **Unity에서 자동 인식**: Unity Editor가 자동으로 DLL을 인식하고 컴파일에 포함

4. **코드에서 사용**:
   ```csharp
   using GameCommon.Blocks;
   using GameCommon.Configuration;

   // BlockType 사용
   BlockType block = BlockType.Stone;

   // ConfigManager 사용
   ConfigManager.Instance.LoadAll("config");
   var worldConfig = ConfigManager.Instance.World;
   ```

### 플랫폼별 호환성

.NET Standard 2.1 DLL은 다음 플랫폼에서 테스트되었습니다:

| 플랫폼 | 지원 상태 | 비고 |
|--------|----------|------|
| **Windows (Standalone)** | ✅ 완벽 지원 | Mono / IL2CPP |
| **macOS (Standalone)** | ✅ 완벽 지원 | Mono / IL2CPP |
| **Linux (Standalone)** | ✅ 완벽 지원 | Mono / IL2CPP |
| **iOS** | ✅ 지원 | IL2CPP 필수 |
| **Android** | ✅ 지원 | Mono / IL2CPP |
| **WebGL** | ✅ 지원 | IL2CPP |

## 패키지 종속성

GameCommon 라이브러리는 다음 NuGet 패키지를 사용합니다:

### System.Text.Json (7.0.0)

- JSON 설정 파일 로드에 사용
- .NET Standard 2.1과 호환
- Unity에서 사용 시 주의사항:
  - Unity 2021.2+ 에서는 별도 DLL 포함 필요 없음
  - System.Text.Json.dll도 Assets/Plugins/에 복사 필요

```bash
# System.Text.Json DLL도 함께 복사
cp GameCommon/bin/Release/netstandard2.1/System.Text.Json.dll Assets/Plugins/
```

## 문제 해결

### 빌드 오류: "The type or namespace 'GameCommon' could not be found"

**원인**: DLL이 Assets/Plugins/ 폴더에 없거나 Unity가 인식하지 못함

**해결**:
1. GameCommon.dll이 Assets/Plugins/에 있는지 확인
2. Unity Editor 재시작
3. Assets > Reimport All

### 런타임 오류: "FileNotFoundException: Could not load file or assembly 'System.Text.Json'"

**원인**: System.Text.Json.dll이 누락됨

**해결**:
```bash
cp GameCommon/bin/Release/netstandard2.1/System.Text.Json.dll Assets/Plugins/
```

### IL2CPP 빌드 오류

**원인**: IL2CPP는 리플렉션 사용 시 추가 설정 필요

**해결**: link.xml 파일 생성
```xml
<linker>
  <assembly fullname="GameCommon" preserve="all"/>
  <assembly fullname="System.Text.Json" preserve="all"/>
</linker>
```

Assets/link.xml로 저장

## 참고 문서

- [Unity 6 .NET Profile Support](https://docs.unity3d.com/6000.0/Documentation/Manual/dotnet-profile-support.html)
- [Unity Scripting Restrictions](https://docs.unity3d.com/6000.0/Documentation/Manual/ScriptingRestrictions.html)
- [.NET Standard 2.1 API Reference](https://docs.microsoft.com/en-us/dotnet/standard/net-standard)
- [Unity and .NET, what's next?](https://blog.unity.com/engine-platform/unity-and-net-whats-next)

## 향후 계획

Unity가 .NET 6+ (CoreCLR)를 지원하게 되면, GameCommon 라이브러리를 다음과 같이 업그레이드할 수 있습니다:

- 타겟 프레임워크: `.NET 6.0` 또는 `.NET 8.0`
- C# 버전: `10.0` 또는 `12.0`
- 성능 향상: Span<T>, Memory<T> 등 최신 기능 활용
- 최신 라이브러리: System.Text.Json 8.0+

현재는 Unity 6의 공식 지원 범위 내에서 `.NET Standard 2.1`을 사용하는 것이 최적의 선택입니다.
