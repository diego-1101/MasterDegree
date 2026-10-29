using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using SimpleFileBrowser;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Experiment2RedesignedMenu : MonoBehaviour
{
    private enum ExperimentMode
    {
        Capture,
        FreeLsl,
        GrazAcquisition,
        GrazFeedback
    }

    private GameUIDois ui;
    private SecondDisplayPresenter secondDisplay;
    private bool secondDisplayEnabled;
    private bool initializingSecondDisplay;
    private int selectedMonitor = 1;
    private ExperimentMode activeMode = ExperimentMode.Capture;
    private Vector2 mainScroll;
    private Vector2 actionScroll;
    private bool stylesReady;
    private GUIStyle pageStyle;
    private GUIStyle panelStyle;
    private GUIStyle darkPanelStyle;
    private GUIStyle titleStyle;
    private GUIStyle subtitleStyle;
    private GUIStyle sectionTitleStyle;
    private GUIStyle bodyStyle;
    private GUIStyle selectedButtonStyle;
    private GUIStyle buttonStyle;
    private GUIStyle fieldLabelStyle;
    private GUIStyle statusStyle;
    private GUIStyle statusBoxStyle;
    private GUIStyle checklistDoneStyle;
    private GUIStyle checklistTodoStyle;
    private GUIStyle checklistTextStyle;
    private GUIStyle tooltipStyle;
    private GUIStyle textFieldStyle;
    private GUIStyle textAreaStyle;
    private GUIStyle toggleStyle;
    private GUIStyle toolbarStyle;
    private string status = "Pronto para configurar";
    private string hoveredTooltip = "";
    private float tooltipHoverStartTime;

    private string trialsText = "25";
    private string streamNameText = "Simulated_Leg_Classifier";
    private string streamTypeText = "Classifications";
    private string baselineText = "10";
    private string fixationText = "3";
    private string cueText = "1";
    private string taskText = "3.75";
    private string minPauseText = "1.5";
    private string maxPauseText = "3.5";
    private string gravityText = "9.71";
    private string toleranceText = "1";
    private string holdTimeText = "3";
    private string presetNameText = "Simple: one leg movement at different angles";
    private string subjectIdText = "S01";
    private string sessionIdText = "01";
    private string runText = "1";
    private string repetitionsPerClassText = "10";
    private string markerStreamNameText = "Unity_GRAZ_Markers";
    private string markerStreamTypeText = "Markers";
    private string randomSeedText = "12345";
    private bool shuffleTrials = true;
    private bool numericMarkersOnly = true;
    private List<EditableCueClass> cueClasses = new List<EditableCueClass>();
    private List<string> trialPreview = new List<string>();
    private List<GeneratedTrialRow> generatedTrialPlan = new List<GeneratedTrialRow>();
    private string lastPresetPath = "";
    private string lastTrialPlanPath = "";
    private bool fileBrowserOpen;
    private string markerTestStatus = "Outlet markers: nao publicado.";
    private string backgroundImagePath = "";
    private Color backgroundColor = new Color(0.78f, 0.78f, 0.78f, 1f);
    private string fixationImagePath = "";
    private Color fixationColor = Color.red;
    private string baselineImagePath = "";
    private string baselineStimulusText = "";
    private string taskImagePath = "";
    private string taskStimulusText = "";
    private string pauseImagePath = "";
    private string pauseStimulusText = "";
    private string cueScreenPercentText = "72";
    private Color cueColor = Color.white;
    private string fixationSizePercentText = "32";
    private Color baselineTextColor = Color.black;
    private string baselineTextSizePercentText = "7";
    private Color taskTextColor = Color.black;
    private string taskTextSizePercentText = "7";
    private Color pauseTextColor = Color.black;
    private string pauseTextSizePercentText = "7";
    private string maxMovementSpeedText = "60";
    private string inputStaleSecondsText = "1.25";
    private bool showFeedbackGauge = true;
    private bool showFeedbackSideView = true;
    private bool showFeedbackGhost = true;
    private bool showResultFeedback = true;
    private string successImagePath = "";
    private string successFeedbackText = "SUCESSO";
    private string successDurationText = "1";
    private string failureImagePath = "";
    private string failureFeedbackText = "TENTE NOVAMENTE";
    private string failureDurationText = "1";
    private Color successTextColor = new Color(0.08f, 0.48f, 0.18f, 1f);
    private string successTextSizePercentText = "8";
    private Color failureTextColor = new Color(0.78f, 0.08f, 0.08f, 1f);
    private string failureTextSizePercentText = "8";
    private bool showHoldProgressFeedback = true;
    private string classificationTestStatus = "Stream de entrada desconectada.";
    private int freeLslDisplaySides = 2;
    private bool freeLslShowGauge = true;
    private bool freeLslShowSideView = true;
    private bool freeLslShowGhost = false;
    private bool freeLslShowToleranceBand = true;
    private string freeLslLeftTargetText = "45";
    private string freeLslLeftToleranceText = "5";
    private string freeLslRightTargetText = "45";
    private string freeLslRightToleranceText = "5";
    private List<FreeLslCommandDefinition> freeLslCommands = new List<FreeLslCommandDefinition>();

    private const float ReferenceWidth = 1920f;
    private const float ReferenceHeight = 1080f;
    private const float TooltipDelaySeconds = 1f;
    private const int MarkerTestCode = 9999;
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    private const string CuePresetPrefsKey = "Experiment2.GrazCuePreset";
    private const string SecondDisplayPrefsKey = "Experiment2.SecondDisplayEnabled";
    private const string SecondDisplayMonitorPrefsKey = "Experiment2.SecondDisplayMonitor";
    private const int CurrentPresetSchemaVersion = 5;

    public void Initialize(GameUIDois owner)
    {
        ui = owner;
        secondDisplay = owner.GetComponent<SecondDisplayPresenter>();
        if (secondDisplay == null)
            secondDisplay = owner.gameObject.AddComponent<SecondDisplayPresenter>();
        initializingSecondDisplay = true;
        bool persistedSecondDisplay = PlayerPrefs.GetInt(SecondDisplayPrefsKey, 0) != 0;
        selectedMonitor = PlayerPrefs.GetInt(SecondDisplayMonitorPrefsKey, 1);
        HideLegacyConfigPanel();
        PullValuesFromLegacyFields();
        LoadCuePresetFromPrefs(false);
        initializingSecondDisplay = false;
        SetSecondDisplayEnabled(persistedSecondDisplay);
        EnsureDefaultCueClasses();
        EnsureDefaultFreeLslCommands();
    }

    private void SetSecondDisplayEnabled(bool enabledForParticipant)
    {
        secondDisplayEnabled = enabledForParticipant;
        if (initializingSecondDisplay)
            return;

        PlayerPrefs.SetInt(SecondDisplayPrefsKey, secondDisplayEnabled ? 1 : 0);
        PlayerPrefs.Save();
        if (!secondDisplayEnabled)
        {
            secondDisplay?.Close();
            status = "Segunda tela desligada.";
            return;
        }

        if (secondDisplay == null || !secondDisplay.Open(selectedMonitor))
            status = Application.platform == RuntimePlatform.WindowsEditor
                ? "A segunda janela requer um build Windows com dois monitores."
                : "Sem segundo monitor disponivel: conecte outro monitor antes de iniciar.";
        else
            status = "Janela do participante aberta. Escolha o monitor ou arraste pelo titulo.";
    }

    public void OnSecondDisplayClosedByUser()
    {
        SetSecondDisplayEnabled(false);
    }

    private void HideLegacyConfigPanel()
    {
        if (ui == null || ui.painelConfigs == null)
            return;

        ui.painelConfigs.alpha = 0f;
        ui.painelConfigs.interactable = false;
        ui.painelConfigs.blocksRaycasts = false;
    }

    public void ShowAfterPhotoMode()
    {
        enabled = true;
        HideLegacyConfigPanel();
        PullValuesFromLegacyFields();
        status = "Modo camera encerrado.";
    }

    public void ShowFromExperimentSelection()
    {
        enabled = true;
        activeMode = ExperimentMode.Capture;
        mainScroll = Vector2.zero;
        actionScroll = Vector2.zero;
        HideLegacyConfigPanel();
        PullValuesFromLegacyFields();
        status = "Pronto para configurar";
        SetSecondDisplayEnabled(secondDisplayEnabled);
    }

    private void PullValuesFromLegacyFields()
    {
        if (ui == null)
            return;

        trialsText = ui.trialsField != null ? ui.trialsField.text : trialsText;
        baselineText = ui._baselineField != null ? ui._baselineField.text : baselineText;
        fixationText = ui._fixationField != null ? ui._fixationField.text : fixationText;
        cueText = GrazController.instance != null ? GrazController.instance.cueDuration.ToString(CultureInfo.InvariantCulture) : cueText;
        taskText = ui._feedbackField != null ? ui._feedbackField.text : taskText;
        minPauseText = ui._minPauseField != null ? ui._minPauseField.text : minPauseText;
        maxPauseText = ui._maxPauseField != null ? ui._maxPauseField.text : maxPauseText;

        var gravidade = PrivateInput("gravidadeField");
        var margem = PrivateInput("margemField");
        var tempo = PrivateInput("tempoPrecisaoField");
        gravityText = gravidade != null ? gravidade.text : gravityText;
        toleranceText = margem != null ? margem.text : toleranceText;
        holdTimeText = tempo != null ? tempo.text : holdTimeText;
    }

    [Serializable]
    private class EditableCueClass
    {
        public bool enabled = true;
        public string classId;
        public string label;
        public string markerCode;
        public string cueImagePath;
        public string description;
        public string targetAngle;
        public string tolerance;
        public string holdTime;
        public GrazLegSide legSide;

        public EditableCueClass(string id, string label, int marker, float targetAngle, string description)
        {
            classId = id;
            this.label = label;
            markerCode = marker.ToString();
            cueImagePath = "";
            this.description = description;
            this.targetAngle = targetAngle.ToString("0");
            tolerance = "5";
            holdTime = "3";
            legSide = id.IndexOf("left", StringComparison.OrdinalIgnoreCase) >= 0
                ? GrazLegSide.Left
                : id.IndexOf("relax", StringComparison.OrdinalIgnoreCase) >= 0
                    ? GrazLegSide.Relaxed
                    : GrazLegSide.Right;
        }
    }

    [Serializable]
    private class EditableCuePresetData
    {
        public int schemaVersion;
        public bool secondDisplayEnabled;
        public string presetName;
        public string subjectId;
        public string sessionId;
        public string run;
        public string repetitionsPerClass;
        public string baselineDuration;
        public string fixationDuration;
        public string cueDuration;
        public string taskDuration;
        public string minPauseDuration;
        public string maxPauseDuration;
        public string randomSeed;
        public bool shuffle;
        public string markerStreamName;
        public string markerStreamType;
        public bool numericOnly;
        public string backgroundImagePath;
        public string backgroundColorHex;
        public string fixationImagePath;
        public string fixationColorHex;
        public string baselineImagePath;
        public string baselineText;
        public string taskImagePath;
        public string taskText;
        public string pauseImagePath;
        public string pauseText;
        public string cueScreenPercent;
        public string cueColorHex;
        public string fixationSizePercent;
        public string baselineTextColorHex;
        public string baselineTextSizePercent;
        public string taskTextColorHex;
        public string taskTextSizePercent;
        public string pauseTextColorHex;
        public string pauseTextSizePercent;
        public string inputStreamName;
        public string inputStreamType;
        public string gravity;
        public string defaultTolerance;
        public string defaultHoldTime;
        public string maxMovementSpeed;
        public string inputStaleSeconds;
        public bool showFeedbackGauge;
        public bool showFeedbackSideView;
        public bool showFeedbackGhost;
        public bool showResultFeedback;
        public string successImagePath;
        public string successText;
        public string successDuration;
        public string failureImagePath;
        public string failureText;
        public string failureDuration;
        public string successTextColorHex;
        public string successTextSizePercent;
        public string failureTextColorHex;
        public string failureTextSizePercent;
        public bool showHoldProgressFeedback;
        public int freeLslDisplaySides;
        public bool freeLslShowGauge;
        public bool freeLslShowSideView;
        public bool freeLslShowGhost;
        public bool freeLslShowToleranceBand;
        public string freeLslLeftTarget;
        public string freeLslLeftTolerance;
        public string freeLslRightTarget;
        public string freeLslRightTolerance;
        public List<FreeLslCommandDefinition> freeLslCommands;
        public List<EditableCueClass> classes;
    }

    [Serializable]
    private class GeneratedTrialRow
    {
        public int trialIndex;
        public string subjectId;
        public string sessionId;
        public string run;
        public string classId;
        public string label;
        public int markerCode;
        public string cueImagePath;
        public string description;
        public float targetAngle;
        public float tolerance;
        public float holdTime;
        public GrazLegSide legSide;
        public int randomSeed;
        public bool shuffled;
    }

    [Serializable]
    private class GeneratedTrialPlanData
    {
        public string presetName;
        public string subjectId;
        public string sessionId;
        public string run;
        public string generatedAt;
        public int repetitionsPerClass;
        public int randomSeed;
        public bool shuffled;
        public List<GeneratedTrialRow> trials;
    }

    private void EnsureDefaultCueClasses()
    {
        if (cueClasses.Count > 0)
            return;

        cueClasses.Add(new EditableCueClass("right_90", "direita 90", 1000, 90, "Perna direita em 90 graus."));
        cueClasses.Add(new EditableCueClass("right_60", "direita 60", 1001, 60, "Perna direita em 60 graus."));
        cueClasses.Add(new EditableCueClass("right_45", "direita 45", 1002, 45, "Perna direita em 45 graus."));
        cueClasses.Add(new EditableCueClass("right_30", "direita 30", 1003, 30, "Perna direita em 30 graus."));
        cueClasses.Add(new EditableCueClass("relaxed", "relaxado", 1004, 0, "Estado relaxado/sem movimento."));
    }

    private void EnsureDefaultFreeLslCommands()
    {
        if (freeLslCommands.Count > 0)
            return;

        freeLslCommands.Add(new FreeLslCommandDefinition(1000, GrazLegSide.Right, 90f));
        freeLslCommands.Add(new FreeLslCommandDefinition(1001, GrazLegSide.Right, 60f));
        freeLslCommands.Add(new FreeLslCommandDefinition(1002, GrazLegSide.Right, 45f));
        freeLslCommands.Add(new FreeLslCommandDefinition(1003, GrazLegSide.Right, 30f));
        freeLslCommands.Add(new FreeLslCommandDefinition(1004, GrazLegSide.Relaxed, 0f));
        freeLslCommands.Add(new FreeLslCommandDefinition(1010, GrazLegSide.Left, 90f));
        freeLslCommands.Add(new FreeLslCommandDefinition(1011, GrazLegSide.Left, 60f));
        freeLslCommands.Add(new FreeLslCommandDefinition(1012, GrazLegSide.Left, 45f));
        freeLslCommands.Add(new FreeLslCommandDefinition(1013, GrazLegSide.Left, 30f));
    }

    private void RestoreDefaultFreeLslCommands()
    {
        freeLslCommands.Clear();
        EnsureDefaultFreeLslCommands();
        status = "Mapeamento padrao do controle livre restaurado.";
    }

    private void ResetDefaultCueClasses()
    {
        cueClasses.Clear();
        EnsureDefaultCueClasses();
        repetitionsPerClassText = "10";
        randomSeedText = "12345";
        shuffleTrials = true;
        trialPreview.Clear();
        status = "Preset simples restaurado.";
    }

    private void SaveCuePresetToPrefs()
    {
        if (activeMode == ExperimentMode.FreeLsl)
        {
            if (!HasClassifierStream())
            {
                status = "Preencha nome e tipo da stream de entrada antes de salvar.";
                return;
            }
            if (!HasValidFreeLslVisualSettings() || !HasValidFreeLslMovementSettings())
            {
                status = "Corrija feedback visual e parametros de movimento antes de salvar.";
                return;
            }
            if (!HasValidFreeLslCommands(out string freeCommandMessage))
            {
                status = freeCommandMessage;
                return;
            }
        }
        else if (!ValidateCueClasses(out string validationMessage))
        {
            status = validationMessage;
            return;
        }

        PlayerPrefs.SetString(CuePresetPrefsKey, JsonUtility.ToJson(BuildPresetData()));
        PlayerPrefs.Save();
        status = "Preset padrao salvo.";
    }

    private void LoadCuePresetFromPrefs(bool updateStatus)
    {
        if (!PlayerPrefs.HasKey(CuePresetPrefsKey))
        {
            if (updateStatus)
                status = "Nenhum preset salvo ainda.";
            return;
        }

        var json = PlayerPrefs.GetString(CuePresetPrefsKey);
        var data = JsonUtility.FromJson<EditableCuePresetData>(json);
        if (data == null)
        {
            if (updateStatus)
                status = "Preset salvo invalido.";
            return;
        }

        ApplyPresetData(data);
        if (updateStatus)
            status = "Preset padrao carregado.";
    }

    private void OnGUI()
    {
        if (ui == null)
            return;
        if (ShouldHideForPhotoMode())
            return;
        if (fileBrowserOpen || FileBrowser.IsOpen)
            return;

        EnsureStyles();

        var previousMatrix = GUI.matrix;
        ApplyScaledGuiMatrix();

        try
        {
            // IMGUI can retain the previous control's tooltip between events.
            // Start each pass without a tooltip so leaving a control hides it.
            GUI.tooltip = string.Empty;
            DrawScaledMenu();
            DrawTooltip();
        }
        finally
        {
            GUI.matrix = previousMatrix;
        }
    }

    private void DrawScaledMenu()
    {
        float margin = ReferenceWidth * 0.035f;
        float top = ReferenceHeight * 0.035f;
        var page = new Rect(margin, top, ReferenceWidth - margin * 2f, ReferenceHeight - top * 2f);

        GUILayout.BeginArea(page, pageStyle);
        DrawHeader();

        GUILayout.Space(12f);
        GUILayout.BeginHorizontal();
        DrawModeColumn(GUILayout.Width(page.width * 0.24f), GUILayout.ExpandHeight(true));
        GUILayout.Space(16f);
        DrawMainColumn(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        GUILayout.Space(16f);
        DrawActionColumn(GUILayout.Width(page.width * 0.25f), GUILayout.ExpandHeight(true));
        GUILayout.EndHorizontal();
        GUILayout.EndArea();
    }

    private void ApplyScaledGuiMatrix()
    {
        float scale = Mathf.Min(Screen.width / ReferenceWidth, Screen.height / ReferenceHeight);
        float offsetX = (Screen.width - ReferenceWidth * scale) * 0.5f;
        float offsetY = (Screen.height - ReferenceHeight * scale) * 0.5f;
        GUI.matrix = Matrix4x4.TRS(new Vector3(offsetX, offsetY, 0f), Quaternion.identity, new Vector3(scale, scale, 1f));
    }

    private void DrawHeader()
    {
        GUILayout.BeginHorizontal();
        GUILayout.BeginVertical();
        GUILayout.Label("Experimento 2", titleStyle);
        GUILayout.Label("Menu do operador para configurar captura, LSL, GRAZ de aquisicao e GRAZ com feedback.", subtitleStyle);
        GUILayout.EndVertical();
        GUILayout.FlexibleSpace();
        DrawStatusBox(GUILayout.Width(520f), GUILayout.Height(78f));
        GUILayout.EndHorizontal();
    }

    private void DrawStatusBox(params GUILayoutOption[] options)
    {
        var previousColor = GUI.backgroundColor;
        GUI.backgroundColor = GetStatusColor();
        GUILayout.BeginVertical(statusBoxStyle, options);
        GUILayout.Label("Status", fieldLabelStyle);
        GUILayout.Label(status, bodyStyle);
        GUILayout.EndVertical();
        GUI.backgroundColor = previousColor;
    }

    private Color GetStatusColor()
    {
        string text = status == null ? "" : status.ToLowerInvariant();
        if (text.Contains("inval") || text.Contains("erro") || text.Contains("sem ") || text.Contains("nao encontrado") || text.Contains("duplicado"))
            return new Color(0.75f, 0.18f, 0.14f, 0.95f);
        if (text.Contains("cancel") || text.Contains("interromp"))
            return new Color(0.86f, 0.51f, 0.12f, 0.95f);
        if (text.Contains("conclu") || text.Contains("salvo") || text.Contains("aplicad") || text.Contains("valida") || text.Contains("gerada"))
            return new Color(0.12f, 0.48f, 0.26f, 0.95f);
        return new Color(0.05f, 0.42f, 0.46f, 0.95f);
    }

    private void DrawModeColumn(params GUILayoutOption[] options)
    {
        GUILayout.BeginVertical(darkPanelStyle, options);
        GUILayout.Label("Fluxo", sectionTitleStyle);
        ModeButton(ExperimentMode.Capture, "1. Captura de imagens", "prints/cues sem LSL");
        ModeButton(ExperimentMode.FreeLsl, "2. Controle livre LSL", "familiarizacao");
        ModeButton(ExperimentMode.GrazAcquisition, "3. GRAZ aquisicao", "treino EEG/EMG");
        ModeButton(ExperimentMode.GrazFeedback, "4. GRAZ feedback", "teste com alvo");

        GUILayout.Space(10f);
        GUILayout.Label("Escolha primeiro o modo. Depois configure os blocos que aparecem no centro.", bodyStyle);
        GUILayout.Space(12f);
        bool requestedSecondDisplay = GUILayout.Toggle(secondDisplayEnabled,
            new GUIContent("Segunda tela do participante", "Abre uma janela independente para o participante. Requer dois monitores em um build Windows; escolha o monitor abaixo ou arraste a janela pelo titulo."), toggleStyle);
        if (requestedSecondDisplay != secondDisplayEnabled)
            SetSecondDisplayEnabled(requestedSecondDisplay);
        var monitors = SecondDisplayWindow.GetMonitors();
        if (secondDisplayEnabled)
        {
            for (int i = 0; i < monitors.Length; i++)
            {
                int monitor = i;
                if (TooltipButton((selectedMonitor == i ? "● " : "○ ") + "Monitor " + (i + 1),
                    "Posiciona a janela no monitor escolhido. Tambem e possivel arrasta-la com o mouse.", buttonStyle, GUILayout.Height(44f)))
                {
                    selectedMonitor = monitor;
                    PlayerPrefs.SetInt(SecondDisplayMonitorPrefsKey, selectedMonitor);
                    PlayerPrefs.Save();
                    if (secondDisplay != null && !secondDisplay.Open(selectedMonitor))
                        status = "Nao foi possivel abrir a janela do participante.";
                }
            }
        }
        GUILayout.FlexibleSpace();

        if (TooltipButton("Voltar aos experimentos", "Fecha o menu do Experimento 2 e volta para a tela de escolha entre Experimento 1 e Experimento 2.", buttonStyle, GUILayout.Height(50f)))
        {
            ReturnToExperimentSelection();
            GUIUtility.ExitGUI();
        }

        GUILayout.Space(6f);

        if (TooltipButton("Mostrar menu antigo", "Abre o menu original da empresa. Use apenas para comparar ou acessar alguma rota antiga que ainda nao migramos.", buttonStyle, GUILayout.Height(50f)))
            RestoreLegacyPanel();
        GUILayout.EndVertical();
    }

    private void ModeButton(ExperimentMode mode, string title, string subtitle)
    {
        var style = activeMode == mode ? selectedButtonStyle : buttonStyle;
        if (TooltipButton(title + "\n" + subtitle, GetModeTooltip(mode), style, GUILayout.Height(80f)))
        {
            activeMode = mode;
            mainScroll = Vector2.zero;
            status = "Modo selecionado: " + title;
        }
    }

    private void DrawMainColumn(params GUILayoutOption[] options)
    {
        GUILayout.BeginVertical(panelStyle, options);
        DrawModeSummary();
        GUILayout.Space(8f);
        mainScroll = GUILayout.BeginScrollView(mainScroll);

        if (activeMode == ExperimentMode.Capture)
            DrawCaptureMode();
        else if (activeMode == ExperimentMode.FreeLsl)
            DrawFreeLslMode();
        else if (activeMode == ExperimentMode.GrazAcquisition)
            DrawGrazAcquisitionMode();
        else
            DrawGrazFeedbackMode();

        GUILayout.EndScrollView();
        GUILayout.EndVertical();
    }

    private void DrawModeSummary()
    {
        string title;
        string summary;

        if (activeMode == ExperimentMode.Capture)
        {
            title = "Captura de imagens";
            summary = "Ajuste camera, perna, vista lateral e feedback visual para gerar imagens de cue/apresentacao.";
        }
        else if (activeMode == ExperimentMode.FreeLsl)
        {
            title = "Controle livre LSL";
            summary = "Modo de familiarizacao para o usuario testar controle da protese via classificador.";
        }
        else if (activeMode == ExperimentMode.GrazAcquisition)
        {
            title = "GRAZ aquisicao";
            summary = "Protocolo limpo para EEG/EMG: fixation, cue, task, pause, markers, shuffle e log.";
        }
        else
        {
            title = "GRAZ feedback/teste";
            summary = "Cue + LSL + feedback da perna, com alvo, tolerancia, tempo no alvo e gravidade.";
        }

        GUILayout.Label(title, titleStyle);
        GUILayout.Label(summary, subtitleStyle);
    }

    private void DrawCaptureMode()
    {
        Card("Modo camera", "Ferramenta do experimentador, independente de LSL.");
        GUILayout.Label("Use este modo para gerar imagens das cues. O print nao precisa existir antes do GRAZ; depois a imagem sera escolhida no editor de classes.", bodyStyle);
        if (TooltipButton("Abrir modo camera", "Entra no modo de captura para ajustar cameras, posicoes das pernas, feedback visual e tirar prints das cues.", buttonStyle, GUILayout.Height(48f)))
        {
            var photoMode = ui.GetComponent<PhotoMode>();
            if (photoMode != null)
            {
                photoMode.EntryPhotoMode();
                GUIUtility.ExitGUI();
            }
        }

        Future("Biblioteca de cues", "Listar screenshots salvos, renomear por classe e anexar ao preset.");
        Future("Presets de camera", "Salvar composicoes de camera/perna/feedback por classe.");
    }

    private void DrawFreeLslMode()
    {
        Card("Entrada LSL do classificador", "Este modo recebe classificacoes continuamente e nao executa nenhuma fase do GRAZ.");
        GUILayout.Label("Formato esperado: canal 0 = codigo discreto da classe; canal 1 = probabilidade de 0 a 1.", bodyStyle);
        streamNameText = Field("Stream entrada", streamNameText, "nome publicado pelo classificador");
        streamTypeText = Field("Tipo entrada", streamTypeText, "use Classifications");
        DrawClassificationInputPanel();

        DrawFreeLslVisualSettings();
        DrawFreeLslMovementSettings();
        DrawFreeLslCommandMap();

        Card("Execucao livre", "Ao iniciar, o menu desaparece e os feedbacks escolhidos permanecem visiveis o tempo todo. Pressione ESC para encerrar e voltar ao menu.");
    }

    private void DrawFreeLslVisualSettings()
    {
        Card("Feedback visual continuo", "Escolha os lados e os elementos que ficarao visiveis durante toda a familiarizacao.");
        GUILayout.Label("Lados exibidos", fieldLabelStyle);
        freeLslDisplaySides = GUILayout.Toolbar(freeLslDisplaySides, new[] { "Direita", "Esquerda", "Ambas" }, toolbarStyle, GUILayout.Height(46f));

        GUILayout.Space(6f);
        freeLslShowGauge = GUILayout.Toggle(freeLslShowGauge, "Barra de angulo 0-90", toggleStyle);
        freeLslShowSideView = GUILayout.Toggle(freeLslShowSideView, "Vista lateral", toggleStyle);
        freeLslShowGhost = GUILayout.Toggle(freeLslShowGhost, "Perna fantasma no alvo", toggleStyle);
        if (freeLslShowGhost)
            freeLslShowSideView = true;
        freeLslShowToleranceBand = GUILayout.Toggle(freeLslShowToleranceBand, "Mostrar area verde de alvo +/- tolerancia", toggleStyle);
        GUILayout.Label("Desligar a area verde nao remove a barra, o marcador vermelho, a vista lateral nem a perna fantasma.", checklistTextStyle);

        bool showLeft = FreeLslShowsLeft();
        bool showRight = FreeLslShowsRight();
        if (showLeft)
        {
            GUILayout.Space(6f);
            GUILayout.Label("Referencia esquerda", sectionTitleStyle);
            freeLslLeftTargetText = Field("Alvo esquerdo (graus)", freeLslLeftTargetText, "centro da faixa verde e posicao da perna fantasma");
            if (freeLslShowToleranceBand)
                freeLslLeftToleranceText = Field("Tolerancia esquerda", freeLslLeftToleranceText, "largura da faixa verde em +/- graus");
        }
        if (showRight)
        {
            GUILayout.Space(6f);
            GUILayout.Label("Referencia direita", sectionTitleStyle);
            freeLslRightTargetText = Field("Alvo direito (graus)", freeLslRightTargetText, "centro da faixa verde e posicao da perna fantasma");
            if (freeLslShowToleranceBand)
                freeLslRightToleranceText = Field("Tolerancia direita", freeLslRightToleranceText, "largura da faixa verde em +/- graus");
        }
    }

    private void DrawFreeLslMovementSettings()
    {
        Card("Resposta das pernas", "A classe define lado e angulo; a probabilidade recebida controla a velocidade de aproximacao.");
        gravityText = Field("Queda/gravid. (graus/s)", gravityText, "velocidade de retorno quando nao ha comando recente");
        maxMovementSpeedText = Field("Velocidade max. (graus/s)", maxMovementSpeedText, "multiplicada pela probabilidade recebida");
        inputStaleSecondsText = Field("Timeout entrada (s)", inputStaleSecondsText, "apos esse tempo sem amostra, aplica queda");
    }

    private void DrawFreeLslCommandMap()
    {
        Card("Mapa de comandos LSL", "Converte cada codigo recebido em lado e angulo. Nao cria cue, trial ou marker GRAZ.");
        GUILayout.Label("Os codigos padrao continuam como fallback. Linhas personalizadas com o mesmo codigo substituem o padrao neste modo.", bodyStyle);

        GUILayout.BeginHorizontal();
        if (TooltipButton("Adicionar comando", "Adiciona um novo codigo discreto para controlar uma perna e um angulo.", buttonStyle, GUILayout.Height(44f)))
            freeLslCommands.Add(new FreeLslCommandDefinition(1020 + freeLslCommands.Count, GrazLegSide.Right, 45f));
        if (TooltipButton("Restaurar codigos padrao", "Restaura os comandos 1000-1004 e 1010-1013.", buttonStyle, GUILayout.Height(44f)))
            RestoreDefaultFreeLslCommands();
        GUILayout.EndHorizontal();

        GUILayout.Space(6f);
        for (int i = 0; i < freeLslCommands.Count; i++)
        {
            FreeLslCommandDefinition command = freeLslCommands[i];
            GUILayout.BeginHorizontal(GUI.skin.box);
            command.enabled = GUILayout.Toggle(command.enabled, "", toggleStyle, GUILayout.Width(26f));
            GUILayout.Label("Codigo", fieldLabelStyle, GUILayout.Width(76f));
            command.code = GUILayout.TextField(command.code, textFieldStyle, GUILayout.Width(76f), GUILayout.Height(38f));
            int sideIndex = command.legSide == GrazLegSide.Right ? 0 : command.legSide == GrazLegSide.Left ? 1 : 2;
            sideIndex = GUILayout.Toolbar(sideIndex, new[] { "D", "E", "Relaxar" }, toolbarStyle, GUILayout.Width(246f), GUILayout.Height(38f));
            command.legSide = sideIndex == 0 ? GrazLegSide.Right : sideIndex == 1 ? GrazLegSide.Left : GrazLegSide.Relaxed;
            GUILayout.Label("Angulo", fieldLabelStyle, GUILayout.Width(76f));
            command.targetAngle = GUILayout.TextField(command.targetAngle, textFieldStyle, GUILayout.Width(76f), GUILayout.Height(38f));
            if (TooltipButton("X", "Remove este comando do mapeamento do controle livre.", buttonStyle, GUILayout.Width(32f), GUILayout.Height(38f)))
            {
                freeLslCommands.RemoveAt(i);
                GUILayout.EndHorizontal();
                break;
            }
            GUILayout.EndHorizontal();
        }
    }

    private void DrawGrazAcquisitionMode()
    {
        DrawSessionAndLsl(false);
        DrawGrazTimings();
        DrawSubjectStimulusSettings();
        DrawCueMarkers(false);
        Card("Tela do sujeito", "Aquisicao limpa: sem painel tecnico e sem feedback de desempenho.");
    }

    private void DrawGrazFeedbackMode()
    {
        DrawSessionAndLsl(true);
        DrawGrazTimings();
        DrawSubjectStimulusSettings();
        DrawFeedbackVisualSettings();
        DrawCueMarkers(true);
        DrawFeedbackCriteria();
    }

    private void DrawSessionAndLsl(bool classifier)
    {
        Card("Sessao e LSL", classifier ? "Identificacao, log e stream do classificador." : "Identificacao, log e markers do protocolo.");
        subjectIdText = Field("Sujeito", subjectIdText, "entra no log e markers");
        sessionIdText = Field("Sessao", sessionIdText, "sessao/bloco experimental");
        runText = Field("Run", runText, "indice do run atual");
        UpdatePlannedTrialsText();
        GUILayout.BeginHorizontal();
        GUILayout.Label("Trials planejados", fieldLabelStyle, GUILayout.Width(190f));
        GUILayout.Label(trialsText + " (" + CountActiveCueClasses() + " classes x " + ParsePositiveInt(repetitionsPerClassText, 1) + " repeticoes/classe)", bodyStyle);
        GUILayout.EndHorizontal();
        if (classifier)
        {
            GUILayout.Space(8f);
            GUILayout.Label("Entrada LSL - Unity recebe do classificador", sectionTitleStyle);
            GUILayout.Label("Formato esperado: canal 0 = codigo discreto da classe; canal 1 = probabilidade de 0 a 1.", bodyStyle);
            streamNameText = Field("Stream entrada", streamNameText, "nome publicado pelo classificador");
            streamTypeText = Field("Tipo entrada", streamTypeText, "use Classifications");
            DrawClassificationInputPanel();
        }

        GUILayout.Space(8f);
        GUILayout.Label("Saida LSL - Unity envia markers", sectionTitleStyle);
        GUILayout.Label("Este outlet envia markers das fases e o codigo da classe para sincronizacao e pos-processamento.", bodyStyle);
        markerStreamNameText = Field("Stream markers", markerStreamNameText, "outlet enviado pelo Unity");
        markerStreamTypeText = Field("Tipo markers", markerStreamTypeText, "normalmente Markers");
        numericMarkersOnly = GUILayout.Toggle(numericMarkersOnly, "Enviar pelo LSL apenas codigo numerico", toggleStyle);
        DrawMarkerTestPanel();

        GUILayout.BeginHorizontal();
        if (TooltipButton("Aplicar campos", "Aplica sujeito, sessao, run, streams LSL e configuracao do logger. Tambem cria/configura o outlet de markers.", buttonStyle, GUILayout.Height(44f)))
            ApplySessionFields();
        if (TooltipButton("Escolher log", "Escolhe a pasta onde os arquivos de log da sessao serao salvos.", buttonStyle, GUILayout.Height(44f)))
        {
            StartCoroutine(SelectLogDirectoryCoroutine());
            GUIUtility.ExitGUI();
        }
        if (TooltipButton("Carregar JSON antigo", "Carrega configuracoes no formato antigo usado pelo menu original da empresa.", buttonStyle, GUILayout.Height(44f)))
            ui.CarregarInfos();
        GUILayout.EndHorizontal();
    }

    private void DrawGrazTimings()
    {
        Card("Tempos do GRAZ", "Duracao de cada fase usada pelos markers e pelo log.");
        baselineText = Field("Baseline (s)", baselineText, "inicio da sessao/run");
        fixationText = Field("Fixation (s)", fixationText, "ponto/cruz antes da cue");
        cueText = Field("Cue (s)", cueText, "imagem da classe e marker numerico");
        taskText = Field("Task/feedback (s)", taskText, "execucao ou imaginacao");
        minPauseText = Field("Pausa min (s)", minPauseText, "pausa randomizada minima");
        maxPauseText = Field("Pausa max (s)", maxPauseText, "pausa randomizada maxima");
        if (TooltipButton("Aplicar tempos", "Salva os tempos de baseline, fixation, cue, task/feedback e pausa no controlador GRAZ.", buttonStyle, GUILayout.Height(44f)))
            ApplyTimingFields();
    }

    private void DrawSubjectStimulusSettings()
    {
        Card("Aparencia da tela do sujeito", "Define o fundo e o conteudo visual das fases do GRAZ.");
        GUILayout.Label("Background", fieldLabelStyle);
        GUILayout.Label("Se uma imagem for escolhida, ela preenche a tela. Se ficar vazia, a cor abaixo vira o fundo.", bodyStyle);
        backgroundImagePath = DrawImagePathRow("Imagem de fundo", backgroundImagePath, "Selecionar imagem de fundo", value => backgroundImagePath = value);
        backgroundColor = DrawColorControls("Cor de fundo", backgroundColor, new Color(0.78f, 0.78f, 0.78f, 1f));

        GUILayout.Space(8f);
        GUILayout.Label("Cue", fieldLabelStyle);
        cueScreenPercentText = Field("Tamanho cue (%)", cueScreenPercentText, "area maxima ocupada pela cue");
        cueColor = DrawColorControls("Cor/tom da cue", cueColor, Color.white);

        GUILayout.Space(8f);
        GUILayout.Label("Fixation", fieldLabelStyle);
        GUILayout.Label("Com imagem: mostra a imagem no tamanho da cue. Sem imagem: mostra a cruz na cor definida.", bodyStyle);
        fixationImagePath = DrawImagePathRow("Imagem fixation", fixationImagePath, "Selecionar imagem fixation", value => fixationImagePath = value);
        fixationColor = DrawColorControls("Cor da cruz", fixationColor, Color.red);
        fixationSizePercentText = Field("Tamanho cruz (%)", fixationSizePercentText, "altura aproximada ocupada na tela");

        GUILayout.Space(8f);
        GUILayout.Label("Conteudo opcional por bloco", fieldLabelStyle);
        DrawPhaseContentEditor("Baseline", ref baselineImagePath, ref baselineStimulusText, ref baselineTextColor, ref baselineTextSizePercentText, "Selecionar imagem baseline", value => baselineImagePath = value);
        DrawPhaseContentEditor("Task/feedback", ref taskImagePath, ref taskStimulusText, ref taskTextColor, ref taskTextSizePercentText, "Selecionar imagem task", value => taskImagePath = value);
        DrawPhaseContentEditor("Pausa/rest", ref pauseImagePath, ref pauseStimulusText, ref pauseTextColor, ref pauseTextSizePercentText, "Selecionar imagem pausa", value => pauseImagePath = value);

        GUILayout.BeginHorizontal();
        if (TooltipButton("Aplicar tela do sujeito", "Aplica fundo, cor/imagem de fixation, tamanho da cue e conteudos opcionais dos blocos na tela limpa do sujeito.", buttonStyle, GUILayout.Height(44f)))
            ApplyStimulusSettings();
        if (TooltipButton("Preset neutro", "Restaura uma tela simples: fundo cinza suave, cruz vermelha e sem imagens/textos opcionais nos blocos.", buttonStyle, GUILayout.Height(44f)))
            ResetStimulusSettings();
        GUILayout.EndHorizontal();
    }

    private void DrawCueMarkers(bool feedback)
    {
        Card("Classes, cues e markers", feedback ? "Cada classe tera imagem, codigo e alvo." : "Cada classe tera imagem, codigo e parametros registrados no log.");
        presetNameText = Field("Nome do preset", presetNameText, "identifica esse desenho");
        repetitionsPerClassText = Field("Repeticoes/classe", repetitionsPerClassText, "define total planejado");
        randomSeedText = Field("Seed", randomSeedText, "ordem reprodutivel");
        shuffleTrials = GUILayout.Toggle(shuffleTrials, "Embaralhar classes antes do run", toggleStyle);

        GUILayout.BeginHorizontal();
        if (TooltipButton("Adicionar classe", "Adiciona uma nova classe/cue ao protocolo para voce configurar nome, codigo, imagem, alvo, tolerancia e hold.", buttonStyle, GUILayout.Height(42f)))
            AddCueClass();
        if (TooltipButton("Autocodigos", "Preenche automaticamente os codigos das classes como 1000, 1001, 1002... Esses codigos sao os markers numericos enviados pelo LSL.", buttonStyle, GUILayout.Height(42f)))
            AutoCodeCueClasses();
        if (TooltipButton("Preview ordem", "Gera e mostra a ordem planejada dos trials usando classes ativas, repeticoes por classe, seed e shuffle.", buttonStyle, GUILayout.Height(42f)))
            GenerateTrialPreview();
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (TooltipButton("Salvar padrao", "Salva esse preset localmente no Unity para recarregar depois sem escolher um arquivo.", buttonStyle, GUILayout.Height(42f)))
            SaveCuePresetToPrefs();
        if (TooltipButton("Carregar padrao", "Carrega o preset local salvo anteriormente no Unity.", buttonStyle, GUILayout.Height(42f)))
            LoadCuePresetFromPrefs(true);
        if (TooltipButton("Salvar JSON", "Exporta o preset atual para um arquivo JSON que pode ser guardado, versionado ou compartilhado.", buttonStyle, GUILayout.Height(42f)))
        {
            StartCoroutine(SaveCuePresetJsonCoroutine());
            GUIUtility.ExitGUI();
        }
        if (TooltipButton("Carregar JSON", "Importa um preset GRAZ salvo em JSON.", buttonStyle, GUILayout.Height(42f)))
        {
            StartCoroutine(LoadCuePresetJsonCoroutine());
            GUIUtility.ExitGUI();
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (TooltipButton("Exportar ordem", "Exporta a sequencia planejada dos trials em JSON e CSV para auditoria ou reproducao do run.", buttonStyle, GUILayout.Height(42f)))
        {
            StartCoroutine(ExportTrialPlanJsonCoroutine());
            GUIUtility.ExitGUI();
        }
        if (TooltipButton("Restaurar simples", "Restaura o preset simples inicial com classes de movimento da perna direita em diferentes angulos e relaxado.", buttonStyle, GUILayout.Height(42f)))
            ResetDefaultCueClasses();
        GUILayout.EndHorizontal();

        if (!string.IsNullOrWhiteSpace(lastPresetPath))
            GUILayout.Label("Preset: " + lastPresetPath, bodyStyle);
        if (!string.IsNullOrWhiteSpace(lastTrialPlanPath))
            GUILayout.Label("Ordem: " + lastTrialPlanPath, bodyStyle);

        GUILayout.Space(6f);
        for (int i = 0; i < cueClasses.Count; i++)
            DrawCueClassEditor(i, feedback);

        if (trialPreview.Count > 0)
        {
            Card("Preview da ordem", "Primeiros trials gerados a partir das classes ativas.");
            int maxPreview = Mathf.Min(trialPreview.Count, 12);
            for (int i = 0; i < maxPreview; i++)
                GUILayout.Label(trialPreview[i], bodyStyle);
            if (trialPreview.Count > maxPreview)
                GUILayout.Label("... total: " + trialPreview.Count + " trials", bodyStyle);
        }
    }

    private void DrawFeedbackVisualSettings()
    {
        Card("Feedback visual do sujeito", "Escolha o que aparece durante a Task do GRAZ feedback.");
        GUILayout.Label("Essas opcoes sao aplicadas quando voce clicar em Iniciar feedback.", bodyStyle);
        GUILayout.Space(6f);

        GUILayout.BeginVertical(GUI.skin.box);
        showFeedbackGauge = GUILayout.Toggle(showFeedbackGauge, "Barra 0-90 da perna alvo", toggleStyle);
        GUILayout.Label("Mostra a escala de 0 a 90 graus, alvo verde e marcador vermelho da posicao atual.", checklistTextStyle);
        GUILayout.EndVertical();

        GUILayout.BeginVertical(GUI.skin.box);
        showFeedbackSideView = GUILayout.Toggle(showFeedbackSideView, "Vista lateral da perna alvo", toggleStyle);
        GUILayout.Label("Mostra uma janela lateral pequena com a perna ativa daquele trial.", checklistTextStyle);
        GUILayout.EndVertical();

        GUILayout.BeginVertical(GUI.skin.box);
        showFeedbackGhost = GUILayout.Toggle(showFeedbackGhost, "Perna fantasma no angulo alvo", toggleStyle);
        GUILayout.Label("Mostra uma referencia transparente parada no angulo que o sujeito deve alcancar.", checklistTextStyle);
        GUILayout.EndVertical();

        GUILayout.BeginVertical(GUI.skin.box);
        showHoldProgressFeedback = GUILayout.Toggle(showHoldProgressFeedback, "Habilitar feedback de tempo de permanencia", toggleStyle);
        GUILayout.Label("Durante a Task, mostra no topo o tempo continuo dentro de alvo +/- tolerancia. Vermelho durante a contagem e verde quando o hold for concluido.", checklistTextStyle);
        GUILayout.EndVertical();

        GUILayout.Space(4f);
        GUILayout.Label("Como cada trial tem uma unica classe, o feedback destaca apenas a perna alvo. Classes de relaxamento nao mostram alvo de movimento.", bodyStyle);
    }

    private void DrawFeedbackCriteria()
    {
        Card("Criterio de sucesso e dificuldade", "Define o que conta como bom controle da protese.");
        gravityText = Field("Queda/gravid. (graus/s)", gravityText, "velocidade de retorno quando nao ha comando valido");
        maxMovementSpeedText = Field("Velocidade max. (graus/s)", maxMovementSpeedText, "multiplicada pela probabilidade recebida");
        inputStaleSecondsText = Field("Timeout entrada (s)", inputStaleSecondsText, "apos esse tempo sem amostra, aplica queda");
        toleranceText = Field("Tolerancia padrao", toleranceText, "usada como padrao; cada classe pode sobrescrever");
        holdTimeText = Field("Tempo no alvo padrao (s)", holdTimeText, "usado como padrao; cada classe pode sobrescrever");

        GUILayout.Space(8f);
        GUILayout.Label("Validacao apos a Task", fieldLabelStyle);
        showResultFeedback = GUILayout.Toggle(showResultFeedback, "Mostrar tela de sucesso ou insucesso", toggleStyle);
        if (showResultFeedback)
        {
            DrawPhaseContentEditor("Sucesso", ref successImagePath, ref successFeedbackText, ref successTextColor, ref successTextSizePercentText, "Selecionar imagem de sucesso", value => successImagePath = value);
            successDurationText = Field("Tempo sucesso (s)", successDurationText, "duracao da tela de sucesso");
            DrawPhaseContentEditor("Insucesso", ref failureImagePath, ref failureFeedbackText, ref failureTextColor, ref failureTextSizePercentText, "Selecionar imagem de insucesso", value => failureImagePath = value);
            failureDurationText = Field("Tempo insucesso (s)", failureDurationText, "duracao da tela de insucesso");
        }

        if (TooltipButton("Aplicar feedback", "Aplica gravidade, tolerancia e tempo minimo no alvo para o modo com feedback em tempo real.", buttonStyle, GUILayout.Height(44f)))
            ApplyFeedbackFields();
        Card("Regra experimental V1", "Classe discreta define perna/angulo. Probabilidade controla velocidade. Movimento ocorre somente na Task; sem sinal recente, a perna cai.");
    }

    private void DrawCueClassEditor(int index, bool feedback)
    {
        var cueClass = cueClasses[index];

        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.BeginHorizontal();
        cueClass.enabled = GUILayout.Toggle(cueClass.enabled, "", toggleStyle, GUILayout.Width(24f));
        GUILayout.Label("Classe " + index, fieldLabelStyle, GUILayout.Width(100f));
        cueClass.label = GUILayout.TextField(cueClass.label, textFieldStyle, GUILayout.Height(38f), GUILayout.MinWidth(110f));
        GUILayout.Label("Codigo", fieldLabelStyle, GUILayout.Width(76f));
        cueClass.markerCode = GUILayout.TextField(cueClass.markerCode, textFieldStyle, GUILayout.Height(38f), GUILayout.Width(70f));
        if (TooltipButton("X", "Remove esta classe do preset atual.", buttonStyle, GUILayout.Width(30f), GUILayout.Height(34f)))
        {
            cueClasses.RemoveAt(index);
            return;
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("ID", fieldLabelStyle, GUILayout.Width(100f));
        cueClass.classId = GUILayout.TextField(cueClass.classId, textFieldStyle, GUILayout.Height(38f), GUILayout.MinWidth(110f));
        GUILayout.Label("Alvo", fieldLabelStyle, GUILayout.Width(58f));
        cueClass.targetAngle = GUILayout.TextField(cueClass.targetAngle, textFieldStyle, GUILayout.Height(38f), GUILayout.Width(76f));
        GUILayout.Label("Tol", fieldLabelStyle, GUILayout.Width(40f));
        cueClass.tolerance = GUILayout.TextField(cueClass.tolerance, textFieldStyle, GUILayout.Height(38f), GUILayout.Width(68f));
        GUILayout.Label("Hold", fieldLabelStyle, GUILayout.Width(58f));
        cueClass.holdTime = GUILayout.TextField(cueClass.holdTime, textFieldStyle, GUILayout.Height(38f), GUILayout.Width(68f));
        GUILayout.EndHorizontal();

        if (feedback)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Comando", fieldLabelStyle, GUILayout.Width(100f));
            int selectedSide = GUILayout.Toolbar((int)cueClass.legSide, new[] { "Direita", "Esquerda", "Relaxar" }, toolbarStyle, GUILayout.Height(40f));
            cueClass.legSide = (GrazLegSide)Mathf.Clamp(selectedSide, 0, 2);
            GUILayout.EndHorizontal();
        }

        GUILayout.BeginHorizontal();
        GUILayout.Label("Imagem", fieldLabelStyle, GUILayout.Width(100f));
        cueClass.cueImagePath = GUILayout.TextField(cueClass.cueImagePath, textFieldStyle, GUILayout.Height(38f));
        if (TooltipButton("Escolher", "Escolhe a imagem que aparecera como cue para esta classe.", buttonStyle, GUILayout.Width(96f), GUILayout.Height(34f)))
        {
            StartCoroutine(SelectCueImageCoroutine(index));
            GUIUtility.ExitGUI();
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Descricao", fieldLabelStyle, GUILayout.Width(100f));
        cueClass.description = GUILayout.TextArea(cueClass.description, textAreaStyle, GUILayout.Height(64f));
        GUILayout.EndHorizontal();
        GUILayout.EndVertical();
    }

    private void AddCueClass()
    {
        int index = cueClasses.Count;
        cueClasses.Add(new EditableCueClass("class_" + index.ToString("00"), "Classe " + index, 1000 + index, 0, ""));
        status = "Classe adicionada.";
    }

    private void AutoCodeCueClasses()
    {
        for (int i = 0; i < cueClasses.Count; i++)
        {
            cueClasses[i].markerCode = (1000 + i).ToString();
            if (string.IsNullOrWhiteSpace(cueClasses[i].classId))
                cueClasses[i].classId = "class_" + i.ToString("00");
        }

        status = "Codigos numericos atualizados.";
    }

    private bool ValidateCueClasses(out string message, bool requireCueImages = false)
    {
        var markerCodes = new HashSet<int>();
        int enabledCount = 0;

        for (int i = 0; i < cueClasses.Count; i++)
        {
            var cueClass = cueClasses[i];
            if (!cueClass.enabled)
                continue;

            enabledCount++;

            if (string.IsNullOrWhiteSpace(cueClass.label))
            {
                message = "Classe " + i + " esta sem label.";
                return false;
            }

            if (!int.TryParse(cueClass.markerCode, out int markerCode))
            {
                message = "Classe " + cueClass.label + " tem codigo invalido.";
                return false;
            }

            if (!markerCodes.Add(markerCode))
            {
                message = "Codigo duplicado: " + markerCode + ".";
                return false;
            }

            if (!float.TryParse(cueClass.targetAngle, out _))
            {
                message = "Classe " + cueClass.label + " tem alvo invalido.";
                return false;
            }

            if (requireCueImages)
            {
                if (string.IsNullOrWhiteSpace(cueClass.cueImagePath))
                {
                    message = "Classe " + cueClass.label + " esta sem imagem de cue.";
                    return false;
                }

                if (!File.Exists(cueClass.cueImagePath))
                {
                    message = "Imagem nao encontrada para " + cueClass.label + ".";
                    return false;
                }
            }
        }

        if (enabledCount == 0)
        {
            message = "Ative pelo menos uma classe.";
            return false;
        }

        message = "Configuracao valida: " + enabledCount + " classes ativas.";
        return true;
    }

    private bool HasSessionFields()
    {
        return !string.IsNullOrWhiteSpace(subjectIdText)
            && !string.IsNullOrWhiteSpace(sessionIdText)
            && !string.IsNullOrWhiteSpace(runText);
    }

    private bool HasLogPath()
    {
        return ui != null && !string.IsNullOrWhiteSpace(ui._pathLog);
    }

    private bool HasMarkerStream()
    {
        return !string.IsNullOrWhiteSpace(markerStreamNameText)
            && !string.IsNullOrWhiteSpace(markerStreamTypeText);
    }

    private bool HasClassifierStream()
    {
        return !string.IsNullOrWhiteSpace(streamNameText)
            && !string.IsNullOrWhiteSpace(streamTypeText);
    }

    private bool FreeLslShowsLeft()
    {
        return freeLslDisplaySides == 1 || freeLslDisplaySides == 2;
    }

    private bool FreeLslShowsRight()
    {
        return freeLslDisplaySides == 0 || freeLslDisplaySides == 2;
    }

    private bool HasValidFreeLslVisualSettings()
    {
        if (!freeLslShowGauge && !freeLslShowSideView && !freeLslShowGhost)
            return false;

        if (FreeLslShowsLeft())
        {
            float target = ParseFloat(freeLslLeftTargetText, -1f);
            float tolerance = freeLslShowToleranceBand ? ParseFloat(freeLslLeftToleranceText, -1f) : 0f;
            if (target < 0f || target > 90f || tolerance < 0f || tolerance > 90f)
                return false;
        }

        if (FreeLslShowsRight())
        {
            float target = ParseFloat(freeLslRightTargetText, -1f);
            float tolerance = freeLslShowToleranceBand ? ParseFloat(freeLslRightToleranceText, -1f) : 0f;
            if (target < 0f || target > 90f || tolerance < 0f || tolerance > 90f)
                return false;
        }

        return true;
    }

    private bool HasValidFreeLslMovementSettings()
    {
        return ParseFloat(gravityText, -1f) >= 0f
            && ParseFloat(maxMovementSpeedText, -1f) > 0f
            && ParseFloat(inputStaleSecondsText, -1f) > 0f;
    }

    private bool HasValidFreeLslCommands(out string message)
    {
        var codes = new HashSet<int>();
        int activeCount = 0;

        foreach (FreeLslCommandDefinition command in freeLslCommands)
        {
            if (command == null || !command.enabled)
                continue;

            activeCount++;
            if (!int.TryParse(command.code, out int code))
            {
                message = "Todo comando ativo precisa ter um codigo inteiro.";
                return false;
            }
            if (!codes.Add(code))
            {
                message = "Os codigos dos comandos do controle livre devem ser unicos.";
                return false;
            }

            float angle = ParseFloat(command.targetAngle, -1f);
            if (angle < 0f || angle > 90f)
            {
                message = "Todo comando ativo precisa ter angulo entre 0 e 90 graus.";
                return false;
            }
        }

        message = activeCount > 0 ? "" : "Ative pelo menos um comando LSL.";
        return activeCount > 0;
    }

    private bool HasConnectedClassifierStream()
    {
        var receiver = ui != null ? ui.GetComponent<ClassificationLSLReceiver>() : null;
        return receiver != null && receiver.IsConnected;
    }

    private bool HasRecentClassifierSample()
    {
        var receiver = ui != null ? ui.GetComponent<ClassificationLSLReceiver>() : null;
        return receiver != null && receiver.HasFreshSignal;
    }

    private bool HasValidTimings()
    {
        float baseline = ParseFloat(baselineText, -1f);
        float fixation = ParseFloat(fixationText, -1f);
        float cue = ParseFloat(cueText, -1f);
        float task = ParseFloat(taskText, -1f);
        float minPause = ParseFloat(minPauseText, -1f);
        float maxPause = ParseFloat(maxPauseText, -1f);

        return baseline >= 0f
            && fixation >= 0f
            && cue >= 0f
            && task >= 0f
            && minPause >= 0f
            && maxPause >= minPause;
    }

    private bool HasValidCueClasses(bool requireCueImages)
    {
        return ValidateCueClasses(out _, requireCueImages);
    }

    private bool HasCueImagesForActiveClasses()
    {
        foreach (var cueClass in cueClasses)
        {
            if (!cueClass.enabled)
                continue;

            if (string.IsNullOrWhiteSpace(cueClass.cueImagePath) || !File.Exists(cueClass.cueImagePath))
                return false;
        }

        return CountActiveCueClasses() > 0;
    }

    private bool HasGeneratedCurrentTrialPlan()
    {
        int expectedTrials = CountActiveCueClasses() * ParsePositiveInt(repetitionsPerClassText, 1);
        return expectedTrials > 0 && generatedTrialPlan.Count == expectedTrials;
    }

    private bool HasValidFeedbackCriteria()
    {
        return ParseFloat(gravityText, -1f) >= 0f
            && ParseFloat(maxMovementSpeedText, -1f) > 0f
            && ParseFloat(inputStaleSecondsText, -1f) > 0f
            && ParseFloat(toleranceText, -1f) >= 0f
            && ParseFloat(holdTimeText, -1f) >= 0f
            && (!showResultFeedback
                || (ParseFloat(successDurationText, -1f) >= 0f
                    && ParseFloat(failureDurationText, -1f) >= 0f));
    }

    private bool ValidateFullConfiguration()
    {
        if (activeMode == ExperimentMode.FreeLsl)
            return ValidateFreeLslConfiguration();

        if (!HasSessionFields())
            return FailValidation("Preencha sujeito, sessao e run.");
        if (!HasLogPath())
            return FailValidation("Escolha a pasta de log antes de iniciar.");
        if (!HasMarkerStream())
            return FailValidation("Configure nome e tipo do stream de markers.");
        if (activeMode == ExperimentMode.GrazFeedback && !HasClassifierStream())
            return FailValidation("Configure nome e tipo do stream de entrada do classificador.");
        if (activeMode == ExperimentMode.GrazFeedback && !HasConnectedClassifierStream())
            return FailValidation("Conecte a stream Classifications de entrada antes de iniciar.");
        if (!HasValidTimings())
            return FailValidation("Preencha tempos validos: baseline, fixation, cue, task, pausa min e pausa max.");
        if (!ValidateCueClasses(out string classMessage, false))
            return FailValidation(classMessage);
        if (!HasCueImagesForActiveClasses())
            return FailValidation("Escolha uma imagem existente para cada classe ativa.");
        if (!HasGeneratedCurrentTrialPlan())
            return FailValidation("Clique em Preview da ordem para gerar a sequencia atual de trials.");
        if (activeMode == ExperimentMode.GrazFeedback && !HasValidFeedbackCriteria())
            return FailValidation("Preencha criterios de feedback validos: gravidade, tolerancia e hold time.");

        status = "Configuracao completa. Pronto para iniciar.";
        return true;
    }

    private bool ValidateFreeLslConfiguration()
    {
        if (!HasClassifierStream())
            return FailValidation("Configure nome e tipo do stream de entrada do classificador.");
        if (!HasConnectedClassifierStream())
            return FailValidation("Conecte a stream Classifications de entrada antes de iniciar.");
        if (!HasValidFreeLslVisualSettings())
            return FailValidation("Escolha ao menos um feedback e use alvo/tolerancia entre 0 e 90 graus.");
        if (!HasValidFreeLslMovementSettings())
            return FailValidation("Preencha gravidade, velocidade maxima e timeout com valores validos.");
        if (!HasValidFreeLslCommands(out string commandMessage))
            return FailValidation(commandMessage);

        status = "Controle livre configurado. Pronto para iniciar.";
        return true;
    }

    private bool FailValidation(string message)
    {
        status = message;
        return false;
    }

    private int CountActiveCueClasses()
    {
        int count = 0;
        foreach (var cueClass in cueClasses)
        {
            if (cueClass.enabled)
                count++;
        }

        return count;
    }

    private void UpdatePlannedTrialsText()
    {
        trialsText = (CountActiveCueClasses() * ParsePositiveInt(repetitionsPerClassText, 1)).ToString();
    }

    private void GenerateTrialPreview()
    {
        if (!ValidateCueClasses(out string validationMessage))
        {
            status = validationMessage;
            return;
        }

        int repetitions = ParsePositiveInt(repetitionsPerClassText, 1);
        int seed = ParseInt(randomSeedText, Environment.TickCount);
        var planned = new List<EditableCueClass>();

        foreach (var cueClass in cueClasses)
        {
            if (!cueClass.enabled)
                continue;

            for (int i = 0; i < repetitions; i++)
                planned.Add(cueClass);
        }

        if (shuffleTrials)
            Shuffle(planned, seed);

        trialPreview.Clear();
        generatedTrialPlan.Clear();
        for (int i = 0; i < planned.Count; i++)
        {
            var cueClass = planned[i];
            int markerCode = ParseInt(cueClass.markerCode, 1000 + i);
            float targetAngle = ParseFloat(cueClass.targetAngle, 0f);
            float tolerance = ParseFloat(cueClass.tolerance, ParseFloat(toleranceText, 5f));
            float holdTime = ParseFloat(cueClass.holdTime, ParseFloat(holdTimeText, 3f));

            generatedTrialPlan.Add(new GeneratedTrialRow
            {
                trialIndex = i + 1,
                subjectId = subjectIdText,
                sessionId = sessionIdText,
                run = runText,
                classId = cueClass.classId,
                label = cueClass.label,
                markerCode = markerCode,
                cueImagePath = cueClass.cueImagePath,
                description = cueClass.description,
                targetAngle = targetAngle,
                tolerance = tolerance,
                holdTime = holdTime,
                legSide = cueClass.legSide,
                randomSeed = seed,
                shuffled = shuffleTrials
            });

            trialPreview.Add(
                (i + 1).ToString("00") +
                " | " + cueClass.label +
                " | marker " + cueClass.markerCode +
                " | alvo " + cueClass.targetAngle + " graus" +
                " | " + cueClass.legSide);
        }

        trialsText = planned.Count.ToString();
        ApplySessionFields();
        status = "Ordem gerada: " + planned.Count + " trials.";
    }

    private void Shuffle<T>(IList<T> items, int seed)
    {
        var random = new System.Random(seed);

        for (int i = items.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            T temp = items[i];
            items[i] = items[j];
            items[j] = temp;
        }
    }

    private IEnumerator SelectCueImageCoroutine(int cueIndex)
    {
        fileBrowserOpen = true;
        FileBrowser.SetFilters(true, new FileBrowser.Filter("Images", ".jpg", ".jpeg", ".png"));
        FileBrowser.SetDefaultFilter(".png");
        FileBrowser.SetExcludedExtensions(".lnk", ".tmp", ".zip", ".rar", ".exe");

        yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.Files, false, null, null, "Selecione a imagem da cue", "Usar imagem");
        fileBrowserOpen = false;
        if (!FileBrowser.Success || FileBrowser.Result == null || FileBrowser.Result.Length == 0)
        {
            status = "Selecao de imagem cancelada.";
            yield break;
        }

        if (cueIndex < 0 || cueIndex >= cueClasses.Count)
            yield break;

        cueClasses[cueIndex].cueImagePath = FileBrowser.Result[0];
        status = "Imagem definida para " + cueClasses[cueIndex].label + ".";
    }

    private IEnumerator SelectImagePathCoroutine(Action<string> assignPath, string dialogTitle)
    {
        fileBrowserOpen = true;
        FileBrowser.SetFilters(true, new FileBrowser.Filter("Images", ".jpg", ".jpeg", ".png"));
        FileBrowser.SetDefaultFilter(".png");
        FileBrowser.SetExcludedExtensions(".lnk", ".tmp", ".zip", ".rar", ".exe");

        yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.Files, false, null, null, dialogTitle, "Usar imagem");
        fileBrowserOpen = false;

        if (!FileBrowser.Success || FileBrowser.Result == null || FileBrowser.Result.Length == 0)
        {
            status = "Selecao de imagem cancelada.";
            yield break;
        }

        assignPath?.Invoke(FileBrowser.Result[0]);
        status = "Imagem selecionada.";
    }

    private IEnumerator SelectLogDirectoryCoroutine()
    {
        fileBrowserOpen = true;
        yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.Folders, false, null, null, "Selecione a pasta dos logs", "Usar pasta");
        fileBrowserOpen = false;

        if (!FileBrowser.Success || FileBrowser.Result == null || FileBrowser.Result.Length == 0)
        {
            status = "Selecao de log cancelada.";
            yield break;
        }

        ui._pathLog = FileBrowser.Result[0];
        ConfigureExperimentalLogger();
        status = "Pasta de log definida: " + ui._pathLog;
    }

    private IEnumerator SaveCuePresetJsonCoroutine()
    {
        var defaultPath = GetPresetDirectory();
        var filename = MakeFileSafe(presetNameText) + ".json";

        fileBrowserOpen = true;
        FileBrowser.SetFilters(true, new FileBrowser.Filter("JSON", ".json"));
        FileBrowser.SetDefaultFilter(".json");
        yield return FileBrowser.WaitForSaveDialog(FileBrowser.PickMode.Files, false, defaultPath, filename, "Salvar preset GRAZ", "Salvar");
        fileBrowserOpen = false;

        if (!FileBrowser.Success || FileBrowser.Result == null || FileBrowser.Result.Length == 0)
        {
            status = "Salvamento cancelado.";
            yield break;
        }

        SaveCuePresetJson(FileBrowser.Result[0]);
    }

    private IEnumerator LoadCuePresetJsonCoroutine()
    {
        fileBrowserOpen = true;
        FileBrowser.SetFilters(true, new FileBrowser.Filter("JSON", ".json"));
        FileBrowser.SetDefaultFilter(".json");
        yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.Files, false, GetPresetDirectory(), null, "Carregar preset GRAZ", "Carregar");
        fileBrowserOpen = false;

        if (!FileBrowser.Success || FileBrowser.Result == null || FileBrowser.Result.Length == 0)
        {
            status = "Carregamento cancelado.";
            yield break;
        }

        LoadCuePresetJson(FileBrowser.Result[0]);
    }

    private IEnumerator ExportTrialPlanJsonCoroutine()
    {
        if (!ValidateCueClasses(out string validationMessage, true))
        {
            status = validationMessage;
            yield break;
        }

        GenerateTrialPreview();

        var defaultPath = GetPresetDirectory();
        var filename = MakeFileSafe(presetNameText) + "_trial_order.json";

        fileBrowserOpen = true;
        FileBrowser.SetFilters(true, new FileBrowser.Filter("JSON", ".json"));
        FileBrowser.SetDefaultFilter(".json");
        yield return FileBrowser.WaitForSaveDialog(FileBrowser.PickMode.Files, false, defaultPath, filename, "Exportar ordem de trials", "Exportar");
        fileBrowserOpen = false;

        if (!FileBrowser.Success || FileBrowser.Result == null || FileBrowser.Result.Length == 0)
        {
            status = "Exportacao cancelada.";
            yield break;
        }

        ExportTrialPlanJson(FileBrowser.Result[0]);
    }

    private void SaveCuePresetJson(string path)
    {
        path = EnsureExtension(path, ".json");
        var data = BuildPresetData();
        EnsureParentDirectory(path);
        File.WriteAllText(path, JsonUtility.ToJson(data, true));
        lastPresetPath = path;
        status = "Preset salvo em JSON.";
    }

    private void LoadCuePresetJson(string path)
    {
        if (!File.Exists(path))
        {
            status = "Arquivo de preset nao encontrado.";
            return;
        }

        var data = JsonUtility.FromJson<EditableCuePresetData>(File.ReadAllText(path));
        if (data == null)
        {
            status = "Arquivo de preset invalido.";
            return;
        }

        ApplyPresetData(data);
        lastPresetPath = path;
        status = "Preset carregado de JSON.";
    }

    private void ExportTrialPlanJson(string path)
    {
        path = EnsureExtension(path, ".json");
        int repetitions = ParsePositiveInt(repetitionsPerClassText, 1);
        int seed = generatedTrialPlan.Count > 0 ? generatedTrialPlan[0].randomSeed : ParseInt(randomSeedText, Environment.TickCount);
        var data = new GeneratedTrialPlanData
        {
            presetName = presetNameText,
            subjectId = subjectIdText,
            sessionId = sessionIdText,
            run = runText,
            generatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            repetitionsPerClass = repetitions,
            randomSeed = seed,
            shuffled = shuffleTrials,
            trials = generatedTrialPlan
        };

        EnsureParentDirectory(path);
        File.WriteAllText(path, JsonUtility.ToJson(data, true));

        var csvPath = Path.ChangeExtension(path, ".csv");
        File.WriteAllText(csvPath, BuildTrialPlanCsv());

        lastTrialPlanPath = path;
        status = "Ordem exportada em JSON e CSV.";
    }

    private EditableCuePresetData BuildPresetData()
    {
        return new EditableCuePresetData
        {
            schemaVersion = CurrentPresetSchemaVersion,
            secondDisplayEnabled = secondDisplayEnabled,
            presetName = presetNameText,
            subjectId = subjectIdText,
            sessionId = sessionIdText,
            run = runText,
            repetitionsPerClass = repetitionsPerClassText,
            baselineDuration = baselineText,
            fixationDuration = fixationText,
            cueDuration = cueText,
            taskDuration = taskText,
            minPauseDuration = minPauseText,
            maxPauseDuration = maxPauseText,
            randomSeed = randomSeedText,
            shuffle = shuffleTrials,
            markerStreamName = markerStreamNameText,
            markerStreamType = markerStreamTypeText,
            numericOnly = numericMarkersOnly,
            backgroundImagePath = backgroundImagePath,
            backgroundColorHex = ColorUtility.ToHtmlStringRGB(backgroundColor),
            fixationImagePath = fixationImagePath,
            fixationColorHex = ColorUtility.ToHtmlStringRGB(fixationColor),
            baselineImagePath = baselineImagePath,
            baselineText = baselineStimulusText,
            taskImagePath = taskImagePath,
            taskText = taskStimulusText,
            pauseImagePath = pauseImagePath,
            pauseText = pauseStimulusText,
            cueScreenPercent = cueScreenPercentText,
            cueColorHex = ColorUtility.ToHtmlStringRGB(cueColor),
            fixationSizePercent = fixationSizePercentText,
            baselineTextColorHex = ColorUtility.ToHtmlStringRGB(baselineTextColor),
            baselineTextSizePercent = baselineTextSizePercentText,
            taskTextColorHex = ColorUtility.ToHtmlStringRGB(taskTextColor),
            taskTextSizePercent = taskTextSizePercentText,
            pauseTextColorHex = ColorUtility.ToHtmlStringRGB(pauseTextColor),
            pauseTextSizePercent = pauseTextSizePercentText,
            inputStreamName = streamNameText,
            inputStreamType = streamTypeText,
            gravity = gravityText,
            defaultTolerance = toleranceText,
            defaultHoldTime = holdTimeText,
            maxMovementSpeed = maxMovementSpeedText,
            inputStaleSeconds = inputStaleSecondsText,
            showFeedbackGauge = showFeedbackGauge,
            showFeedbackSideView = showFeedbackSideView,
            showFeedbackGhost = showFeedbackGhost,
            showResultFeedback = showResultFeedback,
            successImagePath = successImagePath,
            successText = successFeedbackText,
            successDuration = successDurationText,
            failureImagePath = failureImagePath,
            failureText = failureFeedbackText,
            failureDuration = failureDurationText,
            successTextColorHex = ColorUtility.ToHtmlStringRGB(successTextColor),
            successTextSizePercent = successTextSizePercentText,
            failureTextColorHex = ColorUtility.ToHtmlStringRGB(failureTextColor),
            failureTextSizePercent = failureTextSizePercentText,
            showHoldProgressFeedback = showHoldProgressFeedback,
            freeLslDisplaySides = freeLslDisplaySides,
            freeLslShowGauge = freeLslShowGauge,
            freeLslShowSideView = freeLslShowSideView,
            freeLslShowGhost = freeLslShowGhost,
            freeLslShowToleranceBand = freeLslShowToleranceBand,
            freeLslLeftTarget = freeLslLeftTargetText,
            freeLslLeftTolerance = freeLslLeftToleranceText,
            freeLslRightTarget = freeLslRightTargetText,
            freeLslRightTolerance = freeLslRightToleranceText,
            freeLslCommands = freeLslCommands,
            classes = cueClasses
        };
    }

    private void ApplyPresetData(EditableCuePresetData data)
    {
        SetSecondDisplayEnabled(data.schemaVersion >= 5 && data.secondDisplayEnabled);
        presetNameText = string.IsNullOrWhiteSpace(data.presetName) ? presetNameText : data.presetName;
        subjectIdText = string.IsNullOrWhiteSpace(data.subjectId) ? subjectIdText : data.subjectId;
        sessionIdText = string.IsNullOrWhiteSpace(data.sessionId) ? sessionIdText : data.sessionId;
        runText = string.IsNullOrWhiteSpace(data.run) ? runText : data.run;
        repetitionsPerClassText = string.IsNullOrWhiteSpace(data.repetitionsPerClass) ? repetitionsPerClassText : data.repetitionsPerClass;
        baselineText = string.IsNullOrWhiteSpace(data.baselineDuration) ? baselineText : data.baselineDuration;
        fixationText = string.IsNullOrWhiteSpace(data.fixationDuration) ? fixationText : data.fixationDuration;
        cueText = string.IsNullOrWhiteSpace(data.cueDuration) ? cueText : data.cueDuration;
        taskText = string.IsNullOrWhiteSpace(data.taskDuration) ? taskText : data.taskDuration;
        minPauseText = string.IsNullOrWhiteSpace(data.minPauseDuration) ? minPauseText : data.minPauseDuration;
        maxPauseText = string.IsNullOrWhiteSpace(data.maxPauseDuration) ? maxPauseText : data.maxPauseDuration;
        randomSeedText = string.IsNullOrWhiteSpace(data.randomSeed) ? randomSeedText : data.randomSeed;
        markerStreamNameText = string.IsNullOrWhiteSpace(data.markerStreamName) ? markerStreamNameText : data.markerStreamName;
        markerStreamTypeText = string.IsNullOrWhiteSpace(data.markerStreamType) ? markerStreamTypeText : data.markerStreamType;
        shuffleTrials = data.shuffle;
        numericMarkersOnly = data.numericOnly;
        backgroundImagePath = data.backgroundImagePath ?? backgroundImagePath;
        fixationImagePath = data.fixationImagePath ?? fixationImagePath;
        baselineImagePath = data.baselineImagePath ?? baselineImagePath;
        baselineStimulusText = data.baselineText ?? baselineStimulusText;
        taskImagePath = data.taskImagePath ?? taskImagePath;
        taskStimulusText = data.taskText ?? taskStimulusText;
        pauseImagePath = data.pauseImagePath ?? pauseImagePath;
        pauseStimulusText = data.pauseText ?? pauseStimulusText;
        cueScreenPercentText = string.IsNullOrWhiteSpace(data.cueScreenPercent) ? cueScreenPercentText : data.cueScreenPercent;

        if (data.schemaVersion >= 2)
        {
            streamNameText = string.IsNullOrWhiteSpace(data.inputStreamName) ? streamNameText : data.inputStreamName;
            streamTypeText = string.IsNullOrWhiteSpace(data.inputStreamType) ? streamTypeText : data.inputStreamType;
            gravityText = string.IsNullOrWhiteSpace(data.gravity) ? gravityText : data.gravity;
            toleranceText = string.IsNullOrWhiteSpace(data.defaultTolerance) ? toleranceText : data.defaultTolerance;
            holdTimeText = string.IsNullOrWhiteSpace(data.defaultHoldTime) ? holdTimeText : data.defaultHoldTime;
            maxMovementSpeedText = string.IsNullOrWhiteSpace(data.maxMovementSpeed) ? maxMovementSpeedText : data.maxMovementSpeed;
            inputStaleSecondsText = string.IsNullOrWhiteSpace(data.inputStaleSeconds) ? inputStaleSecondsText : data.inputStaleSeconds;
            showFeedbackGauge = data.showFeedbackGauge;
            showFeedbackSideView = data.showFeedbackSideView;
            showFeedbackGhost = data.showFeedbackGhost;
            showResultFeedback = data.showResultFeedback;
            successImagePath = data.successImagePath ?? "";
            successFeedbackText = data.successText ?? successFeedbackText;
            successDurationText = string.IsNullOrWhiteSpace(data.successDuration) ? successDurationText : data.successDuration;
            failureImagePath = data.failureImagePath ?? "";
            failureFeedbackText = data.failureText ?? failureFeedbackText;
            failureDurationText = string.IsNullOrWhiteSpace(data.failureDuration) ? failureDurationText : data.failureDuration;
        }

        if (data.schemaVersion >= 3)
        {
            fixationSizePercentText = string.IsNullOrWhiteSpace(data.fixationSizePercent) ? fixationSizePercentText : data.fixationSizePercent;
            baselineTextSizePercentText = string.IsNullOrWhiteSpace(data.baselineTextSizePercent) ? baselineTextSizePercentText : data.baselineTextSizePercent;
            taskTextSizePercentText = string.IsNullOrWhiteSpace(data.taskTextSizePercent) ? taskTextSizePercentText : data.taskTextSizePercent;
            pauseTextSizePercentText = string.IsNullOrWhiteSpace(data.pauseTextSizePercent) ? pauseTextSizePercentText : data.pauseTextSizePercent;
            successTextSizePercentText = string.IsNullOrWhiteSpace(data.successTextSizePercent) ? successTextSizePercentText : data.successTextSizePercent;
            failureTextSizePercentText = string.IsNullOrWhiteSpace(data.failureTextSizePercent) ? failureTextSizePercentText : data.failureTextSizePercent;
            showHoldProgressFeedback = data.showHoldProgressFeedback;

            TryApplyHtmlColor(data.cueColorHex, ref cueColor);
            TryApplyHtmlColor(data.baselineTextColorHex, ref baselineTextColor);
            TryApplyHtmlColor(data.taskTextColorHex, ref taskTextColor);
            TryApplyHtmlColor(data.pauseTextColorHex, ref pauseTextColor);
            TryApplyHtmlColor(data.successTextColorHex, ref successTextColor);
            TryApplyHtmlColor(data.failureTextColorHex, ref failureTextColor);
        }

        if (data.schemaVersion >= 4)
        {
            freeLslDisplaySides = Mathf.Clamp(data.freeLslDisplaySides, 0, 2);
            freeLslShowGauge = data.freeLslShowGauge;
            freeLslShowSideView = data.freeLslShowSideView;
            freeLslShowGhost = data.freeLslShowGhost;
            freeLslShowToleranceBand = data.freeLslShowToleranceBand;
            freeLslLeftTargetText = string.IsNullOrWhiteSpace(data.freeLslLeftTarget) ? freeLslLeftTargetText : data.freeLslLeftTarget;
            freeLslLeftToleranceText = string.IsNullOrWhiteSpace(data.freeLslLeftTolerance) ? freeLslLeftToleranceText : data.freeLslLeftTolerance;
            freeLslRightTargetText = string.IsNullOrWhiteSpace(data.freeLslRightTarget) ? freeLslRightTargetText : data.freeLslRightTarget;
            freeLslRightToleranceText = string.IsNullOrWhiteSpace(data.freeLslRightTolerance) ? freeLslRightToleranceText : data.freeLslRightTolerance;
            if (data.freeLslCommands != null && data.freeLslCommands.Count > 0)
                freeLslCommands = data.freeLslCommands;
        }

        if (!string.IsNullOrWhiteSpace(data.backgroundColorHex) && ColorUtility.TryParseHtmlString("#" + data.backgroundColorHex, out Color parsedBackground))
            backgroundColor = parsedBackground;
        if (!string.IsNullOrWhiteSpace(data.fixationColorHex) && ColorUtility.TryParseHtmlString("#" + data.fixationColorHex, out Color parsedFixation))
            fixationColor = parsedFixation;

        if (data.classes != null && data.classes.Count > 0)
        {
            cueClasses = data.classes;
            if (data.schemaVersion < 4)
            {
                foreach (var cueClass in cueClasses)
                {
                    string classId = cueClass.classId ?? "";
                    string label = cueClass.label ?? "";
                    if (classId.IndexOf("left", StringComparison.OrdinalIgnoreCase) >= 0 || label.IndexOf("esquer", StringComparison.OrdinalIgnoreCase) >= 0)
                        cueClass.legSide = GrazLegSide.Left;
                    else if (classId.IndexOf("relax", StringComparison.OrdinalIgnoreCase) >= 0 || label.IndexOf("relax", StringComparison.OrdinalIgnoreCase) >= 0)
                        cueClass.legSide = GrazLegSide.Relaxed;
                    else
                        cueClass.legSide = GrazLegSide.Right;
                }
            }
        }

        trialPreview.Clear();
        generatedTrialPlan.Clear();
        EnsureDefaultFreeLslCommands();
        ApplyStimulusSettings();
    }

    private string BuildTrialPlanCsv()
    {
        var lines = new List<string>
        {
            "trial_index,subject_id,session_id,run,class_id,label,marker_code,cue_image_path,target_angle,tolerance,hold_time,leg_side,random_seed,shuffled"
        };

        foreach (var trial in generatedTrialPlan)
        {
            lines.Add(string.Join(",",
                trial.trialIndex,
                EscapeCsv(trial.subjectId),
                EscapeCsv(trial.sessionId),
                EscapeCsv(trial.run),
                EscapeCsv(trial.classId),
                EscapeCsv(trial.label),
                trial.markerCode,
                EscapeCsv(trial.cueImagePath),
                trial.targetAngle.ToString(CultureInfo.InvariantCulture),
                trial.tolerance.ToString(CultureInfo.InvariantCulture),
                trial.holdTime.ToString(CultureInfo.InvariantCulture),
                trial.legSide,
                trial.randomSeed,
                trial.shuffled));
        }

        return string.Join(Environment.NewLine, lines);
    }

    private string EscapeCsv(string value)
    {
        if (value == null)
            return "";

        if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
            return "\"" + value.Replace("\"", "\"\"") + "\"";

        return value;
    }

    private string GetPresetDirectory()
    {
        var directory = Path.Combine(Application.persistentDataPath, "GrazCuePresets");
        Directory.CreateDirectory(directory);
        return directory;
    }

    private string MakeFileSafe(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "graz_cue_preset";

        foreach (char invalid in Path.GetInvalidFileNameChars())
            value = value.Replace(invalid, '_');

        return value.Replace(" ", "_");
    }

    private string EnsureExtension(string path, string extension)
    {
        if (string.IsNullOrWhiteSpace(path) || Path.HasExtension(path))
            return path;

        return path + extension;
    }

    private void EnsureParentDirectory(string path)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);
    }

    private float ParseFloat(string value, float fallback)
    {
        if (float.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out float parsed))
            return parsed;

        if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed))
            return parsed;

        string normalized = string.IsNullOrWhiteSpace(value) ? "" : value.Replace(',', '.');
        if (float.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed))
            return parsed;

        return fallback;
    }

    private void TryApplyHtmlColor(string hex, ref Color target)
    {
        if (!string.IsNullOrWhiteSpace(hex) && ColorUtility.TryParseHtmlString("#" + hex, out Color parsed))
            target = parsed;
    }

    private void DrawActionColumn(params GUILayoutOption[] options)
    {
        GUILayout.BeginVertical(darkPanelStyle, options);
        actionScroll = GUILayout.BeginScrollView(actionScroll, GUILayout.ExpandHeight(true));
        GUILayout.Label("Acoes", sectionTitleStyle);
        if (activeMode == ExperimentMode.FreeLsl)
        {
            GUILayout.Label("LSL\nEntrada continua do classificador.", bodyStyle);
            GUILayout.Space(4f);
            GUILayout.Label("Feedback\nBarras, vistas laterais e referencias escolhidas.", bodyStyle);
            GUILayout.Space(10f);
            DrawFreeLslChecklist();
        }
        else
        {
            GUILayout.Label("Sessao\nSujeito, sessao, trials e log.", bodyStyle);
            GUILayout.Space(4f);
            GUILayout.Label("LSL\nEntrada do classificador e/ou markers.", bodyStyle);
            GUILayout.Space(4f);
            GUILayout.Label("GRAZ\nTempos, classes, cues e shuffle.", bodyStyle);
        }

        if (activeMode == ExperimentMode.GrazAcquisition || activeMode == ExperimentMode.GrazFeedback)
        {
            GUILayout.Space(10f);
            DrawConfigurationChecklist();
            if (activeMode == ExperimentMode.GrazFeedback)
            {
                GUILayout.Space(10f);
                DrawFeedbackVisualSummary();
            }
        }
        GUILayout.EndScrollView();

        GUILayout.Space(8f);
        if (TooltipButton("Validar configuracao", "Confere sessao, log, streams, tempos, classes, imagens, ordem dos trials e criterios do modo atual.", buttonStyle, GUILayout.Height(50f)))
            ValidateFullConfiguration();
        if (TooltipButton("Salvar preset", "Salva a configuracao atual como preset padrao local para ser reutilizada depois.", buttonStyle, GUILayout.Height(50f)))
            SaveCuePresetToPrefs();
        if (activeMode != ExperimentMode.FreeLsl)
        {
            if (TooltipButton("Preview da ordem", "Gera uma pre-visualizacao da ordem dos trials antes de iniciar o run.", buttonStyle, GUILayout.Height(50f)))
                GenerateTrialPreview();
        }

        GUILayout.Space(8f);
        if (TooltipButton(PrimaryActionLabel(), PrimaryActionTooltip(), selectedButtonStyle, GUILayout.Height(66f)))
            StartSelectedMode();
        GUILayout.EndVertical();
    }

    private void DrawFeedbackVisualSummary()
    {
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("Feedback visual", sectionTitleStyle);
        GUILayout.Label("Escolhas atuais para a tela do sujeito.", bodyStyle);
        GUILayout.Space(4f);
        ChecklistRow(showFeedbackGauge, "Barra 0-90", "Escala angular com alvo e posicao atual.");
        ChecklistRow(showFeedbackSideView, "Vista lateral", "Janela lateral da perna ativa.");
        ChecklistRow(showFeedbackGhost, "Perna fantasma", "Referencia transparente no alvo.");
        ChecklistRow(showHoldProgressFeedback, "Tempo no alvo", "Cronometro de permanencia durante a Task.");
        GUILayout.EndVertical();
    }

    private void DrawFreeLslChecklist()
    {
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("Checklist", sectionTitleStyle);
        GUILayout.Label("O modo livre nao exige sessao, log, trials, cues ou markers GRAZ.", bodyStyle);
        GUILayout.Space(4f);
        ChecklistRow(HasClassifierStream(), "Entrada LSL", "Nome e tipo da stream Classifications preenchidos.");
        ChecklistRow(HasConnectedClassifierStream(), "Stream conectada", "O inlet foi aberto e esta pronto para receber amostras.");
        DiagnosticRow(HasRecentClassifierSample(), "Amostra recente", "Diagnostico apenas; nao bloqueia a validacao nem o inicio.");
        ChecklistRow(HasValidFreeLslVisualSettings(), "Feedback visual", "Lados e elementos visuais possuem configuracao valida.");
        ChecklistRow(HasValidFreeLslMovementSettings(), "Movimento", "Queda, velocidade e timeout sao numericos e coerentes.");
        ChecklistRow(HasValidFreeLslCommands(out _), "Mapa de comandos", "Codigos ativos unicos com lado e angulo validos.");
        GUILayout.EndVertical();
    }

    private void DrawConfigurationChecklist()
    {
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("Checklist", sectionTitleStyle);
        GUILayout.Label("Complete estes passos antes de iniciar.", bodyStyle);
        GUILayout.Space(4f);
        ChecklistRow(HasSessionFields(), "Sessao", "Sujeito, sessao e run preenchidos.");
        ChecklistRow(HasLogPath(), "Log", "Pasta onde os arquivos da sessao serao salvos.");
        ChecklistRow(HasMarkerStream(), "Markers LSL", "Nome e tipo do outlet de markers preenchidos.");
        if (activeMode == ExperimentMode.GrazFeedback)
        {
            ChecklistRow(HasClassifierStream(), "Entrada LSL", "Nome e tipo do stream recebido pelo classificador.");
            ChecklistRow(HasConnectedClassifierStream(), "Entrada conectada", "Stream Classifications encontrada e inlet aberto.");
            ChecklistRow(HasRecentClassifierSample(), "Amostra recente", "Indicador de diagnostico: ultimo pacote ainda esta fresco.");
        }
        ChecklistRow(HasValidTimings(), "Tempos", "Baseline, fixation, cue, task e pausa numericos e coerentes.");
        ChecklistRow(HasValidCueClasses(false), "Classes", "Classes ativas com nomes, codigos unicos e alvos validos.");
        ChecklistRow(HasCueImagesForActiveClasses(), "Imagens", "Todas as classes ativas possuem imagem de cue existente.");
        ChecklistRow(HasGeneratedCurrentTrialPlan(), "Ordem", "Preview/ordem dos trials gerada com a configuracao atual.");
        if (activeMode == ExperimentMode.GrazFeedback)
            ChecklistRow(HasValidFeedbackCriteria(), "Feedback", "Gravidade, tolerancia e hold time validos.");
        GUILayout.EndVertical();
    }

    private void ChecklistRow(bool ok, string title, string detail)
    {
        var previousColor = GUI.backgroundColor;
        GUI.backgroundColor = ok ? new Color(0.1f, 0.42f, 0.22f, 0.9f) : new Color(0.47f, 0.18f, 0.13f, 0.9f);
        GUILayout.BeginHorizontal(GUI.skin.box);
        GUI.backgroundColor = previousColor;

        GUILayout.Label(ok ? "OK" : "!", ok ? checklistDoneStyle : checklistTodoStyle, GUILayout.Width(34f), GUILayout.Height(42f));
        GUILayout.BeginVertical();
        GUILayout.Label(title, fieldLabelStyle);
        GUILayout.Label(detail, checklistTextStyle);
        GUILayout.EndVertical();
        GUILayout.EndHorizontal();
    }

    private void DiagnosticRow(bool active, string title, string detail)
    {
        var previousColor = GUI.backgroundColor;
        GUI.backgroundColor = active ? new Color(0.1f, 0.42f, 0.22f, 0.9f) : new Color(0.2f, 0.28f, 0.34f, 0.9f);
        GUILayout.BeginHorizontal(GUI.skin.box);
        GUI.backgroundColor = previousColor;
        GUILayout.Label(active ? "ON" : "i", active ? checklistDoneStyle : checklistTodoStyle, GUILayout.Width(34f), GUILayout.Height(42f));
        GUILayout.BeginVertical();
        GUILayout.Label(title, fieldLabelStyle);
        GUILayout.Label(detail, checklistTextStyle);
        GUILayout.EndVertical();
        GUILayout.EndHorizontal();
    }

    private string PrimaryActionLabel()
    {
        if (activeMode == ExperimentMode.Capture)
            return "Abrir camera";
        if (activeMode == ExperimentMode.FreeLsl)
            return "Iniciar controle livre";
        if (activeMode == ExperimentMode.GrazAcquisition)
            return "Iniciar aquisicao";
        if (activeMode == ExperimentMode.GrazFeedback)
            return "Iniciar feedback";
        return "Rota preparada";
    }

    private string PrimaryActionTooltip()
    {
        if (activeMode == ExperimentMode.Capture)
            return "Entra no modo camera para configurar a vista e capturar imagens de cue/apresentacao.";
        if (activeMode == ExperimentMode.FreeLsl)
            return "Oculta o menu e inicia o controle continuo das pernas pelo LSL, sem qualquer fase do protocolo GRAZ.";
        if (activeMode == ExperimentMode.GrazAcquisition)
            return "Inicia o protocolo GRAZ de aquisicao com tela limpa, markers LSL e log experimental.";
        if (activeMode == ExperimentMode.GrazFeedback)
            return "Inicia o fluxo de GRAZ com feedback visual e controle por LSL.";
        return "Esta rota esta desenhada no menu, mas ainda sera implementada.";
    }

    private string GetModeTooltip(ExperimentMode mode)
    {
        if (mode == ExperimentMode.Capture)
            return "Modo livre do experimentador para ajustar camera, pernas e feedbacks visuais antes de tirar prints.";
        if (mode == ExperimentMode.FreeLsl)
            return "Familiarizacao livre: recebe classificacoes via LSL, controla as pernas e mantem os feedbacks escolhidos sempre visiveis, sem GRAZ.";
        if (mode == ExperimentMode.GrazAcquisition)
            return "Modo de coleta EEG/EMG: mostra fixation, cue, task e pausa, envia markers LSL e grava logs.";
        return "Modo de teste com feedback: recebe classe e probabilidade via LSL, move a perna somente na Task e compara com alvo/tolerancia/hold.";
    }

    private bool TooltipButton(string label, string tooltip, GUIStyle style, params GUILayoutOption[] options)
    {
        return GUILayout.Button(new GUIContent(label, tooltip), style, options);
    }

    private void DrawTooltip()
    {
        if (Event.current.type != EventType.Repaint)
            return;

        string currentTooltip = GUI.tooltip;
        if (string.IsNullOrWhiteSpace(currentTooltip))
        {
            hoveredTooltip = string.Empty;
            tooltipHoverStartTime = 0f;
            return;
        }

        if (currentTooltip != hoveredTooltip)
        {
            hoveredTooltip = currentTooltip;
            tooltipHoverStartTime = Time.unscaledTime;
            return;
        }

        if (Time.unscaledTime - tooltipHoverStartTime < TooltipDelaySeconds)
            return;

        Vector2 mouse = Event.current.mousePosition;
        float width = 430f;
        float height = tooltipStyle.CalcHeight(new GUIContent(currentTooltip), width);
        var rect = new Rect(mouse.x + 18f, mouse.y + 22f, width, height + 16f);

        if (rect.xMax > ReferenceWidth - 12f)
            rect.x = ReferenceWidth - rect.width - 12f;
        if (rect.yMax > ReferenceHeight - 12f)
            rect.y = mouse.y - rect.height - 18f;

        GUI.Label(rect, currentTooltip, tooltipStyle);
    }

    private void Card(string title, string subtitle)
    {
        GUILayout.Space(6f);
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label(title, sectionTitleStyle);
        GUILayout.Label(subtitle, bodyStyle);
        GUILayout.EndVertical();
    }

    private void Future(string title, string text)
    {
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label(title + "  |  futuro", fieldLabelStyle);
        GUILayout.Label(text, bodyStyle);
        GUILayout.EndVertical();
    }

    private string Field(string label, string value, string help)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, fieldLabelStyle, GUILayout.Width(190f));
        var newValue = GUILayout.TextField(value, textFieldStyle, GUILayout.Height(46f));
        GUILayout.Label(help, bodyStyle, GUILayout.Width(210f));
        GUILayout.EndHorizontal();
        return newValue;
    }

    private string DrawImagePathRow(string label, string path, string dialogTitle, Action<string> assignPath)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, fieldLabelStyle, GUILayout.Width(190f));
        path = GUILayout.TextField(path, textFieldStyle, GUILayout.Height(40f));
        assignPath?.Invoke(path);
        if (TooltipButton("Escolher", "Abre o seletor de arquivo para escolher uma imagem.", buttonStyle, GUILayout.Width(96f), GUILayout.Height(36f)))
        {
            StartCoroutine(SelectImagePathCoroutine(assignPath, dialogTitle));
            GUIUtility.ExitGUI();
        }
        if (TooltipButton("Limpar", "Remove o caminho da imagem deste campo.", buttonStyle, GUILayout.Width(82f), GUILayout.Height(36f)))
        {
            path = "";
            assignPath?.Invoke(path);
        }
        GUILayout.EndHorizontal();
        return path;
    }

    private void DrawPhaseContentEditor(
        string phaseName,
        ref string imagePath,
        ref string text,
        ref Color textColor,
        ref string textSizePercent,
        string dialogTitle,
        Action<string> assignImagePath)
    {
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label(phaseName, fieldLabelStyle);
        imagePath = DrawImagePathRow("Imagem", imagePath, dialogTitle, assignImagePath);
        GUILayout.BeginHorizontal();
        GUILayout.Label("Texto", fieldLabelStyle, GUILayout.Width(190f));
        text = GUILayout.TextArea(text, textAreaStyle, GUILayout.Height(72f));
        GUILayout.EndHorizontal();
        textColor = DrawColorControls("Cor do texto", textColor, DefaultTextColorForPhase(phaseName));
        textSizePercent = Field("Tamanho texto (%)", textSizePercent, "percentual da altura da tela");
        GUILayout.EndVertical();
    }

    private Color DefaultTextColorForPhase(string phaseName)
    {
        if (phaseName.IndexOf("Sucesso", StringComparison.OrdinalIgnoreCase) >= 0)
            return new Color(0.08f, 0.48f, 0.18f, 1f);
        if (phaseName.IndexOf("Insucesso", StringComparison.OrdinalIgnoreCase) >= 0)
            return new Color(0.78f, 0.08f, 0.08f, 1f);
        return Color.black;
    }

    private Color DrawColorControls(string label, Color color, Color preset)
    {
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, fieldLabelStyle, GUILayout.Width(190f));
        GUILayout.Box("", GUILayout.Width(42f), GUILayout.Height(30f));
        var swatch = GUILayoutUtility.GetLastRect();
        if (Event.current.type == EventType.Repaint)
        {
            var previousColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(swatch, Texture2D.whiteTexture);
            GUI.color = previousColor;
        }
        GUILayout.Label("#" + ColorUtility.ToHtmlStringRGB(color), bodyStyle, GUILayout.Width(90f));
        if (TooltipButton("Preset", "Restaura a cor padrao deste controle.", buttonStyle, GUILayout.Width(84f), GUILayout.Height(34f)))
            color = preset;
        GUILayout.EndHorizontal();

        color.r = DrawColorSlider("R", color.r);
        color.g = DrawColorSlider("G", color.g);
        color.b = DrawColorSlider("B", color.b);
        color.a = 1f;
        GUILayout.EndVertical();
        return color;
    }

    private float DrawColorSlider(string channel, float value)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(channel + ": " + Mathf.RoundToInt(value * 255f), fieldLabelStyle, GUILayout.Width(70f));
        value = GUILayout.HorizontalSlider(value, 0f, 1f, GUILayout.Height(30f));
        GUILayout.EndHorizontal();
        return value;
    }

    private void ResetStimulusSettings()
    {
        backgroundImagePath = "";
        backgroundColor = new Color(0.78f, 0.78f, 0.78f, 1f);
        fixationImagePath = "";
        fixationColor = Color.red;
        baselineImagePath = "";
        baselineStimulusText = "";
        taskImagePath = "";
        taskStimulusText = "";
        pauseImagePath = "";
        pauseStimulusText = "";
        cueScreenPercentText = "72";
        cueColor = Color.white;
        fixationSizePercentText = "32";
        baselineTextColor = Color.black;
        baselineTextSizePercentText = "7";
        taskTextColor = Color.black;
        taskTextSizePercentText = "7";
        pauseTextColor = Color.black;
        pauseTextSizePercentText = "7";
        successTextColor = new Color(0.08f, 0.48f, 0.18f, 1f);
        successTextSizePercentText = "8";
        failureTextColor = new Color(0.78f, 0.08f, 0.08f, 1f);
        failureTextSizePercentText = "8";
        showHoldProgressFeedback = true;
        ApplyStimulusSettings();
        status = "Tela do sujeito restaurada para preset neutro.";
    }

    private void ApplyStimulusSettings()
    {
        var graz = GrazController.instance;
        if (graz == null)
        {
            status = "GrazController nao encontrado para aplicar tela do sujeito.";
            return;
        }

        graz.ConfigureStimulusSettings(new GrazStimulusSettings
        {
            backgroundImagePath = backgroundImagePath,
            backgroundColor = backgroundColor,
            fixationImagePath = fixationImagePath,
            fixationColor = fixationColor,
            baselineImagePath = baselineImagePath,
            baselineText = baselineStimulusText,
            taskImagePath = taskImagePath,
            taskText = taskStimulusText,
            pauseImagePath = pauseImagePath,
            pauseText = pauseStimulusText,
            cueScreenFraction = Mathf.Clamp(ParseFloat(cueScreenPercentText, 72f) / 100f, 0.25f, 0.9f),
            cueColor = cueColor,
            fixationScreenFraction = ParseScreenFraction(fixationSizePercentText, 32f),
            baselineTextColor = baselineTextColor,
            baselineTextScreenFraction = ParseTextScreenFraction(baselineTextSizePercentText, 7f),
            taskTextColor = taskTextColor,
            taskTextScreenFraction = ParseTextScreenFraction(taskTextSizePercentText, 7f),
            pauseTextColor = pauseTextColor,
            pauseTextScreenFraction = ParseTextScreenFraction(pauseTextSizePercentText, 7f)
        });

        status = "Tela do sujeito aplicada.";
    }

    private float ParseScreenFraction(string value, float fallbackPercent)
    {
        return Mathf.Clamp(ParseFloat(value, fallbackPercent) / 100f, 0.05f, 0.5f);
    }

    private float ParseTextScreenFraction(string value, float fallbackPercent)
    {
        return Mathf.Clamp(ParseFloat(value, fallbackPercent) / 100f, 0.025f, 0.2f);
    }

    private void ApplySessionFields()
    {
        UpdatePlannedTrialsText();
        if (ui.trialsField != null)
            ui.trialsField.text = trialsText;
        if (ui.inputStreamName != null)
            ui.inputStreamName.text = streamNameText;
        if (ui.inputStreamType != null)
            ui.inputStreamType.text = streamTypeText;
        ui.SetTrials(trialsText);
        ConfigureMarkerOutlet();
        ConfigureExperimentalLogger();
        status = "Campos de sessao/LSL aplicados.";
    }

    private void ApplyTimingFields()
    {
        if (ui._baselineField != null)
            ui._baselineField.text = baselineText;
        if (ui._fixationField != null)
            ui._fixationField.text = fixationText;
        if (ui._feedbackField != null)
            ui._feedbackField.text = taskText;
        if (ui._minPauseField != null)
            ui._minPauseField.text = minPauseText;
        if (ui._maxPauseField != null)
            ui._maxPauseField.text = maxPauseText;
        if (GrazController.instance != null)
        {
            float minPause = Mathf.Max(0f, ParseFloat(minPauseText, 1.5f));
            float maxPause = Mathf.Max(minPause, ParseFloat(maxPauseText, 3.5f));

            GrazController.instance.baselineDuration = Mathf.Max(0f, ParseFloat(baselineText, 10f));
            GrazController.instance.fixationDuration = Mathf.Max(0f, ParseFloat(fixationText, 3f));
            GrazController.instance.cueDuration = Mathf.Max(0f, ParseFloat(cueText, 1f));
            GrazController.instance.feedbackDuration = Mathf.Max(0f, ParseFloat(taskText, 3.75f));
            GrazController.instance.minPauseDuration = minPause;
            GrazController.instance.maxPauseDuration = maxPause;
            minPauseText = minPause.ToString("0.###", CultureInfo.InvariantCulture);
            maxPauseText = maxPause.ToString("0.###", CultureInfo.InvariantCulture);
        }
        ApplyStimulusSettings();
        status = "Tempos do GRAZ aplicados.";
    }

    private void ApplyFeedbackFields()
    {
        float gravity = Mathf.Max(0f, ParseFloat(gravityText, 9.81f));
        var gravidade = PrivateInput("gravidadeField");
        var margem = PrivateInput("margemField");
        var tempo = PrivateInput("tempoPrecisaoField");

        if (gravidade != null)
            gravidade.text = gravity.ToString(CultureInfo.CurrentCulture);
        if (margem != null)
            margem.text = toleranceText;
        if (tempo != null)
            tempo.text = holdTimeText;
        Physics.gravity = new Vector3(Physics.gravity.x, -gravity, Physics.gravity.z);
        status = "Criterios de feedback aplicados.";
    }

    private void DrawClassificationInputPanel()
    {
        var receiver = ui != null ? ui.GetComponent<ClassificationLSLReceiver>() : null;
        bool connected = receiver != null && receiver.IsConnected;
        bool receiving = receiver != null && receiver.HasFreshSignal;
        bool resolving = receiver != null && receiver.IsResolving;
        string connectionTitle = "Stream LSL de entrada desconectada";
        if (receiving)
            connectionTitle = "Stream LSL de entrada recebendo sinal";
        else if (resolving)
            connectionTitle = "Procurando stream LSL de entrada";
        else if (connected)
            connectionTitle = "Stream LSL conectada, sem amostra recente";

        if (receiver != null)
        {
            classificationTestStatus = receiver.ConnectionStatus;
            if (!string.IsNullOrWhiteSpace(status)
                && (status.StartsWith("Procurando ", StringComparison.OrdinalIgnoreCase)
                    || status.IndexOf("Classifications", StringComparison.OrdinalIgnoreCase) >= 0
                    || status.IndexOf("Stream de entrada", StringComparison.OrdinalIgnoreCase) >= 0))
            {
                status = receiver.ConnectionStatus;
            }
        }

        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label(connectionTitle, receiving ? statusStyle : bodyStyle);
        GUILayout.Label(receiver != null ? receiver.ConnectionStatus : classificationTestStatus, bodyStyle);

        if (receiver != null && receiver.HasPrediction)
        {
            ClassificationPrediction prediction = receiver.LatestPrediction;
            GUILayout.Label($"Ultimo pacote: classe {prediction.classCode} | probabilidade {prediction.probability:0.000} | idade {receiver.SignalAge:0.000}s | amostras {receiver.ReceivedSamplesCount}", bodyStyle);
        }
        else if (connected)
        {
            GUILayout.Label("Nenhuma amostra recebida ainda. Inicie o simulador/classificador ou clique em Conectar entrada para refazer a busca.", bodyStyle);
        }

        GUILayout.BeginHorizontal();
        if (TooltipButton("Conectar entrada", "Refaz a busca pela stream LSL Classifications. Ela deve ter dois canais float32: codigo da classe e probabilidade.", buttonStyle, GUILayout.Height(42f)))
            ConnectClassificationStream();
        if (TooltipButton("Desconectar entrada", "Fecha apenas o inlet de classificacoes. O outlet de markers do Unity continua independente.", buttonStyle, GUILayout.Height(42f)))
            DisconnectClassificationStream();
        GUILayout.EndHorizontal();
        GUILayout.EndVertical();
    }

    private ClassificationLSLReceiver ConfigureClassificationReceiver()
    {
        if (ui == null)
            return null;

        var receiver = ui.GetComponent<ClassificationLSLReceiver>();
        if (receiver == null)
            receiver = ui.gameObject.AddComponent<ClassificationLSLReceiver>();

        receiver.Configure(streamNameText, streamTypeText, Mathf.Max(0.05f, ParseFloat(inputStaleSecondsText, 1.25f)));
        return receiver;
    }

    private void ConnectClassificationStream()
    {
        var receiver = ConfigureClassificationReceiver();
        if (receiver == null)
        {
            classificationTestStatus = "Nao foi possivel criar o receptor LSL.";
            status = classificationTestStatus;
            return;
        }

        receiver.Reconnect();
        classificationTestStatus = $"Procurando {streamNameText} / {streamTypeText}...";
        status = classificationTestStatus;
    }

    private void DisconnectClassificationStream()
    {
        var receiver = ui != null ? ui.GetComponent<ClassificationLSLReceiver>() : null;
        receiver?.Disconnect();
        classificationTestStatus = "Stream de entrada desconectada.";
        status = classificationTestStatus;
    }

    private void ConfigureMarkerOutlet()
    {
        var outlet = LSLMarkerOutlet.Instance;
        if (outlet == null && ui != null)
            outlet = ui.gameObject.AddComponent<LSLMarkerOutlet>();
        if (outlet != null)
            outlet.Configure(markerStreamNameText, markerStreamTypeText, numericMarkersOnly);
    }

    private void DrawMarkerTestPanel()
    {
        var outlet = LSLMarkerOutlet.Instance;
        bool published = outlet != null && outlet.IsReady;

        GUILayout.Space(6f);
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("Teste LSL markers", sectionTitleStyle);
        GUILayout.Label("Use junto com lsl_tests/marker_inlet_monitor.py para confirmar a recepcao real.", bodyStyle);

        string publishedStatus = published
            ? "Stream LSL markers publicada: " + outlet.StreamName + " / " + outlet.StreamType + " | enviados: " + outlet.SentMarkersCount
            : "Stream LSL markers desconectada/nao publicada.";
        GUILayout.Label(publishedStatus, published ? statusStyle : bodyStyle);
        GUILayout.Label(markerTestStatus, bodyStyle);

        GUILayout.BeginHorizontal();
        if (TooltipButton("Publicar stream", "Cria/configura o outlet LSL de markers com o nome, tipo e formato definidos acima.", buttonStyle, GUILayout.Height(42f)))
            PublishMarkerStreamForTest();
        if (TooltipButton("Enviar marker teste", "Envia o marker 9999 pelo outlet LSL para confirmar no receptor Python.", buttonStyle, GUILayout.Height(42f)))
            SendMarkerTest();
        GUILayout.EndHorizontal();
        GUILayout.EndVertical();
    }

    private void PublishMarkerStreamForTest()
    {
        ConfigureMarkerOutlet();

        var outlet = LSLMarkerOutlet.Instance;
        if (outlet == null || !outlet.IsReady)
        {
            markerTestStatus = "Stream LSL markers desconectada/nao publicada.";
            status = markerTestStatus;
            return;
        }

        outlet.Publish();
        markerTestStatus = "Stream LSL markers publicada. Abra o receptor Python e envie o marker teste.";
        status = markerTestStatus;
    }

    private void SendMarkerTest()
    {
        ConfigureMarkerOutlet();

        var outlet = LSLMarkerOutlet.Instance;
        if (outlet == null || !outlet.IsReady)
        {
            markerTestStatus = "Stream LSL markers desconectada/nao publicada.";
            status = markerTestStatus;
            return;
        }

        outlet.SendTestMarker(MarkerTestCode);
        markerTestStatus = "Marker teste enviado: " + MarkerTestCode + ". Confirme no receptor Python.";
        status = markerTestStatus;
    }

    private void ConfigureExperimentalLogger()
    {
        if (ui == null)
            return;

        var config = new ExperimentalLogSessionConfig
        {
            experimentName = "Experiment 2",
            mode = activeMode.ToString(),
            subjectId = subjectIdText,
            sessionId = sessionIdText,
            run = runText,
            presetName = presetNameText,
            logPath = ui._pathLog,
            inputStreamName = streamNameText,
            inputStreamType = streamTypeText,
            markerStreamName = markerStreamNameText,
            markerStreamType = markerStreamTypeText,
            totalTrials = ParsePositiveInt(trialsText, 1),
            baselineDuration = ParseFloat(baselineText, 10f),
            fixationDuration = ParseFloat(fixationText, 3f),
            cueDuration = ParseFloat(cueText, 1f),
            feedbackDuration = ParseFloat(taskText, 3.75f),
            minPauseDuration = ParseFloat(minPauseText, 1.5f),
            maxPauseDuration = ParseFloat(maxPauseText, 3.5f),
            gravity = ParseFloat(gravityText, 9.81f),
            defaultTolerance = ParseFloat(toleranceText, 5f),
            defaultHoldTime = ParseFloat(holdTimeText, 3f),
            maxMovementSpeed = ParseFloat(maxMovementSpeedText, 60f),
            inputStaleSeconds = ParseFloat(inputStaleSecondsText, 1.25f),
            showFeedbackGauge = showFeedbackGauge,
            showFeedbackSideView = showFeedbackSideView,
            showGhostTarget = showFeedbackGhost,
            showResultFeedback = showResultFeedback,
            successFeedbackDuration = ParseFloat(successDurationText, 1f),
            failureFeedbackDuration = ParseFloat(failureDurationText, 1f),
            shuffleTrials = shuffleTrials,
            randomSeed = randomSeedText
        };

        ui.ConfigureExperimentalLogger(config);
    }

    private void StartSelectedMode()
    {
        if (secondDisplayEnabled && (secondDisplay == null || !secondDisplay.IsOpen))
        {
            status = "Segunda tela ativada: conecte dois monitores e abra o build Windows antes de iniciar.";
            return;
        }
        if (activeMode == ExperimentMode.FreeLsl)
        {
            StartFreeLslMode();
            return;
        }

        ApplySessionFields();
        ApplyTimingFields();
        ApplyFeedbackFields();

        if (activeMode == ExperimentMode.Capture)
        {
            if (secondDisplayEnabled)
                secondDisplay.SetWaiting();
            var photoMode = ui.GetComponent<PhotoMode>();
            if (photoMode != null)
            {
                photoMode.EntryPhotoMode();
                GUIUtility.ExitGUI();
            }
            return;
        }

        if (activeMode == ExperimentMode.GrazAcquisition)
        {
            StartGrazAcquisition();
            return;
        }

        if (activeMode == ExperimentMode.GrazFeedback)
        {
            StartGrazFeedback();
            return;
        }

    }

    private void StartFreeLslMode()
    {
        if (!ValidateFreeLslConfiguration())
            return;

        ClassificationLSLReceiver receiver = ConfigureClassificationReceiver();
        if (receiver == null || !receiver.IsConnected)
        {
            status = "Stream Classifications desconectada. Inicie o classificador e clique em Conectar entrada.";
            return;
        }

        var controller = ui.GetComponent<FreeLslFamiliarizationController>();
        if (controller == null)
            controller = ui.gameObject.AddComponent<FreeLslFamiliarizationController>();

        var overlay = EnsureFeedbackOverlay();
        if (overlay == null)
        {
            status = "Overlay de feedback nao encontrado.";
            return;
        }

        if (ui.configsUI != null)
            ui.configsUI.SetActive(false);
        if (ui.spaceBar != null)
            ui.spaceBar.SetActive(false);
        if (ui.statusUI != null && ui.statusUI.configs != null)
            ui.statusUI.configs.SetActive(false);

        Cursor.visible = false;
        status = "Controle livre LSL em andamento. Pressione ESC para sair.";
        enabled = false;

        if (secondDisplayEnabled)
            secondDisplay.SetFreeLsl();

        controller.StartMode(
            receiver,
            overlay,
            freeLslCommands,
            BuildFreeLslRuntimeSettings(),
            OnFreeLslStopped);
    }

    private FreeLslRuntimeSettings BuildFreeLslRuntimeSettings()
    {
        return new FreeLslRuntimeSettings
        {
            gravityFallSpeed = Mathf.Max(0f, ParseFloat(gravityText, 9.81f)),
            maxMovementSpeed = Mathf.Max(0.1f, ParseFloat(maxMovementSpeedText, 60f)),
            showLeft = FreeLslShowsLeft(),
            showRight = FreeLslShowsRight(),
            showGauge = freeLslShowGauge,
            showSideView = freeLslShowSideView,
            showGhostTarget = freeLslShowGhost,
            showToleranceBand = freeLslShowToleranceBand,
            leftTargetAngle = Mathf.Clamp(ParseFloat(freeLslLeftTargetText, 45f), 0f, 90f),
            leftTolerance = Mathf.Max(0f, ParseFloat(freeLslLeftToleranceText, 5f)),
            rightTargetAngle = Mathf.Clamp(ParseFloat(freeLslRightTargetText, 45f), 0f, 90f),
            rightTolerance = Mathf.Max(0f, ParseFloat(freeLslRightToleranceText, 5f))
        };
    }

    private void OnFreeLslStopped()
    {
        if (secondDisplayEnabled)
            secondDisplay.SetWaiting();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        enabled = true;
        HideLegacyConfigPanel();
        status = "Controle livre encerrado. A stream LSL permanece conectada.";
    }

    private void StartGrazAcquisition()
    {
        GenerateTrialPreview();
        if (!ValidateFullConfiguration())
            return;

        ConfigureMarkerOutlet();
        ConfigureExperimentalLogger();

        var logger = ExperimentalSessionLogger.Instance;
        if (logger == null)
        {
            status = "Logger experimental nao encontrado; aquisicao cancelada.";
            return;
        }
        try
        {
            logger.BeginSession("graz_acquisition_start");
            ExportTrialPlanJson(Path.ChangeExtension(logger.EventPath, ".plan.json"));
        }
        catch (Exception exc)
        {
            logger.EndSession("trial_plan_export_failed");
            status = "Falha ao salvar plano de trials: " + exc.Message;
            Debug.LogError(status);
            return;
        }

        if (ui != null)
        {
            if (ui.configsUI != null)
                ui.configsUI.SetActive(false);
            if (ui.spaceBar != null)
                ui.spaceBar.SetActive(false);
            if (ui.statusUI != null && ui.statusUI.configs != null)
                ui.statusUI.configs.SetActive(false);
            if (ui.startGame != null)
            {
                ui.startGame.Started = true;
                ui.startGame.isInitialize = true;
                ui.startGame.actTrials = 0;
            }
        }

        enabled = false;
        status = "Aquisicao GRAZ em andamento.";

        var graz = GrazController.instance;
        if (graz == null)
        {
            status = "GrazController nao encontrado.";
            enabled = true;
            return;
        }

        var trials = BuildAcquisitionTrials();
        if (secondDisplayEnabled)
            secondDisplay.SetGraz(trials.Count);
        graz.StartAcquisitionProtocol(trials, OnGrazAcquisitionFinished, OnGrazAcquisitionAborted);
    }

    private void StartGrazFeedback()
    {
        GenerateTrialPreview();
        if (!ValidateFullConfiguration())
            return;

        var receiver = ConfigureClassificationReceiver();
        if (receiver == null || !receiver.IsConnected)
        {
            status = "Stream Classifications desconectada. Inicie o simulador/classificador e clique em Conectar entrada.";
            return;
        }

        ConfigureMarkerOutlet();
        ConfigureExperimentalLogger();

        var logger = ExperimentalSessionLogger.Instance;
        if (logger != null)
            logger.BeginSession("graz_feedback_start");

        if (ui != null)
        {
            if (ui.configsUI != null)
                ui.configsUI.SetActive(false);
            if (ui.spaceBar != null)
                ui.spaceBar.SetActive(false);
            if (ui.statusUI != null && ui.statusUI.configs != null)
                ui.statusUI.configs.SetActive(false);
            if (ui.startGame != null)
            {
                ui.startGame.Started = true;
                ui.startGame.isInitialize = true;
                ui.startGame.actTrials = 0;
            }
        }

        var graz = GrazController.instance;
        if (graz == null)
        {
            logger?.EndSession("graz_feedback_start_failed_no_controller");
            status = "GrazController nao encontrado.";
            return;
        }

        var overlay = EnsureFeedbackOverlay();
        enabled = false;
        status = "GRAZ feedback em andamento.";

        var trials = BuildAcquisitionTrials();
        if (secondDisplayEnabled)
            secondDisplay.SetGraz(trials.Count);
        graz.StartFeedbackProtocol(
            trials,
            BuildFeedbackCommandClasses(),
            receiver,
            BuildFeedbackRuntimeSettings(),
            overlay,
            OnGrazFeedbackFinished,
            OnGrazFeedbackAborted);
    }

    private PhotoModeFeedbackOverlay EnsureFeedbackOverlay()
    {
        if (ui == null)
            return null;

        var photoMode = ui.GetComponent<PhotoMode>();
        if (photoMode != null)
            return photoMode.GetFeedbackOverlay();

        var overlay = ui.GetComponent<PhotoModeFeedbackOverlay>();
        if (overlay == null)
            overlay = ui.gameObject.AddComponent<PhotoModeFeedbackOverlay>();
        return overlay;
    }

    private GrazFeedbackRuntimeSettings BuildFeedbackRuntimeSettings()
    {
        return new GrazFeedbackRuntimeSettings
        {
            gravityFallSpeed = Mathf.Max(0f, ParseFloat(gravityText, 9.81f)),
            maxMovementSpeed = Mathf.Max(0.1f, ParseFloat(maxMovementSpeedText, 60f)),
            inputStaleSeconds = Mathf.Max(0.05f, ParseFloat(inputStaleSecondsText, 1.25f)),
            showGauge = showFeedbackGauge,
            showSideView = showFeedbackSideView,
            showGhostTarget = showFeedbackGhost,
            showHoldProgress = showHoldProgressFeedback,
            showResultFeedback = showResultFeedback,
            successImagePath = successImagePath,
            successText = successFeedbackText,
            successTextColor = successTextColor,
            successTextScreenFraction = ParseTextScreenFraction(successTextSizePercentText, 8f),
            successDuration = Mathf.Max(0f, ParseFloat(successDurationText, 1f)),
            failureImagePath = failureImagePath,
            failureText = failureFeedbackText,
            failureTextColor = failureTextColor,
            failureTextScreenFraction = ParseTextScreenFraction(failureTextSizePercentText, 8f),
            failureDuration = Mathf.Max(0f, ParseFloat(failureDurationText, 1f))
        };
    }

    private List<GrazAcquisitionTrial> BuildAcquisitionTrials()
    {
        var trials = new List<GrazAcquisitionTrial>(generatedTrialPlan.Count);
        foreach (var trial in generatedTrialPlan)
        {
            trials.Add(new GrazAcquisitionTrial
            {
                trialIndex = trial.trialIndex,
                subjectId = trial.subjectId,
                sessionId = trial.sessionId,
                run = trial.run,
                classId = trial.classId,
                label = trial.label,
                markerCode = trial.markerCode,
                cueImagePath = trial.cueImagePath,
                description = trial.description,
                targetAngle = trial.targetAngle,
                tolerance = trial.tolerance,
                holdTime = trial.holdTime,
                legSide = trial.legSide,
                randomSeed = trial.randomSeed,
                shuffled = trial.shuffled
            });
        }

        return trials;
    }

    private List<GrazAcquisitionTrial> BuildFeedbackCommandClasses()
    {
        var commands = new List<GrazAcquisitionTrial>(cueClasses.Count);
        for (int i = 0; i < cueClasses.Count; i++)
        {
            var cueClass = cueClasses[i];
            commands.Add(new GrazAcquisitionTrial
            {
                trialIndex = i + 1,
                subjectId = subjectIdText,
                sessionId = sessionIdText,
                run = runText,
                classId = cueClass.classId,
                label = cueClass.label,
                markerCode = ParseInt(cueClass.markerCode, 1000 + i),
                cueImagePath = cueClass.cueImagePath,
                description = cueClass.description,
                targetAngle = ParseFloat(cueClass.targetAngle, 0f),
                tolerance = ParseFloat(cueClass.tolerance, ParseFloat(toleranceText, 5f)),
                holdTime = ParseFloat(cueClass.holdTime, ParseFloat(holdTimeText, 3f)),
                legSide = cueClass.legSide,
                randomSeed = ParseInt(randomSeedText, Environment.TickCount),
                shuffled = shuffleTrials
            });
        }

        return commands;
    }

    private void OnGrazAcquisitionFinished()
    {
        if (secondDisplayEnabled)
            secondDisplay.SetCompleted(false);
        RestoreAfterGrazAcquisition();
        status = "Aquisicao GRAZ concluida. Log salvo.";
    }

    private void OnGrazAcquisitionAborted()
    {
        if (secondDisplayEnabled)
            secondDisplay.SetCompleted(true);
        RestoreAfterGrazAcquisition();
        status = "Aquisicao GRAZ interrompida. Log parcial salvo.";
    }

    private void OnGrazFeedbackFinished()
    {
        if (secondDisplayEnabled)
            secondDisplay.SetCompleted(false);
        RestoreAfterGrazAcquisition();
        status = "GRAZ feedback concluido. Log de desempenho salvo.";
    }

    private void OnGrazFeedbackAborted()
    {
        if (secondDisplayEnabled)
            secondDisplay.SetCompleted(true);
        RestoreAfterGrazAcquisition();
        status = "GRAZ feedback interrompido. Log parcial salvo.";
    }

    private void RestoreAfterGrazAcquisition()
    {
        if (ui != null)
        {
            if (ui.configsUI != null)
                ui.configsUI.SetActive(false);
            if (ui.statusUI != null && ui.statusUI.configs != null)
                ui.statusUI.configs.SetActive(false);
            if (ui.spaceBar != null)
                ui.spaceBar.SetActive(false);
            if (ui.startGame != null)
            {
                ui.startGame.Started = false;
                ui.startGame.isInitialize = false;
                ui.startGame.actTrials = 0;
            }

            var photoMode = ui.GetComponent<PhotoMode>();
            if (photoMode != null && photoMode.modeActive)
                photoMode.LeavePhotoMode();
        }

        enabled = true;
        HideLegacyConfigPanel();
    }

    private bool ShouldHideForPhotoMode()
    {
        var photoMode = ui.GetComponent<PhotoMode>();
        return photoMode != null && photoMode.modeActive;
    }

    private void RestoreLegacyPanel()
    {
        if (ui == null || ui.painelConfigs == null)
            return;

        ui.painelConfigs.alpha = 1f;
        ui.painelConfigs.interactable = true;
        ui.painelConfigs.blocksRaycasts = true;
        enabled = false;
    }

    private async void ReturnToExperimentSelection()
    {
        secondDisplay?.Close();
        HideLegacyConfigPanel();

        if (ui != null)
        {
            if (ui.configsUI != null)
                ui.configsUI.SetActive(false);
            if (ui.spaceBar != null)
                ui.spaceBar.SetActive(false);
            if (ui.startGame != null)
                ui.startGame.Started = false;
        }

        enabled = false;

        var loader = FindAnyObjectByType<SceneLoader>();
        if (loader != null)
        {
            await loader.LoadSceneGroup(0);
            return;
        }

        SceneManager.LoadScene("Menu", LoadSceneMode.Single);
    }

    private TMP_InputField PrivateInput(string fieldName)
    {
        var field = typeof(GameUIDois).GetField(fieldName, PrivateInstance);
        return field != null ? field.GetValue(ui) as TMP_InputField : null;
    }

    private int ParseInt(string value, int fallback)
    {
        return int.TryParse(value, out int parsed) ? parsed : fallback;
    }

    private int ParsePositiveInt(string value, int fallback)
    {
        int parsed = ParseInt(value, fallback);
        return Mathf.Max(1, parsed);
    }

    private void EnsureStyles()
    {
        if (stylesReady)
            return;

        pageStyle = new GUIStyle(GUI.skin.box) { padding = new RectOffset(18, 18, 16, 16) };
        panelStyle = new GUIStyle(GUI.skin.box) { padding = new RectOffset(14, 14, 14, 14) };
        darkPanelStyle = new GUIStyle(GUI.skin.box) { padding = new RectOffset(14, 14, 14, 14) };
        darkPanelStyle.normal.background = Texture2D.grayTexture;

        titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 34, fontStyle = FontStyle.Bold, normal = { textColor = Color.black }, wordWrap = true };
        subtitleStyle = new GUIStyle(GUI.skin.label) { fontSize = 19, normal = { textColor = new Color(0.18f, 0.20f, 0.21f) }, wordWrap = true };
        sectionTitleStyle = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold, normal = { textColor = Color.white }, wordWrap = true };
        bodyStyle = new GUIStyle(GUI.skin.label) { fontSize = 17, normal = { textColor = Color.white }, wordWrap = true };
        fieldLabelStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold, normal = { textColor = Color.white }, wordWrap = true };
        fieldLabelStyle.wordWrap = true;
        statusStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleRight, normal = { textColor = new Color(0.05f, 0.42f, 0.46f) }, wordWrap = true };
        statusBoxStyle = new GUIStyle(GUI.skin.box)
        {
            padding = new RectOffset(12, 12, 8, 8),
            normal = { textColor = Color.white, background = Texture2D.grayTexture }
        };
        checklistDoneStyle = new GUIStyle(GUI.skin.box)
        {
            fontSize = 17,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white, background = Texture2D.grayTexture }
        };
        checklistTodoStyle = new GUIStyle(checklistDoneStyle)
        {
            normal = { textColor = Color.white, background = Texture2D.grayTexture }
        };
        checklistTextStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            normal = { textColor = new Color(0.86f, 0.88f, 0.88f) },
            wordWrap = true
        };
        buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 18, fontStyle = FontStyle.Bold, wordWrap = true };
        selectedButtonStyle = new GUIStyle(GUI.skin.button) { fontSize = 19, fontStyle = FontStyle.Bold, wordWrap = true };
        tooltipStyle = new GUIStyle(GUI.skin.box)
        {
            fontSize = 17,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
            wordWrap = true,
            padding = new RectOffset(12, 12, 8, 8),
            normal = { textColor = Color.white, background = Texture2D.grayTexture }
        };
        textFieldStyle = new GUIStyle(GUI.skin.textField)
        {
            fontSize = 18,
            alignment = TextAnchor.MiddleLeft,
            padding = new RectOffset(8, 8, 5, 5)
        };
        textAreaStyle = new GUIStyle(GUI.skin.textArea)
        {
            fontSize = 18,
            wordWrap = true,
            padding = new RectOffset(8, 8, 6, 6)
        };
        toggleStyle = new GUIStyle(GUI.skin.toggle)
        {
            fontSize = 17,
            wordWrap = true,
            padding = new RectOffset(22, 4, 3, 3)
        };
        toolbarStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 17,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };

        stylesReady = true;
    }
}
