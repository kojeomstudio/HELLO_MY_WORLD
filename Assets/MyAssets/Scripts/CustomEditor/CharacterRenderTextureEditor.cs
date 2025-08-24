#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEditor;

/// <summary>
/// JsonUtility가 최상위 배열([])을 직접 처리하지 못하는 문제를
/// 래퍼를 통해 우회하는 헬퍼.
/// </summary>
public static class JsonArrayUtility
{
    [Serializable]
    private class Wrapper<T> { public T[] items; }

    public static T[] FromJson<T>(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return Array.Empty<T>();
        int i = 0;
        while (i < json.Length && char.IsWhiteSpace(json[i])) i++;
        bool isArray = (i < json.Length && json[i] == '[');
        if (isArray)
        {
            string wrapped = "{\"items\":" + json + "}";
            var wrapper = JsonUtility.FromJson<Wrapper<T>>(wrapped);
            return wrapper?.items ?? Array.Empty<T>();
        }
        else
        {
            // 단일 객체가 들어오는 경우(본 프로젝트에서는 사용 안 함)
            var single = JsonUtility.FromJson<T>(json);
            return single != null ? new[] { single } : Array.Empty<T>();
        }
    }

    public static string ToJson<T>(IList<T> list, bool prettyPrint = false)
    {
        var wrapper = new Wrapper<T> { items = list == null ? Array.Empty<T>() : new List<T>(list).ToArray() };
        string wrapped = JsonUtility.ToJson(wrapper, prettyPrint);
        int firstBracket = wrapped.IndexOf('[');
        int lastBracket = wrapped.LastIndexOf(']');
        if (firstBracket >= 0 && lastBracket >= firstBracket)
            return wrapped.Substring(firstBracket, lastBracket - firstBracket + 1);
        return wrapped; // 방어적
    }
}

/// <summary>
/// Json파일로 저장될 캐릭터 데이터 (JsonUtility 직렬화 대상)
/// </summary>
[Serializable]
public class CharacterJsonDataFormat
{
    public string chName;
    public string chType;
    public string chLevel;
    public string detailScript;
    public string chFaceTextureName;
    public string chPrefabName;
}

/// <summary>
/// Chracter RenderTexture 생성기.
/// </summary>
public class CharacterRenderTextureEditor : EditorWindow
{
    private string filePath = null;

    // JSONObject / Dictionary → 강타입 배열로 변경
    private TextAsset jsonFile;
    private CharacterJsonDataFormat[] chDataArray = Array.Empty<CharacterJsonDataFormat>();
    private int maxChCard = 0;

    [MenuItem("CustomEditor/Character Front View To RenderTexture")]
    static void Init()
    {
        CharacterRenderTextureEditor window = (CharacterRenderTextureEditor)EditorWindow.GetWindow(typeof(CharacterRenderTextureEditor));
        window.Show();
    }

    void OnGUI()
    {
        EditorGUILayout.BeginToggleGroup("Features", true);
        GUILayout.TextArea("(1) 캐릭터 프리팹들의 정면 모습을 렌더링텍스처 파일들로 만들어냅니다.\n " +
                           "(2) 작업이 완료되면, SelectChars 프리팹을 Scene에 배치시킨 후, TextureUtility 툴에서 렌더링텍스처 to 텍스처 작업을 시작합니다.");
        if (GUILayout.Button("Process_AllInOne( Make DataFile and RenderTextures )"))
        {
            CreateCharsDataFile();
            ClickOpenFile();
            CreateRenderTextureFiles();
        }
        EditorGUILayout.EndToggleGroup();
    }

    private void CreateCharsDataFile()
    {
        GameObject[] charPrefabs = Resources.LoadAll<GameObject>(ConstFilePath.PREFAB_CHARACTER_RESOURCE_PATH);
        if (charPrefabs == null)
        {
            KojeomLogger.DebugLog("캐릭터 프리팹로딩에 실패했습니다.(데이터파일생성에 필요한)", LOG_TYPE.ERROR);
            return;
        }

        var datas = new List<CharacterJsonDataFormat>(charPrefabs.Length);
        int idx = 0;
        foreach (var p in charPrefabs)
        {
            var format = new CharacterJsonDataFormat
            {
                chName = p.name,
                chPrefabName = p.name,
                chFaceTextureName = p.name,
                detailScript = "Something here..",
                chLevel = "1",
                chType = idx.ToString()
            };
            datas.Add(format);
            idx++;
        }

        // JsonUtility 기반으로 최상위 배열([]) 저장
        string jsonData = JsonArrayUtility.ToJson(datas, prettyPrint: true);

        // 덮어쓰기
        File.WriteAllText(ConstFilePath.WINDOW_PATH_CHARACTER_DATAS_FILE, jsonData, new UTF8Encoding(false));
        KojeomLogger.DebugLog("CreateCharsDataFile Done.");
    }

