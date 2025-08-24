# HELLO_MY_WORLD

This project is an open-source voxel game that aims to mimic the core mechanics of Minecraft. All source code and assets in this repository are available under the MIT license, though external libraries and resources may carry their own licenses.

![hello_my_world](https://user-images.githubusercontent.com/9248400/75618900-dc37ab00-5bb7-11ea-9ec0-9759c0b6429f.png)
![hmw_git_main_img](https://user-images.githubusercontent.com/9248400/102211930-b47fbc80-3f17-11eb-8d7a-53281bb826ce.png)

## Project Overview
- **Development period:** 2016/01 ~ 2021/12 (hold)
- **Engine:** Unity 6000.0.23f1
- **Language:** C# targeting the .NET Framework 4.5
- **Libraries:** NGUI 3.x, Sqlite3, JsonObject, Newtonsoft.Json, iTween, FMOD, UniRx, FreeNet, ECM, etc.
- **Platforms:** Windows PC (Android planned)
- **License:** MIT

## Repository Structure
- `Assets/` – Unity game content and scripts. `MyAssets/Scripts` includes modules for AI, GameWorld, Network, Player, UI, pathfinding, and more.
- `KojeomNetWorkSpace/` – contains the `KojeomNet` network library along with the game server and test clients.
- `MapGeneratorLib/` – standalone library for procedural map generation.
- `CustomToolSet/` – editor utilities such as `ActorGeneratorTool` and `MapTool`.
- `Documents/` – design documents and guides (`Project_PDD.md`).
- `Packages/` – Unity package manifest listing engine dependencies.
- `Config/`, `ProjectSettings/`, `UserSettings/` – engine configuration files.
- `Recordings/` – gameplay capture sessions.

## Development Environment
- Unity Engine **6000.0.23f1**
- C# / .NET Framework 4.5
- IDE: Visual Studio, Rider, or VS Code

## Unity Package Dependencies
Key packages from `Packages/manifest.json` include:

- `com.unity.2d.sprite` 1.0.0
- `com.unity.2d.tilemap` 1.0.0
- `com.unity.ai.navigation` 2.0.8
- `com.unity.collab-proxy` 2.5.2
- `com.unity.ext.nunit` 2.0.5
- `com.unity.ide.visualstudio` 2.0.22
- `com.unity.multiplayer.center` 1.0.0
- `com.unity.postprocessing` 3.4.0
- `com.unity.recorder` 5.1.1
- `com.unity.render-pipelines.core` 17.0.3
- `com.unity.shadergraph` 17.0.3
- `com.unity.test-framework` 1.4.5
- `com.unity.timeline` 1.8.7
- `com.unity.ugui` 2.0.0
- `com.unity.xr.legacyinputhelpers` 2.1.11

See `Packages/manifest.json` for the full dependency list.

## Building and Testing
1. Clone this repository and open the root folder with **Unity 6000.0.23f1**.
2. Optional .NET components can be built with:
   ```bash
   dotnet build KojeomNetWorkSpace/KojeomNet/KojeomNet.csproj
   dotnet build MapGeneratorLib/MapGeneratorLib.sln
   ```
3. After installing the .NET SDK, run available tests with `dotnet test`.
4. Custom tools such as the map and actor generators can be opened through their solution files in `CustomToolSet/`.

## Additional Resources
There is a helpful tutorial used at the start of the project:<br>
http://studentgamedev.blogspot.kr/2013/08/unity-voxel-tutorial-part-1-generating.html
