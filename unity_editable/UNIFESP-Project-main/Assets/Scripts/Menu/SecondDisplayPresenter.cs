using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Presentation only. The existing GRAZ and free LSL controllers remain the
// sole owners of protocol timing, classification, logging and marker output.
public class SecondDisplayPresenter : MonoBehaviour
{
    private enum Presentation { Waiting, Graz, FreeLsl, Completed, Aborted }
    private Presentation presentation = Presentation.Waiting;
    private bool open;
    private int monitorIndex = 1;
    private GameObject messageCanvas;
    private TMP_Text messageText;
    private Camera participantCamera;
    private readonly List<CanvasCopy> copies = new List<CanvasCopy>();
    private readonly Queue<string> markerHistory = new Queue<string>();
    private GUIStyle panelStyle;
    private GUIStyle labelStyle;
    private int totalTrials;

    public bool IsOpen => open;
    public int MonitorIndex => monitorIndex;

    private void OnEnable()
    {
        LSLMarkerOutlet.MarkerSent += RecordMarker;
    }

    private void OnDisable()
    {
        LSLMarkerOutlet.MarkerSent -= RecordMarker;
    }

    public bool Open(int requestedMonitor)
    {
        var monitors = SecondDisplayWindow.GetMonitors();
        if (!SecondDisplayWindow.Available || monitors.Length < 2)
            return false;

        monitorIndex = Mathf.Clamp(requestedMonitor, 0, monitors.Length - 1);
        RectInt screen = monitors[monitorIndex];
        int width = Mathf.Min(960, screen.width);
        int height = Mathf.Min(540, screen.height);
        int x = screen.x + (screen.width - width) / 2;
        int y = screen.y + (screen.height - height) / 2;
        if (!SecondDisplayWindow.Show(1, x, y, width, height))
            return false;

        open = true;
        EnsureMessageCanvas();
        ApplyPresentation();
        return true;
    }

    public void Close()
    {
        open = false;
        ClearCopies();
        if (messageCanvas != null)
            Destroy(messageCanvas);
        messageCanvas = null;
        messageText = null;
        if (participantCamera != null)
            Destroy(participantCamera.gameObject);
        participantCamera = null;
        SecondDisplayWindow.Hide();
    }

    public void SetWaiting()
    {
        presentation = Presentation.Waiting;
        ApplyPresentation();
    }

    public void SetGraz(int trialCount)
    {
        totalTrials = trialCount;
        markerHistory.Clear();
        presentation = Presentation.Graz;
        ApplyPresentation();
    }

    public void SetFreeLsl()
    {
        markerHistory.Clear();
        presentation = Presentation.FreeLsl;
        ApplyPresentation();
    }

    public void SetCompleted(bool aborted)
    {
        presentation = aborted ? Presentation.Aborted : Presentation.Completed;
        ApplyPresentation();
    }

    public void RecordMarker(int code, string label)
    {
        markerHistory.Enqueue(code + " " + label);
        while (markerHistory.Count > 6)
            markerHistory.Dequeue();
    }

    private void Update()
    {
        if (!open)
            return;

        if (presentation == Presentation.Graz)
        {
            EnsureCopy("GRAZ Subject Stimulus Display");
            EnsureCopy("Photo Feedback Overlay");
        }
        else if (presentation == Presentation.FreeLsl)
        {
            EnsureParticipantCamera();
            EnsureCopy("Photo Feedback Overlay");
        }
    }

    private void LateUpdate()
    {
        if (SecondDisplayWindow.WasActivated)
        {
            SecondDisplayWindow.EnsurePrimaryWindowChrome();
            SecondDisplayWindow.EnsureSecondaryWindowChrome();
        }
        if (SecondDisplayWindow.ConsumeCloseRequest())
        {
            var menu = GetComponent<Experiment2RedesignedMenu>();
            if (menu != null)
                menu.OnSecondDisplayClosedByUser();
            else
                Close();
        }
        if (!open)
            return;
        foreach (CanvasCopy copy in copies)
            copy.Sync();
        if (participantCamera != null && Camera.main != null)
        {
            participantCamera.CopyFrom(Camera.main);
            participantCamera.targetDisplay = 1;
            participantCamera.targetTexture = null;
            participantCamera.depth = Camera.main.depth - 10f;
            participantCamera.transform.SetPositionAndRotation(Camera.main.transform.position, Camera.main.transform.rotation);
        }
    }

    private void ApplyPresentation()
    {
        ClearCopies();
        if (participantCamera != null)
            participantCamera.gameObject.SetActive(open && presentation == Presentation.FreeLsl);
        if (!open)
            return;
        EnsureMessageCanvas();
        bool showMessage = presentation != Presentation.Graz && presentation != Presentation.FreeLsl;
        messageCanvas.SetActive(showMessage);
        if (showMessage)
            messageText.text = presentation == Presentation.Completed
                ? "Sessão concluída. Aguarde o experimentador."
                : presentation == Presentation.Aborted
                    ? "Sessão interrompida. Aguarde o experimentador."
                    : "Aguarde a configuração por parte do experimentador";
    }