    private void ClickOpenFile()
    {
        // Resources에서 JSON 텍스트 로드
        jsonFile = Resources.Load(ConstFilePath.TXT_RESOURCE_CHARACTER_DATAS) as TextAsset;
        if (jsonFile == null)
        {
            KojeomLogger.DebugLog("캐릭터 데이터 파일 로드 실패.", LOG_TYPE.ERROR);
            return;
        }

        // 최상위 배열 → 강타입 배열 역직렬화
        chDataArray = JsonArrayUtility.FromJson<CharacterJsonDataFormat>(jsonFile.text) ?? Array.Empty<CharacterJsonDataFormat>();
        maxChCard = chDataArray.Length;

        KojeomLogger.DebugLog("LoadCharDatas Done.");
    }

    private void CreateRenderTextureFiles()
    {
        if (maxChCard <= 0)
        {
            KojeomLogger.DebugLog("생성할 캐릭터 데이터가 없습니다.", LOG_TYPE.ERROR);
            return;
        }

        GameObject newSelectCharsGroup = Instantiate<GameObject>(Resources.Load<GameObject>(ConstFilePath.SELECT_CHARS_TEMPLATE_PREFAB));
        newSelectCharsGroup.name = "SelectCharacters";

        Light sceneEnvLight = Instantiate<GameObject>(Resources.Load<GameObject>(ConstFilePath.EDITOR_SCENE_ENV_LIGHT_PREFAB)).GetComponent<Light>();
        sceneEnvLight.transform.parent = newSelectCharsGroup.transform;

        // create renderTexture files.
        for (int i = 0; i < maxChCard; i++)
        {
            var data = chDataArray[i];
            string outTextureFileName = data.chFaceTextureName;

            StringBuilder toPath = new StringBuilder();
            toPath.AppendFormat(ConstFilePath.CH_RT_BASE_FILE_DIR, outTextureFileName);

            string sourcePath = ConstFilePath.CH_RT_BASE_FILE_WITH_EXT;
            string destPath = toPath.ToString();

            FileUtil.DeleteFileOrDirectory(destPath); // 기존 파일 삭제
            FileUtil.CopyFileOrDirectory(sourcePath, destPath); // 새 파일 생성.

            // renderTexture에 사용될 캐릭터 prefab 생성.
            CreateSelectCharPrefab(i, newSelectCharsGroup.transform, data);
        }

        // SelectCharacters 프리팹 저장
        StringBuilder selectCharsPrefabPath = new StringBuilder();
        selectCharsPrefabPath.AppendFormat(ConstFilePath.SAVE_PATH_FOR_SELECT_CHARS_PREFAB, "SelectCharacters");
        bool outSuccess = false;
        PrefabUtility.SaveAsPrefabAsset(newSelectCharsGroup, selectCharsPrefabPath.ToString(), out outSuccess);

        KojeomLogger.DebugLog("CreateRenderTextureFiles Done.");
    }

    private void CreateSelectCharPrefab(int idx, Transform group, CharacterJsonDataFormat data)
    {
        int objIntervalPos = 10;

        string prefabName = data.chPrefabName;
        string chName = data.chName;

        // Instanciate character object;
        StringBuilder prefabPath = new StringBuilder();
        prefabPath.AppendFormat(ConstFilePath.PREFAB_CHARACTER_RESOURCE_PATH + "{0}", prefabName);
        GameObject character = Instantiate(Resources.Load(prefabPath.ToString()),
            new Vector3(idx * objIntervalPos, 0, idx * objIntervalPos), Quaternion.identity) as GameObject;
        character.name = chName;
        character.transform.parent = group;

        // set camera - position, rot, parenting, naming
        GameObject camObj = new GameObject();
        camObj.transform.position = character.transform.position;
        camObj.transform.Rotate(new Vector3(0, 180.0f, 0));
        camObj.transform.parent = character.transform;
        camObj.name = "RenderTextureCamera";
        Vector3 newPos = camObj.transform.position;
        newPos.y += 0.5f;
        newPos.z += 1.0f;
        camObj.transform.position = newPos;

        // set camera - addComp, cullMasks, SetRT
        Camera cam = camObj.AddComponent<Camera>();
        // NOTE: LayerMask.NameToLayer는 인덱스를 반환 → 실제 마스크는 1 << index
        int layerIndex = LayerMask.NameToLayer("PlayerCharacter");
        if (layerIndex >= 0)
            cam.cullingMask = 1 << layerIndex;
        cam.farClipPlane = 1.0f;
        cam.clearFlags = CameraClearFlags.Nothing;

        StringBuilder targetRT_Path = new StringBuilder();
        targetRT_Path.AppendFormat(ConstFilePath.SELECT_CHARS_RT_RESOURCE_PATH, data.chFaceTextureName);

        // RenderTexture 리소스 로드
        RenderTexture rt = Resources.Load<RenderTexture>(targetRT_Path.ToString());
        cam.targetTexture = rt;
    }
}
#endif
