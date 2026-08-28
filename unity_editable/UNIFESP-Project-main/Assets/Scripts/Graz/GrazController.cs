using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

[Serializable]
public class GrazStimulusSettings
{
    public string backgroundImagePath = "";
    public Color backgroundColor = new Color(0.78f, 0.78f, 0.78f, 1f);
    public string fixationImagePath = "";
    public Color fixationColor = Color.red;
    public string baselineImagePath = "";
    public string baselineText = "";
    public string taskImagePath = "";
    public string taskText = "";
    public string pauseImagePath = "";
    public string pauseText = "";
    public float cueScreenFraction = 0.68f;
    public Color cueColor = Color.white;
    public float fixationScreenFraction = 0.32f;
    public Color baselineTextColor = Color.black;
    public float baselineTextScreenFraction = 0.07f;
    public Color taskTextColor = Color.black;
    public float taskTextScreenFraction = 0.07f;
    public Color pauseTextColor = Color.black;
    public float pauseTextScreenFraction = 0.07f;
}

[Serializable]
public class GrazFeedbackRuntimeSettings
{
    public float gravityFallSpeed = 9.81f;
    public float maxMovementSpeed = 60f;
    public float inputStaleSeconds = 1.25f;
    public bool showGauge = true;
    public bool showSideView = true;
    public bool showGhostTarget = true;
    public bool showHoldProgress = true;
    public bool showResultFeedback = true;
    public string successImagePath = "";
    public string successText = "SUCESSO";
    public Color successTextColor = new Color(0.08f, 0.48f, 0.18f, 1f);
    public float successTextScreenFraction = 0.08f;
    public float successDuration = 1f;
    public string failureImagePath = "";
    public string failureText = "TENTE NOVAMENTE";
    public Color failureTextColor = new Color(0.78f, 0.08f, 0.08f, 1f);
    public float failureTextScreenFraction = 0.08f;
    public float failureDuration = 1f;
}

// Esse Script serve para controlar o protocolo GRAZ
public class GrazController : MonoBehaviour
{
    #region SINGLETON

    private static GrazController _instance;

    public static GrazController instance
    {
        get
        {
            if (_instance == null)
                _instance = FindAnyObjectByType<GrazController>();
            return _instance;
        }
    }

    #endregion

    [SerializeField] private CanvasGroup _group;

    [Header("Baseline Phase")]
    public float baselineDuration = 10f; // Duração da baseline

    [Header("Fixation Phase")]
    public float fixationDuration = 3f; // Duração da fixação
    public RawImage indicationImage; // Imagem de indicação

    [Header("Cue Phase")]
    public float cueDuration = 1f; // Duração do cue
    public RawImage cueImageDirection; // Imagem de direção
    public Texture defaultCueImage; // Imagem padrão

    [Header("Feedback Phase")]
    public float feedbackDuration = 3.75f; // Duração do feedback
    [HideInInspector] public bool OnFeedbackPhase = false; // Flag de fase de feedback

    [Header("Pause Phase")]
    public float minPauseDuration = 1.5f; // Pausa mínima
    public float maxPauseDuration = 3.5f; // Pausa máxima

    [HideInInspector] public float grazInterval;
    [HideInInspector] public bool inGrazProtocol;

    private AudioSource source;

    private Coroutine grazCoroutine;
    private Coroutine acquisitionCoroutine;
    private Action acquisitionAbortedCallback;
    private string activeProtocolLogName = "graz_acquisition";
    private bool showAbortConfirmation;
    private bool abortPauseActive;
    private float timeScaleBeforeAbortPrompt = 1f;

    private RealtimeLog realtimeLog; // Sistema de log
    private StartGame startGame; // Controlador do jogo
    private int globalTrialIndex;
    private readonly Dictionary<string, Texture2D> cueTextureCache = new Dictionary<string, Texture2D>();
    private readonly GrazStimulusSettings stimulusSettings = new GrazStimulusSettings();
    private bool useSubjectStimulusDisplay;
    private ClassificationLSLReceiver feedbackReceiver;
    private PhotoModeFeedbackOverlay feedbackOverlay;
    private GrazFeedbackRuntimeSettings feedbackRuntimeSettings;
    private readonly Dictionary<int, GrazAcquisitionTrial> feedbackClassMap = new Dictionary<int, GrazAcquisitionTrial>();
    private float feedbackLeftCommandAngle;
    private float feedbackRightCommandAngle;

    private Canvas subjectCanvas;
    private RawImage subjectBackground;
    private RawImage subjectImage;
    private TMP_Text subjectText;
    private TMP_Text fixationCross;
    private TMP_Text holdProgressText;

    private const float StimulusReferenceWidth = 1920f;
    private const float StimulusReferenceHeight = 1080f;

    private void Awake()
    {
        if (TryGetComponent(out AudioSource s))
            source = s;
        else
            source = gameObject.AddComponent<AudioSource>();

        indicationImage.enabled = false;
        cueImageDirection.enabled = false;
        ToggleCanvas(false);
    }

    private void Start()
    {
        if (realtimeLog == null)
            realtimeLog = RealtimeLog.instance;
        if (realtimeLog != null)
            realtimeLog._acerto = "NEUTRO";
    }

    // Ativa/desativa canvas
    public void ToggleCanvas(bool on)
    {
        _group.alpha = on == true ? _group.alpha = 1f : _group.alpha = 0f;
        _group.interactable = on;
    }

    public void StartProtocol() => grazCoroutine = StartCoroutine(GrazProtocol());

    private void Update()
    {
        if (!inGrazProtocol || acquisitionCoroutine == null)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
            ToggleAbortConfirmation();
    }

    private void OnGUI()
    {
        if (showAbortConfirmation)
            DrawAbortConfirmation();
    }

    public void StartAcquisitionProtocol(List<GrazAcquisitionTrial> trials, Action onCompleted = null, Action onAborted = null)
    {
        if (acquisitionCoroutine != null)
            StopCoroutine(acquisitionCoroutine);

        acquisitionAbortedCallback = onAborted;
        activeProtocolLogName = "graz_acquisition";
        showAbortConfirmation = false;
        RestoreTimeScaleAfterAbortPrompt();
        acquisitionCoroutine = StartCoroutine(GrazAcquisitionProtocol(trials, onCompleted));
    }

