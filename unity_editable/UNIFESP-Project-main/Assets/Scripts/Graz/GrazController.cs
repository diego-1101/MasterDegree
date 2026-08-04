using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

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

    private RealtimeLog realtimeLog; // Sistema de log
    private StartGame startGame; // Controlador do jogo
    private int globalTrialIndex;
    private readonly Dictionary<string, Texture2D> cueTextureCache = new Dictionary<string, Texture2D>();

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

    public void StartAcquisitionProtocol(List<GrazAcquisitionTrial> trials, Action onCompleted = null)
    {
        if (acquisitionCoroutine != null)
            StopCoroutine(acquisitionCoroutine);

        acquisitionCoroutine = StartCoroutine(GrazAcquisitionProtocol(trials, onCompleted));
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
        ToggleCanvas(false);
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

        ToggleCanvas(true);
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
        ToggleCanvas(false);
        ExperimentalSessionLogger.Instance?.EndSession("graz_acquisition_completed");
        acquisitionCoroutine = null;

        Debug.Log("Protocolo GRAZ de aquisicao concluido");
        onCompleted?.Invoke();
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
        SendPhaseStart(1, "BASE", "baseline", $"duration={baselineDuration}");

        yield return new WaitForSeconds(baselineDuration);

        ExperimentalSessionLogger.Instance?.LogPhaseEnd("baseline", 1, "BASE");
        Debug.Log("Fim da baseline");
    }

    public IEnumerator FixationPhase()
    {
        Debug.Log("Inicio do fixation");

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
        cueImageDirection.enabled = true;
        cueImageDirection.rectTransform.eulerAngles = Vector3.zero;
        cueImageDirection.texture = LoadCueTexture(trial.cueImagePath) ?? defaultCueImage;

        if (realtimeLog != null)
        {
            realtimeLog._marcador = "CUE";
            realtimeLog._marcadorNum = 3;
        }

        SendPhaseStart(3, "CUE", "cue", $"duration={cueDuration};class={trial.label};image={trial.cueImagePath}");
        LSLMarkerOutlet.Instance?.SendClassMarker(trial.markerCode);
        ExperimentalSessionLogger.Instance?.LogClassMarker(trial.markerCode, classIndex, trial.label);

        yield return new WaitForSeconds(cueDuration);

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
}