    private void EnsureMessageCanvas()
    {
        if (messageCanvas != null)
            return;
        messageCanvas = new GameObject("Participant Waiting Screen");
        var canvas = messageCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.targetDisplay = 1;
        canvas.sortingOrder = 30000;
        var background = new GameObject("Background", typeof(RectTransform), typeof(Image));
        background.transform.SetParent(messageCanvas.transform, false);
        var rect = (RectTransform)background.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        background.GetComponent<Image>().color = new Color(0.78f, 0.78f, 0.78f, 1f);
        var textObject = new GameObject("Instruction", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(messageCanvas.transform, false);
        var textRect = (RectTransform)textObject.transform;
        textRect.anchorMin = new Vector2(0.1f, 0.2f);
        textRect.anchorMax = new Vector2(0.9f, 0.8f);
        textRect.offsetMin = textRect.offsetMax = Vector2.zero;
        messageText = textObject.GetComponent<TextMeshProUGUI>();
        messageText.alignment = TextAlignmentOptions.Center;
        messageText.fontSize = 48;
        messageText.color = Color.black;
        messageText.enableAutoSizing = true;
        messageText.fontSizeMin = 24;
        messageText.fontSizeMax = 48;
    }

    private void EnsureParticipantCamera()
    {
        if (participantCamera != null || Camera.main == null)
            return;
        participantCamera = new GameObject("Participant Scene Camera").AddComponent<Camera>();
        participantCamera.CopyFrom(Camera.main);
        participantCamera.targetDisplay = 1;
        participantCamera.targetTexture = null;
        participantCamera.depth = Camera.main.depth - 10f;
        participantCamera.tag = "Untagged";
    }

    private void EnsureCopy(string sourceName)
    {
        foreach (CanvasCopy copy in copies)
            if (copy.Source != null && copy.Source.name == sourceName)
                return;
        var source = GameObject.Find(sourceName);
        if (source == null || source.GetComponent<Canvas>() == null)
            return;
        copies.Add(new CanvasCopy(source));
    }

    private void ClearCopies()
    {
        foreach (CanvasCopy copy in copies)
            copy.Dispose();
        copies.Clear();
    }

    private void OnDestroy()
    {
        Close();
    }

    private void OnGUI()
    {
        if (!open || (presentation != Presentation.Graz && presentation != Presentation.FreeLsl))
            return;
        panelStyle ??= new GUIStyle(GUI.skin.box) { fontSize = 16, alignment = TextAnchor.UpperLeft, padding = new RectOffset(12, 12, 10, 10) };
        labelStyle ??= new GUIStyle(GUI.skin.label) { fontSize = 15, normal = { textColor = Color.white } };
        int width = Mathf.Min(390, Screen.width / 3);
        GUILayout.BeginArea(new Rect(Screen.width - width - 15, 15, width, 210), panelStyle);
        GUILayout.Label("Monitor do experimentador", labelStyle);
        var graz = GrazController.instance;
        if (presentation == Presentation.Graz && graz != null)
            GUILayout.Label("Fase: " + graz.CurrentPhase + " | Trial: " + graz.CurrentTrial + "/" + totalTrials, labelStyle);
        else
            GUILayout.Label("Controle livre LSL", labelStyle);
        var outlet = LSLMarkerOutlet.Instance;
        GUILayout.Label("Último marker Unity: " + (outlet != null && outlet.SentMarkersCount > 0 ? outlet.LastMarkerCode + " " + outlet.LastMarkerLabel : "nenhum"), labelStyle);
        GUILayout.Label("Histórico de saída Unity:", labelStyle);
        foreach (string marker in markerHistory)
            GUILayout.Label(marker, labelStyle);
        GUILayout.EndArea();
    }

    private sealed class CanvasCopy
    {
        public readonly GameObject Source;
        private readonly GameObject duplicate;
        private readonly Transform[] originals;
        private readonly Transform[] mirrors;

        public CanvasCopy(GameObject source)
        {
            Source = source;
            duplicate = Object.Instantiate(source);
            duplicate.name = source.name + " Participant Copy";
            var canvas = duplicate.GetComponent<Canvas>();
            canvas.targetDisplay = 1;
            foreach (var raycaster in duplicate.GetComponentsInChildren<GraphicRaycaster>(true))
                raycaster.enabled = false;
            foreach (var selectable in duplicate.GetComponentsInChildren<Selectable>(true))
                selectable.interactable = false;
            originals = source.GetComponentsInChildren<Transform>(true);
            mirrors = duplicate.GetComponentsInChildren<Transform>(true);
            Sync();
        }

        public void Sync()
        {
            if (Source == null || duplicate == null)
                return;
            for (int i = 0; i < Mathf.Min(originals.Length, mirrors.Length); i++)
            {
                if (originals[i] == null || mirrors[i] == null)
                    continue;
                mirrors[i].gameObject.SetActive(originals[i].gameObject.activeSelf);
                if (originals[i] is RectTransform sourceRect && mirrors[i] is RectTransform mirrorRect)
                {
                    mirrorRect.anchorMin = sourceRect.anchorMin;
                    mirrorRect.anchorMax = sourceRect.anchorMax;
                    mirrorRect.anchoredPosition = sourceRect.anchoredPosition;
                    mirrorRect.sizeDelta = sourceRect.sizeDelta;
                    mirrorRect.localRotation = sourceRect.localRotation;
                    mirrorRect.localScale = sourceRect.localScale;
                }
                var sourceGraphic = originals[i].GetComponent<Graphic>();
                var mirrorGraphic = mirrors[i].GetComponent<Graphic>();
                if (sourceGraphic != null && mirrorGraphic != null)
                    mirrorGraphic.color = sourceGraphic.color;
                if (sourceGraphic is RawImage sourceRaw && mirrorGraphic is RawImage mirrorRaw)
                    mirrorRaw.texture = sourceRaw.texture;
                if (sourceGraphic is Image sourceImage && mirrorGraphic is Image mirrorImage)
                    mirrorImage.sprite = sourceImage.sprite;
                if (sourceGraphic is TMP_Text sourceText && mirrorGraphic is TMP_Text mirrorText)
                    mirrorText.text = sourceText.text;
            }
        }

        public void Dispose()
        {
            if (duplicate != null)
                Object.Destroy(duplicate);
        }
    }
}