    public void StartFeedbackProtocol(
        List<GrazAcquisitionTrial> trials,
        List<GrazAcquisitionTrial> commandClasses,
        ClassificationLSLReceiver receiver,
        GrazFeedbackRuntimeSettings runtimeSettings,
        PhotoModeFeedbackOverlay overlay,
        Action onCompleted = null,
        Action onAborted = null)
    {
        if (acquisitionCoroutine != null)
            StopCoroutine(acquisitionCoroutine);

        feedbackReceiver = receiver;
        feedbackRuntimeSettings = runtimeSettings ?? new GrazFeedbackRuntimeSettings();
        feedbackOverlay = overlay;
        acquisitionAbortedCallback = onAborted;
        activeProtocolLogName = "graz_feedback";
        showAbortConfirmation = false;
        RestoreTimeScaleAfterAbortPrompt();
        acquisitionCoroutine = StartCoroutine(GrazFeedbackProtocol(trials, commandClasses, onCompleted));
    }

    public void ConfigureStimulusSettings(GrazStimulusSettings settings)
    {
        if (settings == null)
            return;

        stimulusSettings.backgroundImagePath = settings.backgroundImagePath ?? "";
        stimulusSettings.backgroundColor = settings.backgroundColor;
        stimulusSettings.fixationImagePath = settings.fixationImagePath ?? "";
        stimulusSettings.fixationColor = settings.fixationColor;
        stimulusSettings.baselineImagePath = settings.baselineImagePath ?? "";
        stimulusSettings.baselineText = settings.baselineText ?? "";
        stimulusSettings.taskImagePath = settings.taskImagePath ?? "";
        stimulusSettings.taskText = settings.taskText ?? "";
        stimulusSettings.pauseImagePath = settings.pauseImagePath ?? "";
        stimulusSettings.pauseText = settings.pauseText ?? "";
        stimulusSettings.cueScreenFraction = Mathf.Clamp(settings.cueScreenFraction, 0.25f, 0.9f);
        stimulusSettings.cueColor = settings.cueColor;
        stimulusSettings.fixationScreenFraction = Mathf.Clamp(settings.fixationScreenFraction, 0.05f, 0.5f);
        stimulusSettings.baselineTextColor = settings.baselineTextColor;
        stimulusSettings.baselineTextScreenFraction = Mathf.Clamp(settings.baselineTextScreenFraction, 0.025f, 0.2f);
        stimulusSettings.taskTextColor = settings.taskTextColor;
        stimulusSettings.taskTextScreenFraction = Mathf.Clamp(settings.taskTextScreenFraction, 0.025f, 0.2f);
        stimulusSettings.pauseTextColor = settings.pauseTextColor;
        stimulusSettings.pauseTextScreenFraction = Mathf.Clamp(settings.pauseTextScreenFraction, 0.025f, 0.2f);
    }

    // Define rotação da imagem de cue
    public void SetCueImageDirection(float dir)
    {
        cueImageDirection.rectTransform.eulerAngles = new Vector3(cueImageDirection.rectTransform.rotation.x, cueImageDirection.rectTransform.rotation.y, dir);
    }

    public void EndProtocol()
    {
        if (grazCoroutine != null)
        {
            StopCoroutine(grazCoroutine);
            grazCoroutine = null;
        }

        if (acquisitionCoroutine != null)
        {
            StopCoroutine(acquisitionCoroutine);
            acquisitionCoroutine = null;
        }

        inGrazProtocol = false;
        OnFeedbackPhase = false;
        indicationImage.enabled = false;
        cueImageDirection.enabled = false;
        StopFeedbackVisualsAndMotion();
        HideSubjectStimulusDisplay();
        RestoreTimeScaleAfterAbortPrompt();
        showAbortConfirmation = false;
        ToggleCanvas(false);
    }

    public void AbortAcquisitionProtocol()
    {
        if (acquisitionCoroutine == null)
            return;

        StopCoroutine(acquisitionCoroutine);
        acquisitionCoroutine = null;

        inGrazProtocol = false;
        OnFeedbackPhase = false;
        useSubjectStimulusDisplay = false;
        showAbortConfirmation = false;
        RestoreTimeScaleAfterAbortPrompt();

        indicationImage.enabled = false;
        cueImageDirection.enabled = false;
        StopFeedbackVisualsAndMotion();
        HideSubjectStimulusDisplay();
        ToggleCanvas(false);

        ExperimentalSessionLogger.Instance?.EndSession(activeProtocolLogName + "_aborted");
        acquisitionAbortedCallback?.Invoke();
        Debug.Log("Protocolo GRAZ interrompido prematuramente pelo operador.");
    }

    private void ToggleAbortConfirmation()
    {
        if (showAbortConfirmation)
        {
            CloseAbortConfirmation();
            return;
        }

        showAbortConfirmation = true;
        timeScaleBeforeAbortPrompt = Time.timeScale;
        Time.timeScale = 0f;
        abortPauseActive = true;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private void CloseAbortConfirmation()
    {
        showAbortConfirmation = false;
        RestoreTimeScaleAfterAbortPrompt();
    }

    private void RestoreTimeScaleAfterAbortPrompt()
    {
        if (!abortPauseActive)
            return;

        Time.timeScale = Mathf.Approximately(timeScaleBeforeAbortPrompt, 0f) ? 1f : timeScaleBeforeAbortPrompt;
        abortPauseActive = false;
    }

    private void DrawAbortConfirmation()
    {
        int previousDepth = GUI.depth;
        GUI.depth = -10000;
        var overlay = new Rect(0f, 0f, Screen.width, Screen.height);
        var previousColor = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.55f);
        GUI.DrawTexture(overlay, Texture2D.whiteTexture);
        GUI.color = previousColor;

        float width = Mathf.Min(560f, Screen.width * 0.82f);
        float height = 220f;
        var dialog = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);

