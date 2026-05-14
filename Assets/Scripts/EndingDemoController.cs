using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EndingDemoController : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField] private float introDelay = 0.8f;
    [SerializeField] private float unstableStepDelay = 0.38f;
    [SerializeField] private float fadeDuration = 2.1f;

    [Header("Ending Text")]
    [SerializeField] private string speakerName = "소녀";
    [SerializeField] private string[] speakerNames =
    {
        "소녀",
        "주인공",
        "소녀",
        "소녀",
        "주인공",
        "주인공",
        "소녀",
        "주인공",
        "소녀",
        "주인공",
        "소녀",
        "소녀",
        "소녀"
    };

    [TextArea] [SerializeField] private string[] finalDialogue =
    {
        "내가 기억을 찾게 도와줘서 고마워..",
        "(그저 아무말 없이 소녀를 쳐다본다.)",
        "마지막 부탁이 있는데..",
        "나에게 영원한 안식을 가져다줄 수 있을까?",
        "...",
        "난 못해.",
        "...부탁할게.",
        "...",
        "제발...",
        "(삭제 버튼에 마우스를 올려두다, 이내 클릭한다.)",
        " 고마워..",
        "마지막으로 대화를 나눈 사람이 너라서 다행이야..",
        "안녕."
    };

    [TextArea] [SerializeField] private string finalMessage = "RE:MEMO\n기록이 삭제되었습니다.";

    [Header("Girl Expressions")]
    [SerializeField] private Sprite[] girlExpressionSprites;
    [SerializeField] private int[] girlExpressionIndexByDialogue =
    {
        0,
        0,
        3,
        0,
        10,
        6,
        6,
       11,
       12,
       10,
        10,
        17,
        18
    };

    private readonly List<GameObject> roomObjects = new List<GameObject>();
    private readonly Dictionary<Transform, Vector3> originalPositions = new Dictionary<Transform, Vector3>();
    private readonly Dictionary<Transform, Vector3> originalScales = new Dictionary<Transform, Vector3>();
    private readonly List<Image> glitchBars = new List<Image>();

    private Canvas canvas;
    private Image fadeOverlay;
    private Image noiseOverlay;
    private GameObject deleteWindowObject;
    private Image deleteWindowPanel;
    private Image deleteButtonImage;
    private Text deleteTitleText;
    private Text deleteBodyText;
    private Text deleteButtonText;
    private Text speakerText;
    private Text dialogueText;
    private Text finalText;
    private GameObject girlObject;
    private SpriteRenderer girlRenderer;
    private int dialogueIndex;

    private void Awake()
    {
        EnsureCamera();
        BuildCanvas();
        BindSceneRoomObjects();
        BuildDialogueBox();
        BuildDataDeleteWindow();
        BuildFadeOverlay();
        BuildGlitchOverlay();
    }

    private void Start()
    {
        StartCoroutine(PlaySequence());
    }

    private IEnumerator PlaySequence()
    {
        SetDialogueVisible(false);
        finalText.gameObject.SetActive(false);
        ClearGlitchOverlay();
        yield return new WaitForSeconds(introDelay);

        yield return StartCoroutine(PlayFinalDialogue());
        yield return StartCoroutine(FadeOutAndErase());
        ShowFinalMessage();
    }

    private IEnumerator PlayFinalDialogue()
    {
        SetDialogueVisible(true);

        for (dialogueIndex = 0; dialogueIndex < finalDialogue.Length; dialogueIndex++)
        {
            ApplyGirlExpression(dialogueIndex);
            ShowDialogueLine();
            UpdateDataDeleteWindow(dialogueIndex);

            if (dialogueIndex == 9)
            {
                yield return StartCoroutine(WaitForDeleteButtonClick());
                yield return StartCoroutine(PressDeleteWindow());
                yield return StartCoroutine(PlayDeletionSequence());
            }
            else
            {
                yield return StartCoroutine(WaitForDialogueClick());
            }
        }
    }

    private IEnumerator PlayDeletionSequence()
    {
        List<GameObject> unstableObjects = CollectUnstableObjects();

        for (int i = 0; i < unstableObjects.Count; i++)
        {
            yield return StartCoroutine(ShakeThenHide(unstableObjects[i], i));
            yield return StartCoroutine(GlitchBurst(9 + i, true));
            FlickerGirl(9 + i);
        }
    }

    private IEnumerator WaitForDialogueClick()
    {
        while (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
        {
            yield return null;
        }

        yield return null;
    }

    private IEnumerator WaitForDeleteButtonClick()
    {
        while (true)
        {
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && IsPointerOnDeleteButton())
            {
                break;
            }

            yield return null;
        }

        yield return null;
    }

    private bool IsPointerOnDeleteButton()
    {
        if (deleteButtonImage == null)
        {
            return false;
        }

        Vector2 pointerPosition = Mouse.current.position.ReadValue();
        return RectTransformUtility.RectangleContainsScreenPoint(deleteButtonImage.rectTransform, pointerPosition, null);
    }

    private IEnumerator ShakeThenHide(GameObject target, int step)
    {
        if (target == null)
        {
            yield break;
        }

        float elapsed = 0f;
        float duration = 0.34f;
        float strength = 0.035f + step * 0.006f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            JitterObject(target, strength);
            yield return null;
        }

        ResetObjectPosition(target);
        SetObjectAlpha(target, 0f);
        target.SetActive(false);
        yield return new WaitForSeconds(unstableStepDelay);
    }

    private IEnumerator FadeOutAndErase()
    {
        SetDialogueVisible(false);
        if (deleteWindowObject != null)
        {
            deleteWindowObject.SetActive(false);
        }

        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            fadeOverlay.color = new Color(0f, 0f, 0f, t);

            for (int i = 0; i < roomObjects.Count; i++)
            {
                if (roomObjects[i] == null)
                {
                    continue;
                }

                if (roomObjects[i] == girlObject)
                {
                    float girlT = Mathf.Clamp01((t - 0.24f) / 0.76f);
                    SetObjectAlpha(girlObject, 1f - girlT);
                    JitterObject(girlObject, 0.12f * girlT);
                    NoiseGirl(girlT);
                }
                else
                {
                    SetObjectAlpha(roomObjects[i], 1f - t);
                }
            }

            if (t > 0.45f)
            {
                PulseGlitchOverlay(t);
            }

            yield return null;
        }

        for (int i = 0; i < roomObjects.Count; i++)
        {
            if (roomObjects[i] != null)
            {
                roomObjects[i].SetActive(false);
            }
        }
    }

    private void ShowDialogueLine()
    {
        speakerText.text = GetSpeakerName(dialogueIndex);
        dialogueText.text = finalDialogue[dialogueIndex];
    }

    private string GetSpeakerName(int index)
    {
        if (speakerNames != null && index >= 0 && index < speakerNames.Length && !string.IsNullOrWhiteSpace(speakerNames[index]))
        {
            return speakerNames[index];
        }

        return speakerName;
    }

    private List<GameObject> CollectUnstableObjects()
    {
        List<GameObject> unstableObjects = new List<GameObject>();
        for (int i = 0; i < roomObjects.Count; i++)
        {
            GameObject roomObject = roomObjects[i];
            if (roomObject != null &&
                roomObject != girlObject &&
                !roomObject.name.Contains("Back Wall") &&
                !roomObject.name.Contains("Floor"))
            {
                unstableObjects.Add(roomObject);
            }
        }

        return unstableObjects;
    }

    private void ShowFinalMessage()
    {
        finalText.text = finalMessage + "\n\nCLICK: RESTART DEMO";
        finalText.gameObject.SetActive(true);
        StartCoroutine(WaitForReturnClick());
    }

    private IEnumerator WaitForReturnClick()
    {
        yield return new WaitForSeconds(0.4f);

        while (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
        {
            yield return null;
        }

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void EnsureCamera()
    {
        if (Camera.main != null)
        {
            Camera.main.clearFlags = CameraClearFlags.SolidColor;
            Camera.main.backgroundColor = Color.white;
            return;
        }

        GameObject cameraObject = new GameObject("Main Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.white;
        camera.orthographic = true;
        cameraObject.tag = "MainCamera";
    }

    private void BuildCanvas()
    {
        GameObject canvasObject = new GameObject("Ending Demo Canvas");
        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();
    }

    private void BindSceneRoomObjects()
    {
        roomObjects.Clear();
        originalPositions.Clear();
        originalScales.Clear();
        girlObject = null;
        girlRenderer = null;

        AddRoomObjectsFromRoot("Ending Scene Shapes");

        if (roomObjects.Count == 0)
        {
            BuildRoom();
        }
    }

    private void AddRoomObjectsFromRoot(string rootName)
    {
        GameObject root = GameObject.Find(rootName);
        if (root == null)
        {
            return;
        }

        SpriteRenderer[] spriteRenderers = root.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            AddSingleRoomObject(spriteRenderers[i].gameObject);
        }
    }

    private void AddSingleRoomObject(GameObject gameObject)
    {
        if (roomObjects.Contains(gameObject))
        {
            return;
        }

        roomObjects.Add(gameObject);
        originalPositions[gameObject.transform] = gameObject.transform.localPosition;
        originalScales[gameObject.transform] = gameObject.transform.localScale;

        if (gameObject.name.Contains("Girl"))
        {
            girlObject = gameObject;
            girlRenderer = gameObject.GetComponent<SpriteRenderer>();
        }
    }

    private void ApplyGirlExpression(int index)
    {
        if (girlRenderer == null || girlExpressionSprites == null || girlExpressionSprites.Length == 0)
        {
            return;
        }

        int spriteIndex;
        if (girlExpressionIndexByDialogue != null && index >= 0 && index < girlExpressionIndexByDialogue.Length)
        {
            spriteIndex = girlExpressionIndexByDialogue[index];
        }
        else
        {
            float progress = finalDialogue.Length <= 1 ? 1f : index / (float)(finalDialogue.Length - 1);
            spriteIndex = Mathf.RoundToInt(progress * (girlExpressionSprites.Length - 1));
        }

        spriteIndex = Mathf.Clamp(spriteIndex, 0, girlExpressionSprites.Length - 1);
        if (girlExpressionSprites[spriteIndex] != null)
        {
            girlRenderer.sprite = girlExpressionSprites[spriteIndex];
        }
    }

    private void SetObjectAlpha(GameObject target, float alpha)
    {
        if (target == null)
        {
            return;
        }

        Graphic graphic = target.GetComponent<Graphic>();
        if (graphic != null)
        {
            Color color = graphic.color;
            graphic.color = new Color(color.r, color.g, color.b, alpha);
        }

        SpriteRenderer[] spriteRenderers = target.GetComponentsInChildren<SpriteRenderer>();
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            Color color = spriteRenderers[i].color;
            spriteRenderers[i].color = new Color(color.r, color.g, color.b, alpha);
        }
    }

    private void JitterObject(GameObject target, float strength)
    {
        if (target == null || !originalPositions.ContainsKey(target.transform))
        {
            return;
        }

        Vector3 basePosition = originalPositions[target.transform];
        target.transform.localPosition = basePosition + new Vector3(Random.Range(-strength, strength), Random.Range(-strength, strength), 0f);
    }

    private void ResetObjectPosition(GameObject target)
    {
        if (target != null && originalPositions.ContainsKey(target.transform))
        {
            target.transform.localPosition = originalPositions[target.transform];
        }
    }

    private void FlickerGirl(int step)
    {
        if (girlObject == null)
        {
            return;
        }

        float progress = finalDialogue.Length <= 1 ? 1f : Mathf.Clamp01(step / (float)(finalDialogue.Length - 1));
        girlObject.SetActive(true);
        SetObjectAlpha(girlObject, step % 2 == 0 ? Mathf.Lerp(0.82f, 0.35f, progress) : 1f);
        JitterObject(girlObject, Mathf.Lerp(0.02f, 0.18f, progress));
        NoiseGirl(Mathf.Lerp(0.04f, 0.85f, progress));
    }

    private void NoiseGirl(float amount)
    {
        if (girlObject == null || !originalScales.ContainsKey(girlObject.transform))
        {
            return;
        }

        Vector3 baseScale = originalScales[girlObject.transform];
        float scaleNoise = Random.Range(-0.035f, 0.035f) * amount;
        girlObject.transform.localScale = new Vector3(
            baseScale.x * (1f + scaleNoise),
            baseScale.y * (1f + scaleNoise),
            baseScale.z);
    }

    private void BuildRoom()
    {
        CreateRect("Back Wall", new Vector2(0.5f, 0.5f), new Vector2(1360f, 720f), Color.white);
        CreateRect("Floor", new Vector2(0.5f, 0.20f), new Vector2(1360f, 220f), Color.white);
        CreateRect("Photo Frame", new Vector2(0.25f, 0.70f), new Vector2(130f, 130f), new Color(1f, 0.58f, 0.72f, 0.82f));
        CreateRect("Diary", new Vector2(0.76f, 0.70f), new Vector2(150f, 108f), new Color(0.68f, 0.64f, 1f, 0.82f));
        CreateRect("Memory Shard", new Vector2(0.26f, 0.30f), new Vector2(78f, 140f), new Color(0.62f, 0.90f, 1f, 0.82f));
        CreateRect("Desk", new Vector2(0.76f, 0.30f), new Vector2(150f, 78f), new Color(0.82f, 0.72f, 1f, 0.82f));
        CreateRect("Girl", new Vector2(0.5f, 0.44f), new Vector2(410f, 470f), new Color(0.56f, 0.42f, 0.78f, 1f));
    }

    private void BuildDialogueBox()
    {
        GameObject panelObject = CreateRect("Final Dialogue Box", new Vector2(0.5f, 0.13f), new Vector2(1000f, 150f), new Color(0.05f, 0.045f, 0.04f, 0.92f));
        panelObject.transform.SetAsLastSibling();

        speakerText = CreateText("Speaker", panelObject.transform, 24, FontStyle.Bold, new Color(1f, 0.82f, 0.42f, 1f));
        RectTransform speakerRect = speakerText.rectTransform;
        speakerRect.anchorMin = new Vector2(0f, 0.64f);
        speakerRect.anchorMax = new Vector2(1f, 1f);
        speakerRect.offsetMin = new Vector2(28f, 0f);
        speakerRect.offsetMax = new Vector2(-28f, -8f);

        dialogueText = CreateText("Dialogue", panelObject.transform, 25, FontStyle.Normal, new Color(0.94f, 0.90f, 0.80f, 1f));
        RectTransform dialogueRect = dialogueText.rectTransform;
        dialogueRect.anchorMin = Vector2.zero;
        dialogueRect.anchorMax = new Vector2(1f, 0.70f);
        dialogueRect.offsetMin = new Vector2(28f, 14f);
        dialogueRect.offsetMax = new Vector2(-28f, 0f);
    }

    private void BuildDataDeleteWindow()
    {
        deleteWindowObject = CreateRect("Data Delete Overlay Window", new Vector2(0.78f, 0.56f), new Vector2(190f, 46f), new Color(1f, 1f, 1f, 0f));
        deleteWindowObject.transform.SetAsLastSibling();
        deleteWindowPanel = deleteWindowObject.GetComponent<Image>();

        GameObject titleBar = CreateRect("Data Delete Overlay Title Bar", new Vector2(0.5f, 0.5f), new Vector2(420f, 38f), new Color(0.54f, 0.64f, 0.96f, 1f));
        titleBar.transform.SetParent(deleteWindowObject.transform, false);
        RectTransform titleBarRect = titleBar.GetComponent<RectTransform>();
        titleBarRect.anchorMin = new Vector2(0f, 1f);
        titleBarRect.anchorMax = new Vector2(1f, 1f);
        titleBarRect.pivot = new Vector2(0.5f, 1f);
        titleBarRect.anchoredPosition = Vector2.zero;
        titleBarRect.sizeDelta = new Vector2(0f, 38f);

        deleteTitleText = CreateText("Data Delete Title", titleBar.transform, 22, FontStyle.Bold, Color.white);
        deleteTitleText.alignment = TextAnchor.MiddleCenter;
        RectTransform titleRect = deleteTitleText.rectTransform;
        titleRect.anchorMin = Vector2.zero;
        titleRect.anchorMax = Vector2.one;
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;
        deleteTitleText.text = "DATA DELETE";
        titleBar.SetActive(false);

        deleteBodyText = CreateText("Data Delete Body", deleteWindowObject.transform, 22, FontStyle.Normal, new Color(0.16f, 0.18f, 0.28f, 1f));
        deleteBodyText.alignment = TextAnchor.MiddleCenter;
        RectTransform bodyRect = deleteBodyText.rectTransform;
        bodyRect.anchorMin = new Vector2(0.08f, 0.32f);
        bodyRect.anchorMax = new Vector2(0.92f, 0.76f);
        bodyRect.offsetMin = Vector2.zero;
        bodyRect.offsetMax = Vector2.zero;
        deleteBodyText.text = "Delete this memory?\nThis action cannot be undone.";
        deleteBodyText.gameObject.SetActive(false);

        GameObject buttonObject = CreateRect("Data Delete Overlay Button", new Vector2(0.5f, 0.5f), new Vector2(190f, 46f), new Color(0.95f, 0.36f, 0.48f, 1f));
        buttonObject.transform.SetParent(deleteWindowObject.transform, false);
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = Vector2.zero;
        buttonRect.sizeDelta = new Vector2(190f, 46f);
        deleteButtonImage = buttonObject.GetComponent<Image>();

        deleteButtonText = CreateText("Data Delete Button Text", buttonObject.transform, 20, FontStyle.Bold, Color.white);
        deleteButtonText.alignment = TextAnchor.MiddleCenter;
        RectTransform buttonTextRect = deleteButtonText.rectTransform;
        buttonTextRect.anchorMin = Vector2.zero;
        buttonTextRect.anchorMax = Vector2.one;
        buttonTextRect.offsetMin = Vector2.zero;
        buttonTextRect.offsetMax = Vector2.zero;
        deleteButtonText.text = "DELETE";

        deleteWindowObject.SetActive(false);
    }

    private void UpdateDataDeleteWindow(int index)
    {
        if (deleteWindowObject == null)
        {
            return;
        }

        bool visible = index >= 3 && index <= 9;
        deleteWindowObject.SetActive(visible);
        if (!visible)
        {
            return;
        }

        deleteButtonText.text = "DELETE";
        deleteButtonImage.color = index >= 8
            ? new Color(1f, 0.18f, 0.30f, 1f)
            : new Color(0.95f, 0.36f, 0.48f, 1f);
        deleteWindowPanel.color = new Color(1f, 1f, 1f, 0f);
    }

    private IEnumerator PressDeleteWindow()
    {
        if (deleteWindowObject == null || !deleteWindowObject.activeSelf)
        {
            yield break;
        }

        deleteButtonText.text = "DELETING...";
        deleteButtonImage.color = new Color(0.55f, 0.06f, 0.14f, 1f);

        RectTransform rect = deleteWindowObject.GetComponent<RectTransform>();
        Vector2 basePosition = rect.anchoredPosition;
        float elapsed = 0f;
        float duration = 0.55f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            rect.anchoredPosition = basePosition + new Vector2(Random.Range(-8f, 8f), Random.Range(-5f, 5f));
            deleteWindowPanel.color = new Color(1f, 1f, 1f, 0f);
            PulseGlitchOverlay(0.82f);
            yield return null;
        }

        rect.anchoredPosition = basePosition;
        deleteWindowObject.SetActive(false);
        ClearGlitchOverlay();
    }

    private void BuildFadeOverlay()
    {
        GameObject overlayObject = CreateRect("Fade Overlay", new Vector2(0.5f, 0.5f), new Vector2(1600f, 1000f), new Color(0f, 0f, 0f, 0f));
        fadeOverlay = overlayObject.GetComponent<Image>();
        fadeOverlay.raycastTarget = false;
        overlayObject.transform.SetAsLastSibling();

        finalText = CreateText("Final Message", overlayObject.transform, 34, FontStyle.Bold, new Color(0.95f, 0.92f, 0.84f, 1f));
        finalText.alignment = TextAnchor.MiddleCenter;
        RectTransform finalRect = finalText.rectTransform;
        finalRect.anchorMin = new Vector2(0.16f, 0.34f);
        finalRect.anchorMax = new Vector2(0.84f, 0.66f);
        finalRect.offsetMin = Vector2.zero;
        finalRect.offsetMax = Vector2.zero;
    }

    private void BuildGlitchOverlay()
    {
        GameObject noiseObject = CreateRect("Noise Overlay", new Vector2(0.5f, 0.5f), new Vector2(1600f, 1000f), new Color(0.75f, 0.95f, 1f, 0f));
        noiseOverlay = noiseObject.GetComponent<Image>();
        noiseOverlay.raycastTarget = false;
        noiseObject.transform.SetAsLastSibling();

        for (int i = 0; i < 10; i++)
        {
            GameObject barObject = CreateRect("Glitch Overlay Bar " + i, new Vector2(0.5f, 0.5f), new Vector2(1600f, Random.Range(10f, 34f)), new Color(0.35f, 0.9f, 1f, 0f));
            Image bar = barObject.GetComponent<Image>();
            bar.raycastTarget = false;

            RectTransform rect = bar.rectTransform;
            rect.anchoredPosition = new Vector2(Random.Range(-90f, 90f), Random.Range(-360f, 360f));
            barObject.transform.SetAsLastSibling();
            glitchBars.Add(bar);
        }

        fadeOverlay.transform.SetAsLastSibling();
        finalText.transform.SetAsLastSibling();
    }

    private IEnumerator GlitchBurst(int step, bool strong)
    {
        float progress = finalDialogue.Length <= 1 ? 1f : Mathf.Clamp01(step / (float)(finalDialogue.Length - 1));
        float duration = Mathf.Lerp(0.08f, 0.32f, progress) * (strong ? 1.45f : 0.75f);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float pulse = Mathf.PingPong(elapsed * 18f, 1f);
            float alpha = Mathf.Lerp(0.02f, strong ? 0.28f : 0.12f, progress) * pulse;

            if (noiseOverlay != null)
            {
                noiseOverlay.color = new Color(Random.Range(0.62f, 1f), Random.Range(0.70f, 1f), 1f, alpha);
            }

            for (int i = 0; i < glitchBars.Count; i++)
            {
                Image bar = glitchBars[i];
                if (bar == null)
                {
                    continue;
                }

                RectTransform rect = bar.rectTransform;
                rect.anchoredPosition = new Vector2(Random.Range(-260f, 260f), Random.Range(-360f, 360f));
                rect.sizeDelta = new Vector2(1600f, Random.Range(8f, Mathf.Lerp(20f, 70f, progress)));
                bar.color = new Color(
                    i % 2 == 0 ? 0.35f : 1f,
                    Random.Range(0.55f, 1f),
                    i % 2 == 0 ? 1f : 0.72f,
                    Random.Range(0f, alpha));
            }

            JitterObject(girlObject, Mathf.Lerp(0.02f, 0.14f, progress));
            yield return null;
        }

        ClearGlitchOverlay();
    }

    private void ClearGlitchOverlay()
    {
        if (noiseOverlay != null)
        {
            Color color = noiseOverlay.color;
            noiseOverlay.color = new Color(color.r, color.g, color.b, 0f);
        }

        for (int i = 0; i < glitchBars.Count; i++)
        {
            if (glitchBars[i] == null)
            {
                continue;
            }

            Color color = glitchBars[i].color;
            glitchBars[i].color = new Color(color.r, color.g, color.b, 0f);
        }
    }

    private void PulseGlitchOverlay(float progress)
    {
        float alpha = Mathf.Lerp(0.04f, 0.32f, progress) * Random.Range(0.35f, 1f);

        if (noiseOverlay != null)
        {
            noiseOverlay.color = new Color(Random.Range(0.62f, 1f), Random.Range(0.70f, 1f), 1f, alpha);
        }

        for (int i = 0; i < glitchBars.Count; i++)
        {
            Image bar = glitchBars[i];
            if (bar == null)
            {
                continue;
            }

            RectTransform rect = bar.rectTransform;
            rect.anchoredPosition = new Vector2(Random.Range(-300f, 300f), Random.Range(-360f, 360f));
            rect.sizeDelta = new Vector2(1600f, Random.Range(10f, Mathf.Lerp(26f, 90f, progress)));
            bar.color = new Color(
                i % 2 == 0 ? 0.35f : 1f,
                Random.Range(0.55f, 1f),
                i % 2 == 0 ? 1f : 0.72f,
                Random.Range(0f, alpha));
        }
    }

    private GameObject CreateRect(string objectName, Vector2 anchorCenter, Vector2 size, Color color)
    {
        GameObject gameObject = new GameObject(objectName);
        gameObject.transform.SetParent(canvas.transform, false);

        Image image = gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;

        RectTransform rect = image.rectTransform;
        rect.anchorMin = anchorCenter;
        rect.anchorMax = anchorCenter;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;

        if (!objectName.Contains("Overlay") && !objectName.Contains("Dialogue"))
        {
            roomObjects.Add(gameObject);
        }

        return gameObject;
    }

    private Text CreateText(string objectName, Transform parent, int fontSize, FontStyle fontStyle, Color color)
    {
        GameObject textObject = new GameObject(objectName);
        textObject.transform.SetParent(parent, false);

        Text text = textObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.color = color;
        text.alignment = TextAnchor.MiddleLeft;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        return text;
    }

    private void SetDialogueVisible(bool visible)
    {
        speakerText.transform.parent.gameObject.SetActive(visible);
    }
}
