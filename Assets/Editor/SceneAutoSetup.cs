#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class SceneAutoSetup : EditorWindow
{
    [MenuItem("CopRunner/🚀 씬 자동 세팅")]
    public static void SetupUI()
    {
        // Canvas 생성
        GameObject canvasObj = new GameObject("Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasObj.AddComponent<GraphicRaycaster>();

        // 패널 생성
        CreatePanel(canvasObj, "LobbyPanel",     Color.black);
        CreatePanel(canvasObj, "CountdownPanel", new Color(0,0,0,0.5f));
        CreatePanel(canvasObj, "HUDPanel",       new Color(0,0,0,0));
        CreatePanel(canvasObj, "ResultPanel",    new Color(0,0,0,0.7f));

        Debug.Log("[SceneAutoSetup] ✅ UI 세팅 완료!");
    }

    private static GameObject CreatePanel(
        GameObject parent, string name, Color color)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent.transform, false);

        Image img = panel.AddComponent<Image>();
        img.color = color;

        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        panel.SetActive(false);
        return panel;
    }
    public static void SetupScene()
    {
        Debug.Log("[SceneAutoSetup] 씬 자동 세팅 시작...");

        SetupGameManager();
        SetupMapPlane();
        SetupPlayer();
        SetupCamera();
        SetupUIManager();

        // 씬 자동 저장
        UnityEditor.SceneManagement.EditorSceneManager
            .SaveOpenScenes();

        Debug.Log("[SceneAutoSetup] ✅ 씬 세팅 + 저장 완료!");
    }

    private static void SetupUIManager()
    {
        if (GameObject.Find("UIManager") != null)
        {
            Debug.Log("[SceneAutoSetup] UIManager 이미 존재 — 스킵");
            return;
        }

        GameObject go = new GameObject("UIManager");
        go.AddComponent<UIManager>();
        Debug.Log("[SceneAutoSetup] UIManager 생성 완료");
    }

    private static void SetupGameManager()
    {
        if (GameObject.Find("GameManager") != null)
        {
            Debug.Log("[SceneAutoSetup] GameManager 이미 존재 — 스킵");
            return;
        }

        GameObject go = new GameObject("GameManager");
        go.AddComponent<GameManager>();
        Debug.Log("[SceneAutoSetup] GameManager 생성 완료");
    }

    private static void SetupMapPlane()
    {
        if (GameObject.Find("MapPlane") != null)
        {
            Debug.Log("[SceneAutoSetup] MapPlane 이미 존재 — 스킵");
            return;
        }

        GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        plane.name = "MapPlane";
        plane.transform.position   = Vector3.zero;
        plane.transform.localScale = new Vector3(5f, 1f, 5f);

        GameObject loaderObj = new GameObject("NaverMapLoader");
        NaverMapLoader loader = loaderObj.AddComponent<NaverMapLoader>();

        SerializedObject so = new SerializedObject(loader);
        so.FindProperty("targetRenderer").objectReferenceValue =
            plane.GetComponent<Renderer>();
        so.ApplyModifiedProperties();

        Debug.Log("[SceneAutoSetup] MapPlane + NaverMapLoader 생성 완료");
    }

    private static void SetupPlayer()
    {
        if (GameObject.Find("Player") != null)
        {
            Debug.Log("[SceneAutoSetup] Player 이미 존재 — 스킵");
            return;
        }

        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        player.transform.position = new Vector3(0f, 1f, 0f);

        Rigidbody rb = player.AddComponent<Rigidbody>();
        rb.freezeRotation = true;

        player.AddComponent<PlayerController>();

        // Input Action Asset 연결 (Asset이 존재할 때만)
        string assetPath = "Assets/PlayerInputActions.inputactions";
        var inputAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(assetPath);

        PlayerInput playerInput = player.AddComponent<PlayerInput>();

        if (inputAsset != null)
        {
            SerializedObject so = new SerializedObject(playerInput);
            so.FindProperty("m_Actions").objectReferenceValue = inputAsset;
            so.ApplyModifiedProperties();
            Debug.Log("[SceneAutoSetup] InputActionAsset 연결 완료");
        }
        else
        {
            Debug.LogWarning("[SceneAutoSetup] PlayerInputActions.inputactions 없음 — 수동 연결 필요");
        }

        Debug.Log("[SceneAutoSetup] Player 생성 완료");
    }

    private static void SetupCamera()
    {
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            Debug.LogWarning("[SceneAutoSetup] Main Camera 없음 — 스킵");
            return;
        }

        GameObject player = GameObject.Find("Player");

        // 카메라를 Player 자식에서 분리
        mainCam.transform.SetParent(null);
        mainCam.transform.position = Vector3.zero;
        mainCam.transform.rotation = Quaternion.identity;

        // CameraFollow 컴포넌트 추가
        CameraFollow follow = mainCam.gameObject.GetComponent<CameraFollow>();
        if (follow == null)
            follow = mainCam.gameObject.AddComponent<CameraFollow>();

        // Target 자동 연결
        if (player != null)
        {
            SerializedObject so = new SerializedObject(follow);
            so.FindProperty("target").objectReferenceValue = player.transform;
            so.ApplyModifiedProperties();
        }

        Debug.Log("[SceneAutoSetup] CameraFollow 세팅 완료");
    }
}
#endif