        GUIStyle boxStyle = new GUIStyle(GUI.skin.box)
        {
            padding = new RectOffset(24, 24, 22, 22)
        };
        GUIStyle title = new GUIStyle(GUI.skin.label)
        {
            fontSize = 22,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true,
            normal = { textColor = Color.white }
        };
        GUIStyle body = new GUIStyle(GUI.skin.label)
        {
            fontSize = 15,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true,
            normal = { textColor = Color.white }
        };
        GUIStyle button = new GUIStyle(GUI.skin.button)
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold
        };

        GUILayout.BeginArea(dialog, boxStyle);
        GUILayout.Label("Deseja realmente terminar prematuramente a sessao?", title, GUILayout.Height(64f));
        GUILayout.Label("Aperte ESC novamente para voltar ao experimento. A sessao so sera encerrada se voce clicar em Sim.", body, GUILayout.Height(52f));
        GUILayout.FlexibleSpace();
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Nao, voltar", button, GUILayout.Height(44f)))
            CloseAbortConfirmation();
        if (GUILayout.Button("Sim, encerrar sessao", button, GUILayout.Height(44f)))
            AbortAcquisitionProtocol();
        GUILayout.EndHorizontal();
        GUILayout.EndArea();
        GUI.depth = previousDepth;
    }

    // Protocolo principal Graz
    private IEnumerator GrazProtocol()
    {
        Debug.Log($"Protocolo Graz iniciado");

        if (startGame == null)
            startGame = StartGame.instance;

        int trials = startGame.trials;

        yield return BaselinePhase();

        realtimeLog._acerto = "NEUTRO";

        inGrazProtocol = true;
        globalTrialIndex = 0;

        int cueCount = 0;

        var cueConfigs = UIConfigs.instance.cueConfigs;

        // Loop por todas as configurações de cue
        while (cueCount < UIConfigs.instance.cueCount)
        {
            realtimeLog._classe = cueCount;
            var config = cueConfigs[cueCount].GetComponent<CueConfigs>();
            cueDuration = string.IsNullOrEmpty(config.field.text) ? 5f : float.Parse(config.field.text);
            config.SetCueImage();

            UIConfigs.instance.SetLegType(config.dropdown);

            // Configura target baseado no tipo de UI
            if (UIConfigs.instance._ui == UIModo.Dois)
            {
                if (UIConfigs.instance._legType == LegType.LEFT)
                {
                    realtimeLog._target = "ESQUERDA";
                    SetCueImageDirection(180f);
                }
                else
                {
                    realtimeLog._target = "DIREITA";
                    SetCueImageDirection(0f);
                }
            }

            // Loop por todos os trials
            while (startGame.actTrials < trials && StartGame.instance.Started)
            {
                int trialIndex = ++globalTrialIndex;
                int markerCode = 1000 + cueCount;
                float targetAngle = GetCurrentTargetAngle();
                float holdTime = GetCurrentHoldTime();
                ExperimentalSessionLogger.Instance?.LogTrialStart(trialIndex, cueCount, realtimeLog._target, markerCode, targetAngle, 0f, holdTime);
                realtimeLog._tentativa = startGame.actTrials;
                Debug.Log($"Trial {StartGame.instance.actTrials} / {trials}");

                yield return FixationPhase();

                yield return CuePhase(cueCount);

                yield return FeedbackPhase();
                bool success = realtimeLog._acerto == "ACERTOU";
                ExperimentalSessionLogger.Instance?.LogTrialEnd(realtimeLog._acerto, success);
                realtimeLog._acerto = "NEUTRO";
                yield return PausePhase();

                startGame.actTrials++;
            }

            cueCount++;
            StartGame.instance.actTrials = 0;
        }

        inGrazProtocol = false;

        Debug.Log($"Protocolo Graz concluido");

        StartGame.instance.RestartGame();
    }
    private IEnumerator GrazAcquisitionProtocol(List<GrazAcquisitionTrial> trials, Action onCompleted)
    {
        Debug.Log("Protocolo GRAZ de aquisicao iniciado");

        if (trials == null || trials.Count == 0)
        {
            Debug.LogWarning("Plano de trials GRAZ vazio.");
            onCompleted?.Invoke();
            yield break;
        }

        if (realtimeLog == null)
            realtimeLog = RealtimeLog.instance;

        useSubjectStimulusDisplay = true;
        EnsureSubjectStimulusDisplay();
        ShowSubjectStimulusDisplay(true);

        ToggleCanvas(false);
        indicationImage.enabled = false;
        cueImageDirection.enabled = false;
        inGrazProtocol = true;
        OnFeedbackPhase = false;
        globalTrialIndex = 0;

        yield return BaselinePhase();

        for (int i = 0; i < trials.Count; i++)
        {
            var trial = trials[i];
            globalTrialIndex = trial.trialIndex > 0 ? trial.trialIndex : i + 1;

            if (realtimeLog != null)
            {
                realtimeLog._tentativa = globalTrialIndex;
                realtimeLog._classe = i;
                realtimeLog._target = trial.label;
                realtimeLog._acerto = "AQUISICAO";
            }

            ExperimentalSessionLogger.Instance?.LogTrialStart(
                globalTrialIndex,
                i,
                trial.label,
                trial.markerCode,
                trial.targetAngle,
                trial.tolerance,
                trial.holdTime);

            yield return FixationPhase();
            yield return CuePhase(trial, i);
            yield return AcquisitionTaskPhase(trial);
            yield return PausePhase();

            ExperimentalSessionLogger.Instance?.LogTrialEnd("AQUISICAO", true);
        }

        inGrazProtocol = false;
        indicationImage.enabled = false;
        cueImageDirection.enabled = false;
        useSubjectStimulusDisplay = false;
        HideSubjectStimulusDisplay();
        ToggleCanvas(false);
        ExperimentalSessionLogger.Instance?.EndSession("graz_acquisition_completed");
        acquisitionCoroutine = null;

        Debug.Log("Protocolo GRAZ de aquisicao concluido");
        onCompleted?.Invoke();
    }

    private IEnumerator GrazFeedbackProtocol(List<GrazAcquisitionTrial> trials, List<GrazAcquisitionTrial> commandClasses, Action onCompleted)
    {
        Debug.Log("Protocolo GRAZ com feedback iniciado");

        if (trials == null || trials.Count == 0 || feedbackReceiver == null || !feedbackReceiver.IsConnected)
        {
            Debug.LogWarning("Nao foi possivel iniciar o feedback: plano vazio ou stream Classifications desconectada.");
            onCompleted?.Invoke();
            yield break;
        }

        if (realtimeLog == null)
            realtimeLog = RealtimeLog.instance;

        BuildFeedbackClassMap(commandClasses != null && commandClasses.Count > 0 ? commandClasses : trials);

        ReadCurrentCommandAngles();
        useSubjectStimulusDisplay = true;
        EnsureSubjectStimulusDisplay();
        ShowSubjectStimulusDisplay(true);
        ToggleCanvas(false);
        indicationImage.enabled = false;
        cueImageDirection.enabled = false;
        inGrazProtocol = true;
        OnFeedbackPhase = false;
        globalTrialIndex = 0;

        yield return BaselinePhase();

        for (int i = 0; i < trials.Count; i++)
        {
            GrazAcquisitionTrial trial = trials[i];
            globalTrialIndex = trial.trialIndex > 0 ? trial.trialIndex : i + 1;

            if (realtimeLog != null)
            {
                realtimeLog._tentativa = globalTrialIndex;
                realtimeLog._classe = i;
                realtimeLog._target = trial.label;
                realtimeLog._acerto = "EM_ANDAMENTO";
            }

            ExperimentalSessionLogger.Instance?.LogTrialStart(
                globalTrialIndex,
                i,
                trial.label,
                trial.markerCode,
                trial.targetAngle,
                trial.tolerance,
                trial.holdTime);

            yield return FixationPhase();
            yield return CuePhase(trial, i);

            bool success = false;
            float timeInTarget = 0f;
            yield return FeedbackTaskPhase(trial, (result, heldTime) =>
            {
                success = result;
                timeInTarget = heldTime;
            });

            string resultLabel = success ? "SUCCESS" : "FAILURE";
            ExperimentalSessionLogger.Instance?.LogResult(success, resultLabel, $"time_in_target={timeInTarget:0.###}");
            ExperimentalSessionLogger.Instance?.LogTrialEnd(resultLabel, success);

            if (realtimeLog != null)
                realtimeLog._acerto = success ? "ACERTOU" : "ERROU";

            if (feedbackRuntimeSettings.showResultFeedback)
                yield return ResultFeedbackPhase(success);

            yield return PausePhase();
        }

        inGrazProtocol = false;
        OnFeedbackPhase = false;
        useSubjectStimulusDisplay = false;
        StopFeedbackVisualsAndMotion();
        HideSubjectStimulusDisplay();
        ToggleCanvas(false);
        ExperimentalSessionLogger.Instance?.EndSession("graz_feedback_completed");
        acquisitionCoroutine = null;

        Debug.Log("Protocolo GRAZ com feedback concluido");
        onCompleted?.Invoke();
    }

    private void BuildFeedbackClassMap(List<GrazAcquisitionTrial> commandClasses)
    {
        feedbackClassMap.Clear();
        if (commandClasses == null)
            return;

        for (int i = 0; i < commandClasses.Count; i++)
        {
            GrazAcquisitionTrial command = commandClasses[i];
            if (command == null || feedbackClassMap.ContainsKey(command.markerCode))
                continue;

            feedbackClassMap.Add(command.markerCode, command);
        }
    }

    private IEnumerator FeedbackTaskPhase(GrazAcquisitionTrial trial, Action<bool, float> completed)
    {
        Debug.Log("Inicio da task com feedback");

        if (realtimeLog != null)
        {
            realtimeLog._marcador = "TASK";
            realtimeLog._marcadorNum = 4;
        }

        if (useSubjectStimulusDisplay)
            ShowFeedbackTaskStimulus();

        float elapsed = 0f;
        float continuousTimeInTarget = 0f;
        float bestTimeInTarget = 0f;
        bool success = false;

        OnFeedbackPhase = true;

        EnsureRuntimeFeedbackOverlay();
        feedbackOverlay?.BeginRuntimeFeedback(
            feedbackRuntimeSettings.showGauge,
            feedbackRuntimeSettings.showSideView,
            feedbackRuntimeSettings.showGhostTarget);
        feedbackOverlay?.SetRuntimeTarget(trial.legSide, trial.targetAngle, trial.tolerance);
        feedbackOverlay?.SetRuntimeAngles(feedbackLeftCommandAngle, feedbackRightCommandAngle);
        if (feedbackRuntimeSettings.showHoldProgress)
            UpdateHoldProgress(0f, trial.holdTime, false);

        SendPhaseStart(4, "TASK", "task", $"duration={feedbackDuration};class={trial.label};target={trial.targetAngle};tolerance={trial.tolerance};hold={trial.holdTime}");

        while (elapsed < feedbackDuration)
        {
            float delta = Time.deltaTime;
            UpdateFeedbackLegMotion(delta);
            feedbackOverlay?.SetRuntimeAngles(feedbackLeftCommandAngle, feedbackRightCommandAngle);

            float measuredAngle = GetTrialCommandAngle(trial.legSide);
            bool insideTarget = Mathf.Abs(measuredAngle - trial.targetAngle) <= trial.tolerance;
            if (trial.legSide == GrazLegSide.Relaxed)
                insideTarget = feedbackLeftCommandAngle <= trial.tolerance && feedbackRightCommandAngle <= trial.tolerance;

            if (insideTarget)
            {
                continuousTimeInTarget += delta;
                bestTimeInTarget = Mathf.Max(bestTimeInTarget, continuousTimeInTarget);
                if (!success && continuousTimeInTarget >= trial.holdTime)
                {
                    success = true;
                    ExperimentalSessionLogger.Instance?.LogResult(true, "TARGET_REACHED", $"time_in_target={continuousTimeInTarget:0.###}");
                }
            }
            else
            {
                continuousTimeInTarget = 0f;
            }

            UpdateHoldProgress(continuousTimeInTarget, trial.holdTime, success);

            ExperimentalSessionLogger.Instance?.RegisterFeedbackEvaluation(
                trial.legSide.ToString(),
                measuredAngle,
                measuredAngle - trial.targetAngle,
                insideTarget,
                continuousTimeInTarget);

            elapsed += delta;
            yield return null;
        }

        OnFeedbackPhase = false;
        SetHoldProgressVisible(false);
        StopFeedbackVisualsAndMotion();
        ExperimentalSessionLogger.Instance?.LogPhaseEnd("task", 4, "TASK", $"success={success};best_time_in_target={bestTimeInTarget:0.###}");
        completed?.Invoke(success, bestTimeInTarget);
        Debug.Log("Fim da task com feedback");
    }

    private void EnsureRuntimeFeedbackOverlay()
    {
        if (feedbackOverlay != null)
            return;

        var photoMode = FindAnyObjectByType<PhotoMode>();
        if (photoMode != null)
            feedbackOverlay = photoMode.GetFeedbackOverlay();
    }

    private IEnumerator ResultFeedbackPhase(bool success)
    {
        int markerCode = success ? 6 : 7;
        string markerLabel = success ? "SUCCESS" : "FAILURE";
        string imagePath = success ? feedbackRuntimeSettings.successImagePath : feedbackRuntimeSettings.failureImagePath;
        string text = success ? feedbackRuntimeSettings.successText : feedbackRuntimeSettings.failureText;
        Color textColor = success ? feedbackRuntimeSettings.successTextColor : feedbackRuntimeSettings.failureTextColor;
        float textScreenFraction = success ? feedbackRuntimeSettings.successTextScreenFraction : feedbackRuntimeSettings.failureTextScreenFraction;
        float duration = success ? feedbackRuntimeSettings.successDuration : feedbackRuntimeSettings.failureDuration;

        ShowPhaseStimulus(imagePath, text, textColor, textScreenFraction);
        SendPhaseStart(markerCode, markerLabel, "result", $"duration={duration};success={success}");
        yield return new WaitForSeconds(Mathf.Max(0f, duration));
        ExperimentalSessionLogger.Instance?.LogPhaseEnd("result", markerCode, markerLabel);
    }

    private void UpdateFeedbackLegMotion(float deltaTime)
    {
        float fallStep = Mathf.Max(0f, feedbackRuntimeSettings.gravityFallSpeed) * deltaTime;
        bool usablePrediction = feedbackReceiver != null
            && feedbackReceiver.HasFreshSignal;

        if (!usablePrediction || !TryResolveFeedbackCommand(feedbackReceiver.LatestPrediction.classCode, out GrazAcquisitionTrial command))
        {
            ApplyGravityFall(fallStep);
            ApplyCommandAnglesToPlayer();
            return;
        }

        float probability = Mathf.Clamp01(feedbackReceiver.LatestPrediction.probability);
        float movementStep = Mathf.Max(0f, feedbackRuntimeSettings.maxMovementSpeed) * probability * deltaTime;

        if (command.legSide == GrazLegSide.Right)
        {
            feedbackRightCommandAngle = Mathf.MoveTowards(feedbackRightCommandAngle, command.targetAngle, movementStep);
            feedbackLeftCommandAngle = Mathf.MoveTowards(feedbackLeftCommandAngle, 0f, fallStep);
        }
        else if (command.legSide == GrazLegSide.Left)
        {
            feedbackLeftCommandAngle = Mathf.MoveTowards(feedbackLeftCommandAngle, command.targetAngle, movementStep);
            feedbackRightCommandAngle = Mathf.MoveTowards(feedbackRightCommandAngle, 0f, fallStep);
        }
        else
        {
            ApplyGravityFall(fallStep);
        }

        ApplyCommandAnglesToPlayer();
    }

    private bool TryResolveFeedbackCommand(int classCode, out GrazAcquisitionTrial command)
    {
        if (feedbackClassMap.TryGetValue(classCode, out command))
            return true;

        return TryCreateDefaultFeedbackCommand(classCode, out command);
    }

    private bool TryCreateDefaultFeedbackCommand(int classCode, out GrazAcquisitionTrial command)
    {
        command = null;

        GrazLegSide side;
        float targetAngle;
        switch (classCode)
        {
            case 1000:
                side = GrazLegSide.Right;
                targetAngle = 90f;
                break;
            case 1001:
                side = GrazLegSide.Right;
                targetAngle = 60f;
                break;
            case 1002:
                side = GrazLegSide.Right;
                targetAngle = 45f;
                break;
            case 1003:
                side = GrazLegSide.Right;
                targetAngle = 30f;
                break;
            case 1004:
                side = GrazLegSide.Relaxed;
                targetAngle = 0f;
                break;
            case 1010:
                side = GrazLegSide.Left;
                targetAngle = 90f;
                break;
            case 1011:
                side = GrazLegSide.Left;
                targetAngle = 60f;
                break;
            case 1012:
                side = GrazLegSide.Left;
                targetAngle = 45f;
                break;
            case 1013:
                side = GrazLegSide.Left;
                targetAngle = 30f;
                break;
            default:
                return false;
        }

        command = new GrazAcquisitionTrial
        {
            classId = "lsl_" + classCode,
            label = "LSL " + classCode,
            markerCode = classCode,
            targetAngle = targetAngle,
            tolerance = 0f,
            holdTime = 0f,
            legSide = side
        };
        return true;
    }

    private void ReadCurrentCommandAngles()
    {
        var player = PlayerController.instance;
        float[] inputs = player != null ? player.GetInputValues() : null;
        feedbackLeftCommandAngle = inputs != null && inputs.Length > 0 ? Mathf.Clamp01(Mathf.Abs(inputs[0])) * 90f : 0f;
        feedbackRightCommandAngle = inputs != null && inputs.Length > 1 ? Mathf.Clamp01(Mathf.Abs(inputs[1])) * 90f : 0f;
    }

    private void ApplyGravityFall(float step)
    {
        feedbackLeftCommandAngle = Mathf.MoveTowards(feedbackLeftCommandAngle, 0f, step);
        feedbackRightCommandAngle = Mathf.MoveTowards(feedbackRightCommandAngle, 0f, step);
    }

    private void ApplyCommandAnglesToPlayer()
    {
        var player = PlayerController.instance;
        if (player == null)
            return;

        player.SetInputLeft(Mathf.Clamp01(feedbackLeftCommandAngle / 90f));
        player.SetInputRight(Mathf.Clamp01(feedbackRightCommandAngle / 90f));
    }

    private float GetTrialCommandAngle(GrazLegSide side)
    {
        if (side == GrazLegSide.Left)
            return feedbackLeftCommandAngle;
        if (side == GrazLegSide.Right)
            return feedbackRightCommandAngle;
        return Mathf.Max(feedbackLeftCommandAngle, feedbackRightCommandAngle);
    }

    private void StopFeedbackVisualsAndMotion(bool resetCommands = true)
    {
        feedbackOverlay?.EndRuntimeFeedback();
        if (!resetCommands)
            return;

        feedbackLeftCommandAngle = 0f;
        feedbackRightCommandAngle = 0f;
        ApplyCommandAnglesToPlayer();
    }
    public IEnumerator BaselinePhase()
    {
        Debug.Log("Inicio da baseline");

        if (realtimeLog == null)
            realtimeLog = RealtimeLog.instance;

        if (realtimeLog != null)
        {
            realtimeLog._marcador = "BASE";
            realtimeLog._marcadorNum = 1;
        }
        if (useSubjectStimulusDisplay)
            ShowPhaseStimulus(
                stimulusSettings.baselineImagePath,
                stimulusSettings.baselineText,
                stimulusSettings.baselineTextColor,
                stimulusSettings.baselineTextScreenFraction);

        SendPhaseStart(1, "BASE", "baseline", $"duration={baselineDuration}");

        yield return new WaitForSeconds(baselineDuration);

        ExperimentalSessionLogger.Instance?.LogPhaseEnd("baseline", 1, "BASE");
        Debug.Log("Fim da baseline");
    }

    public IEnumerator FixationPhase()
    {
        Debug.Log("Inicio do fixation");

        if (useSubjectStimulusDisplay)
            ShowFixationStimulus();
        else
            indicationImage.enabled = true;

        if (realtimeLog != null)
        {
            realtimeLog._marcador = "FIX";
            realtimeLog._marcadorNum = 2;
        }
        SendPhaseStart(2, "FIX", "fixation", $"duration={fixationDuration}");

        yield return new WaitForSeconds(fixationDuration);

        ExperimentalSessionLogger.Instance?.LogPhaseEnd("fixation", 2, "FIX");
        Debug.Log("Fim do fixation");
    }

    public IEnumerator CuePhase(int cue)
    {
        Debug.Log("Inicio do cue");

        cueImageDirection.enabled = true;
        indicationImage.enabled = false;

        // Lança bola se for experiencia 1
        if (BallLauncher.instance != null)
            BallLauncher.instance.LaunchSingleBall();

        realtimeLog._marcador = "CUE";
        realtimeLog._marcadorNum = 3;
        SendPhaseStart(3, "CUE", "cue", $"duration={cueDuration}");
        int classMarkerCode = 1000 + cue;
        LSLMarkerOutlet.Instance?.SendClassMarker(classMarkerCode);
        ExperimentalSessionLogger.Instance?.LogClassMarker(classMarkerCode, cue, realtimeLog._target);

        yield return new WaitForSeconds(cueDuration);

        cueImageDirection.enabled = false;

        ExperimentalSessionLogger.Instance?.LogPhaseEnd("cue", 3, "CUE");
        Debug.Log("Fim do cue");
    }
    private IEnumerator CuePhase(GrazAcquisitionTrial trial, int classIndex)
    {
        Debug.Log("Inicio do cue de aquisicao");

        indicationImage.enabled = false;
        Texture cueTexture = LoadCueTexture(trial.cueImagePath) ?? defaultCueImage;

        if (useSubjectStimulusDisplay)
        {
            ShowCueStimulus(cueTexture);
        }
        else
        {
            cueImageDirection.enabled = true;
            cueImageDirection.rectTransform.eulerAngles = Vector3.zero;
            cueImageDirection.texture = cueTexture;
        }

        if (realtimeLog != null)
        {
            realtimeLog._marcador = "CUE";
            realtimeLog._marcadorNum = 3;
        }

        SendPhaseStart(3, "CUE", "cue", $"duration={cueDuration};class={trial.label};image={trial.cueImagePath}");
        LSLMarkerOutlet.Instance?.SendClassMarker(trial.markerCode);
        ExperimentalSessionLogger.Instance?.LogClassMarker(trial.markerCode, classIndex, trial.label);

        yield return new WaitForSeconds(cueDuration);

        if (!useSubjectStimulusDisplay)
            cueImageDirection.enabled = false;
        ExperimentalSessionLogger.Instance?.LogPhaseEnd("cue", 3, "CUE");
        Debug.Log("Fim do cue de aquisicao");
    }

    private IEnumerator AcquisitionTaskPhase(GrazAcquisitionTrial trial)
    {
        Debug.Log("Inicio da task de aquisicao");

        if (realtimeLog != null)
        {
            realtimeLog._marcador = "TASK";
            realtimeLog._marcadorNum = 4;
        }

        if (useSubjectStimulusDisplay)
            ShowPhaseStimulus(
                stimulusSettings.taskImagePath,
                stimulusSettings.taskText,
                stimulusSettings.taskTextColor,
                stimulusSettings.taskTextScreenFraction);

        SendPhaseStart(4, "TASK", "task", $"duration={feedbackDuration};class={trial.label}");
        yield return new WaitForSeconds(feedbackDuration);
        ExperimentalSessionLogger.Instance?.LogPhaseEnd("task", 4, "TASK");

        Debug.Log("Fim da task de aquisicao");
    }
    private float randomDuration;

    public IEnumerator FeedbackPhase()
    {
        Debug.Log("Inicio do feedback");

        OnFeedbackPhase = true;

        realtimeLog._marcador = "FEED";
        realtimeLog._marcadorNum = 4;
        SendPhaseStart(4, "FEED", "feedback", $"duration={feedbackDuration}");

        yield return new WaitForSeconds(feedbackDuration);

        OnFeedbackPhase = false;

        // Processa resultado para experiencia 2
        if (StartGame.instance.name == "Iniciar Game Dois")
        {
            var medidor = UIConfigs.instance.statusUI.medidor.GetComponentInChildren<MedidorMovement>();

            if (medidor.entrouOver)
            {
                medidor.ConfirmOverlapping();
                medidor.entrouOver = false;
                FeedbackController.instance.PlayErrou();
            }
        }

        ExperimentalSessionLogger.Instance?.LogPhaseEnd("feedback", 4, "FEED");
        Debug.Log("Fim do feedback");
    }

    public IEnumerator PausePhase()
    {
        Debug.Log("Inicio do pause");

        if (realtimeLog != null)
        {
            realtimeLog._marcador = "PAUSE";
            realtimeLog._marcadorNum = 5;
        }
        if (useSubjectStimulusDisplay)
            ShowPhaseStimulus(
                stimulusSettings.pauseImagePath,
                stimulusSettings.pauseText,
                stimulusSettings.pauseTextColor,
                stimulusSettings.pauseTextScreenFraction);

        randomDuration = UnityEngine.Random.Range(minPauseDuration, maxPauseDuration);
        SendPhaseStart(5, "PAUSE", "pause", $"duration={randomDuration}");

        yield return new WaitForSeconds(randomDuration);

        ExperimentalSessionLogger.Instance?.LogPhaseEnd("pause", 5, "PAUSE");
        Debug.Log("Fim do pause");
    }

    private void SendPhaseStart(int markerCode, string markerLabel, string phase, string note)
    {
        LSLMarkerOutlet.Instance?.SendPhaseMarker(markerCode, markerLabel);
        ExperimentalSessionLogger.Instance?.LogPhaseStart(phase, markerCode, markerLabel, note);
    }

    private float GetCurrentTargetAngle()
    {
        var uiDois = GameUIDois.Instance;
        if (uiDois != null && uiDois._anguloPerna != null && startGame != null && startGame.actTrials < uiDois._anguloPerna.Length)
            return uiDois._anguloPerna[startGame.actTrials];

        return 0f;
    }

    private float GetCurrentHoldTime()
    {
        var uiDois = GameUIDois.Instance;
        if (uiDois != null && uiDois._temposPrecisao != null && uiDois._temposPrecisao.Count > 0)
            return uiDois._temposPrecisao[Mathf.Min(startGame.actTrials, uiDois._temposPrecisao.Count - 1)];

        return feedbackDuration;
    }

    private Texture2D LoadCueTexture(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return null;

        if (cueTextureCache.TryGetValue(path, out Texture2D cached) && cached != null)
            return cached;

        byte[] bytes = File.ReadAllBytes(path);
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!texture.LoadImage(bytes))
        {
            Destroy(texture);
            return null;
        }

        cueTextureCache[path] = texture;
        return texture;
    }

    private void EnsureSubjectStimulusDisplay()
    {
        if (subjectCanvas != null)
            return;

        var canvasObject = new GameObject("GRAZ Subject Stimulus Display");
        subjectCanvas = canvasObject.AddComponent<Canvas>();
        subjectCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        subjectCanvas.sortingOrder = 9000;

        var scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(StimulusReferenceWidth, StimulusReferenceHeight);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();

        subjectBackground = CreateRawImage("Background", canvasObject.transform);
        Stretch(subjectBackground.rectTransform);

        subjectImage = CreateRawImage("Stimulus Image", canvasObject.transform);
        Center(subjectImage.rectTransform);

        var textObject = new GameObject("Stimulus Text");
        textObject.transform.SetParent(canvasObject.transform, false);
        subjectText = textObject.AddComponent<TextMeshProUGUI>();
        subjectText.alignment = TextAlignmentOptions.Center;
        subjectText.fontSize = 72;
        subjectText.color = Color.black;
        subjectText.enableWordWrapping = true;
        Center(subjectText.rectTransform);
        subjectText.rectTransform.sizeDelta = new Vector2(StimulusReferenceWidth * 0.75f, StimulusReferenceHeight * 0.25f);

        var crossObject = new GameObject("Fixation Cross");
        crossObject.transform.SetParent(canvasObject.transform, false);
        fixationCross = crossObject.AddComponent<TextMeshProUGUI>();
        fixationCross.text = "+";
        fixationCross.alignment = TextAlignmentOptions.Center;
        fixationCross.fontSize = 360;
        fixationCross.fontStyle = FontStyles.Bold;
        Center(fixationCross.rectTransform);
        fixationCross.rectTransform.sizeDelta = new Vector2(520f, 520f);

        var holdObject = new GameObject("Hold Progress Feedback");
        holdObject.transform.SetParent(canvasObject.transform, false);
        holdProgressText = holdObject.AddComponent<TextMeshProUGUI>();
        holdProgressText.alignment = TextAlignmentOptions.Top;
        holdProgressText.fontSize = 46f;
        holdProgressText.fontStyle = FontStyles.Bold;
        holdProgressText.outlineWidth = 0.18f;
        holdProgressText.outlineColor = Color.black;
        holdProgressText.rectTransform.anchorMin = new Vector2(0.5f, 1f);
        holdProgressText.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        holdProgressText.rectTransform.pivot = new Vector2(0.5f, 1f);
        holdProgressText.rectTransform.anchoredPosition = new Vector2(0f, -36f);
        holdProgressText.rectTransform.sizeDelta = new Vector2(1000f, 110f);
        holdProgressText.gameObject.SetActive(false);

        ShowSubjectStimulusDisplay(false);
    }

    private RawImage CreateRawImage(string name, Transform parent)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        return obj.AddComponent<RawImage>();
    }

    private void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private void Center(RectTransform rect)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
    }

    private void ShowSubjectStimulusDisplay(bool visible)
    {
        if (subjectCanvas != null)
            subjectCanvas.gameObject.SetActive(visible);
        if (visible)
            ApplySubjectBackground();
    }

    private void HideSubjectStimulusDisplay()
    {
        ShowSubjectStimulusDisplay(false);
    }

    private void ApplySubjectBackground()
    {
        EnsureSubjectStimulusDisplay();
        subjectBackground.gameObject.SetActive(true);

        Texture2D backgroundTexture = LoadCueTexture(stimulusSettings.backgroundImagePath);
        subjectBackground.texture = backgroundTexture;
        subjectBackground.color = backgroundTexture != null ? Color.white : stimulusSettings.backgroundColor;

        if (backgroundTexture != null)
            FitRectToCover(subjectBackground.rectTransform, backgroundTexture);
        else
            Stretch(subjectBackground.rectTransform);
    }

    private void ShowPhaseStimulus(string imagePath, string text)
    {
        ShowPhaseStimulus(imagePath, text, Color.black, 0.07f);
    }

    private void ShowPhaseStimulus(string imagePath, string text, Color textColor, float textScreenFraction)
    {
        EnsureSubjectStimulusDisplay();
        ApplySubjectBackground();
        fixationCross.gameObject.SetActive(false);
        SetHoldProgressVisible(false);

        Texture2D texture = LoadCueTexture(imagePath);
        subjectImage.gameObject.SetActive(texture != null);
        subjectImage.texture = texture;
        subjectImage.color = Color.white;
        if (texture != null)
            FitRectToContain(subjectImage.rectTransform, texture, stimulusSettings.cueScreenFraction);

        bool hasText = !string.IsNullOrWhiteSpace(text);
        subjectText.gameObject.SetActive(hasText);
        subjectText.text = hasText ? text : "";
        ApplySubjectTextStyle(textColor, textScreenFraction);
    }

    private void ShowFeedbackTaskStimulus()
    {
        EnsureSubjectStimulusDisplay();
        subjectBackground.gameObject.SetActive(false);
        fixationCross.gameObject.SetActive(false);
        SetHoldProgressVisible(false);

        Texture2D texture = LoadCueTexture(stimulusSettings.taskImagePath);
        subjectImage.gameObject.SetActive(texture != null);
        subjectImage.texture = texture;
        subjectImage.color = Color.white;
        if (texture != null)
            FitRectToContain(subjectImage.rectTransform, texture, stimulusSettings.cueScreenFraction);

        bool hasText = !string.IsNullOrWhiteSpace(stimulusSettings.taskText);
        subjectText.gameObject.SetActive(hasText);
        subjectText.text = hasText ? stimulusSettings.taskText : "";
        ApplySubjectTextStyle(stimulusSettings.taskTextColor, stimulusSettings.taskTextScreenFraction);
    }

    private void ShowFixationStimulus()
    {
        Texture2D texture = LoadCueTexture(stimulusSettings.fixationImagePath);
        if (texture != null)
        {
            ShowPhaseStimulus(stimulusSettings.fixationImagePath, "");
            return;
        }

        EnsureSubjectStimulusDisplay();
        ApplySubjectBackground();
        subjectImage.gameObject.SetActive(false);
        subjectText.gameObject.SetActive(false);
        SetHoldProgressVisible(false);
        fixationCross.gameObject.SetActive(true);
        fixationCross.color = stimulusSettings.fixationColor;
        fixationCross.fontSize = StimulusReferenceHeight * stimulusSettings.fixationScreenFraction;
        float crossBox = Mathf.Max(320f, fixationCross.fontSize * 1.45f);
        fixationCross.rectTransform.sizeDelta = new Vector2(crossBox, crossBox);
    }

    private void ShowCueStimulus(Texture texture)
    {
        EnsureSubjectStimulusDisplay();
        ApplySubjectBackground();
        fixationCross.gameObject.SetActive(false);
        subjectText.gameObject.SetActive(false);
        SetHoldProgressVisible(false);

        subjectImage.gameObject.SetActive(texture != null);
        subjectImage.texture = texture;
        subjectImage.color = stimulusSettings.cueColor;

        if (texture != null)
            FitRectToContain(subjectImage.rectTransform, texture, stimulusSettings.cueScreenFraction);
    }

    private void ApplySubjectTextStyle(Color color, float screenFraction)
    {
        subjectText.color = color;
        subjectText.fontSize = StimulusReferenceHeight * Mathf.Clamp(screenFraction, 0.025f, 0.2f);
        subjectText.rectTransform.sizeDelta = new Vector2(StimulusReferenceWidth * 0.86f, StimulusReferenceHeight * 0.42f);
    }

    private void SetHoldProgressVisible(bool visible)
    {
        if (holdProgressText != null)
            holdProgressText.gameObject.SetActive(visible);
    }

    private void UpdateHoldProgress(float current, float required, bool completed)
    {
        if (holdProgressText == null || feedbackRuntimeSettings == null || !feedbackRuntimeSettings.showHoldProgress)
        {
            SetHoldProgressVisible(false);
            return;
        }

        float safeRequired = Mathf.Max(0f, required);
        float displayedCurrent = completed ? safeRequired : Mathf.Clamp(current, 0f, safeRequired);
        holdProgressText.SetText("Tempo no alvo: {0:0.0} / {1:0.0} s", displayedCurrent, safeRequired);
        holdProgressText.color = completed ? new Color(0.12f, 0.9f, 0.24f, 1f) : new Color(1f, 0.12f, 0.08f, 1f);
        SetHoldProgressVisible(true);
    }

    private void FitRectToContain(RectTransform rect, Texture texture, float maxFraction)
    {
        float maxWidth = StimulusReferenceWidth * Mathf.Clamp(maxFraction, 0.25f, 0.9f);
        float maxHeight = StimulusReferenceHeight * Mathf.Clamp(maxFraction, 0.25f, 0.9f);
        float aspect = texture != null && texture.height > 0 ? (float)texture.width / texture.height : 1f;

        float width = maxWidth;
        float height = width / aspect;
        if (height > maxHeight)
        {
            height = maxHeight;
            width = height * aspect;
        }

        Center(rect);
        rect.sizeDelta = new Vector2(width, height);
    }

    private void FitRectToCover(RectTransform rect, Texture texture)
    {
        float aspect = texture != null && texture.height > 0 ? (float)texture.width / texture.height : 1f;
        float screenAspect = StimulusReferenceWidth / StimulusReferenceHeight;

        float width = StimulusReferenceWidth;
        float height = StimulusReferenceHeight;
        if (aspect > screenAspect)
            width = height * aspect;
        else
            height = width / aspect;

        Center(rect);
        rect.sizeDelta = new Vector2(width, height);
    }
}
