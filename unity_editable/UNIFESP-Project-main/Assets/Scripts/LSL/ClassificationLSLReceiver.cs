using System;
using System.Collections;
using LSL;
using UnityEngine;

[Serializable]
public struct ClassificationPrediction
{
    public int classCode;
    public float probability;
    public long predictionId;
    public int trialId;
    public double lslTimestamp;
    public float unityReceivedTime;
    public long sequence;
}

public class ClassificationLSLReceiver : MonoBehaviour
{
    [SerializeField] private string streamName = "Simulated_Leg_Classifier";
    [SerializeField] private string streamType = "Classifications";
    [SerializeField] private float resolveTimeoutSeconds = 5f;
    [SerializeField] private float staleAfterSeconds = 1.25f;

    private StreamInlet inlet;
    private float[] sample;
    private Coroutine resolveCoroutine;
    private string connectionStatus = "Stream de entrada desconectada.";
    private ClassificationPrediction latestPrediction;
    private bool hasPrediction;
    private long receivedSamplesCount;

    public event Action<ClassificationPrediction> PredictionReceived;

    public string StreamName => streamName;
    public string StreamType => streamType;
    public string ConnectionStatus => connectionStatus;
    public bool IsConnected => inlet != null;
    public bool IsResolving => resolveCoroutine != null;
    public bool HasPrediction => hasPrediction;
    public ClassificationPrediction LatestPrediction => latestPrediction;
    public long ReceivedSamplesCount => receivedSamplesCount;
    public float SignalAge => hasPrediction ? Mathf.Max(0f, Time.realtimeSinceStartup - latestPrediction.unityReceivedTime) : float.PositiveInfinity;
    public bool HasFreshSignal => IsConnected && hasPrediction && SignalAge <= staleAfterSeconds;

    private void Awake()
    {
        UnityLatencyAckOutlet.Instance?.Publish();
    }

    public void Configure(string newStreamName, string newStreamType, float newStaleAfterSeconds)
    {
        string normalizedName = string.IsNullOrWhiteSpace(newStreamName) ? "Simulated_Leg_Classifier" : newStreamName.Trim();
        string normalizedType = string.IsNullOrWhiteSpace(newStreamType) ? "Classifications" : newStreamType.Trim();
        float normalizedStale = Mathf.Max(0.05f, newStaleAfterSeconds);

        bool streamChanged = streamName != normalizedName || streamType != normalizedType;
        streamName = normalizedName;
        streamType = normalizedType;
        staleAfterSeconds = normalizedStale;

        if (streamChanged)
            Disconnect();
    }

    public void Connect()
    {
        if (IsConnected || resolveCoroutine != null)
            return;

        resolveCoroutine = StartCoroutine(ResolveAndOpen());
    }

    public void Reconnect()
    {
        Disconnect();
        Connect();
    }

    public void Disconnect()
    {
        if (resolveCoroutine != null)
        {
            StopCoroutine(resolveCoroutine);
            resolveCoroutine = null;
        }

        if (inlet != null)
        {
            inlet.close_stream();
            inlet = null;
        }

        sample = null;
        hasPrediction = false;
        connectionStatus = "Stream de entrada desconectada.";
    }

    private IEnumerator ResolveAndOpen()
    {
        connectionStatus = $"Procurando {streamName} / {streamType}...";
        float startedAt = Time.realtimeSinceStartup;

        while (Time.realtimeSinceStartup - startedAt < resolveTimeoutSeconds)
        {
            StreamInfo[] candidates = LSL.LSL.resolve_stream("name", streamName, 1, 0.1);
            StreamInfo selected = null;

            if (candidates != null)
            {
                for (int i = 0; i < candidates.Length; i++)
                {
                    if (candidates[i] != null && string.Equals(candidates[i].type(), streamType, StringComparison.OrdinalIgnoreCase))
                    {
                        selected = candidates[i];
                        break;
                    }
                }
            }

            if (selected != null)
            {
                int channelCount = selected.channel_count();
                if (channelCount < 2)
                {
                    connectionStatus = $"Stream encontrada, mas possui {channelCount} canal(is). Esperado: classe + probabilidade.";
                    resolveCoroutine = null;
                    yield break;
                }

                inlet = new StreamInlet(selected);
                sample = new float[channelCount];
                receivedSamplesCount = 0;
                hasPrediction = false;
                connectionStatus = $"Stream de entrada conectada: {streamName} / {streamType} ({channelCount} canais).";
                resolveCoroutine = null;
                yield break;
            }

            yield return new WaitForSecondsRealtime(0.25f);
        }

        connectionStatus = $"Stream de entrada nao encontrada: {streamName} / {streamType}.";
        resolveCoroutine = null;
    }

    private void Update()
    {
        if (inlet == null || sample == null)
            return;

        int drained = 0;
        while (drained < 256)
        {
            double timestamp = inlet.pull_sample(sample, 0f);
            if (timestamp == 0.0)
                break;

            receivedSamplesCount++;
            latestPrediction = new ClassificationPrediction
            {
                classCode = Mathf.RoundToInt(sample[0]),
                probability = Mathf.Clamp01(sample[1]),
                predictionId = sample.Length > 2 ? (long)Mathf.Round(sample[2]) : receivedSamplesCount,
                trialId = sample.Length > 3 ? Mathf.RoundToInt(sample[3]) : 0,
                lslTimestamp = timestamp,
                unityReceivedTime = Time.realtimeSinceStartup,
                sequence = receivedSamplesCount
            };
            hasPrediction = true;

            ExperimentalSessionLogger.Instance?.RegisterClassifierSample(sample, timestamp);
            UnityLatencyAckOutlet.Instance?.SendReceived(latestPrediction);
            PredictionReceived?.Invoke(latestPrediction);
            drained++;
        }

        if (hasPrediction)
        {
            string freshness = HasFreshSignal ? "sinal recente" : $"sinal sem atualizacao ha {SignalAge:0.00}s";
            connectionStatus = $"Stream de entrada conectada: classe {latestPrediction.classCode}, prob. {latestPrediction.probability:0.000} ({freshness}).";
        }
    }

    private void OnDestroy()
    {
        Disconnect();
    }
}

[Serializable]
public class FreeLslCommandDefinition
{
    public bool enabled = true;
    public string code = "1000";
    public GrazLegSide legSide = GrazLegSide.Right;
    public string targetAngle = "90";

    public FreeLslCommandDefinition()
    {
    }

    public FreeLslCommandDefinition(int classCode, GrazLegSide side, float angle)
    {
        code = classCode.ToString();
        legSide = side;
        targetAngle = angle.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
    }
}

public struct FreeLslRuntimeSettings
{
    public float gravityFallSpeed;
    public float maxMovementSpeed;
    public bool showLeft;
    public bool showRight;
    public bool showGauge;
    public bool showSideView;
    public bool showGhostTarget;
    public bool showToleranceBand;
    public float leftTargetAngle;
    public float leftTolerance;
    public float rightTargetAngle;
    public float rightTolerance;
}

public class FreeLslFamiliarizationController : MonoBehaviour
{
    public static bool IsAnyRunning { get; private set; }

    private struct ResolvedCommand
    {
        public GrazLegSide side;
        public float targetAngle;
    }

    private readonly System.Collections.Generic.Dictionary<int, ResolvedCommand> commands = new System.Collections.Generic.Dictionary<int, ResolvedCommand>();

    private ClassificationLSLReceiver receiver;
    private PhotoModeFeedbackOverlay overlay;
    private FreeLslRuntimeSettings settings;
    private Action stopped;
    private float leftCommandAngle;
    private float rightCommandAngle;
    private bool running;

    public bool IsRunning => running;

    public void StartMode(
        ClassificationLSLReceiver classificationReceiver,
        PhotoModeFeedbackOverlay feedbackOverlay,
        System.Collections.Generic.IEnumerable<FreeLslCommandDefinition> commandDefinitions,
        FreeLslRuntimeSettings runtimeSettings,
        Action onStopped)
    {
        StopMode(false, false);
        receiver = classificationReceiver;
        overlay = feedbackOverlay;
        settings = runtimeSettings;
        stopped = onStopped;
        BuildCommandMap(commandDefinitions);
        ReadCurrentAngles();

        var player = PlayerController.instance;
        if (player != null)
        {
            if (player.mainCam != null)
                player.mainCam.gameObject.SetActive(true);
            if (player.lateralCam != null)
                player.lateralCam.gameObject.SetActive(false);
        }

        overlay?.BeginContinuousRuntimeFeedback(
            settings.showLeft,
            settings.showRight,
            settings.showGauge,
            settings.showSideView,
            settings.showGhostTarget,
            settings.showToleranceBand);
        overlay?.SetContinuousRuntimeTargets(
            settings.leftTargetAngle,
            settings.leftTolerance,
            settings.rightTargetAngle,
            settings.rightTolerance);
        overlay?.SetRuntimeAngles(leftCommandAngle, rightCommandAngle);

        running = true;
        IsAnyRunning = true;
        enabled = true;
    }

    public void StopMode()
    {
        StopMode(true, true);
    }

    private void Update()
    {
        if (!running)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            StopMode();
            return;
        }

        UpdateLegMotion(Time.deltaTime);
        overlay?.SetRuntimeAngles(leftCommandAngle, rightCommandAngle);
    }

    private void UpdateLegMotion(float deltaTime)
    {
        float fallStep = Mathf.Max(0f, settings.gravityFallSpeed) * deltaTime;
        ResolvedCommand command = default;
        bool hasCommand = receiver != null
            && receiver.HasFreshSignal
            && TryResolveCommand(receiver.LatestPrediction.classCode, out command);

        if (!hasCommand)
        {
            ApplyGravityFall(fallStep);
            ApplyAnglesToPlayer();
            return;
        }

        float probability = Mathf.Clamp01(receiver.LatestPrediction.probability);
        float movementStep = Mathf.Max(0f, settings.maxMovementSpeed) * probability * deltaTime;

        if (command.side == GrazLegSide.Right)
        {
            rightCommandAngle = Mathf.MoveTowards(rightCommandAngle, command.targetAngle, movementStep);
            leftCommandAngle = Mathf.MoveTowards(leftCommandAngle, 0f, fallStep);
        }
        else if (command.side == GrazLegSide.Left)
        {
            leftCommandAngle = Mathf.MoveTowards(leftCommandAngle, command.targetAngle, movementStep);
            rightCommandAngle = Mathf.MoveTowards(rightCommandAngle, 0f, fallStep);
        }
        else
        {
            ApplyGravityFall(fallStep);
        }

        ApplyAnglesToPlayer();
    }

    private void BuildCommandMap(System.Collections.Generic.IEnumerable<FreeLslCommandDefinition> definitions)
    {
        commands.Clear();
        if (definitions == null)
            return;

        foreach (FreeLslCommandDefinition definition in definitions)
        {
            if (definition == null || !definition.enabled)
                continue;
            if (!int.TryParse(definition.code, out int code))
                continue;
            if (!TryParseFloat(definition.targetAngle, out float targetAngle))
                continue;

            commands[code] = new ResolvedCommand
            {
                side = definition.legSide,
                targetAngle = Mathf.Clamp(targetAngle, 0f, 90f)
            };
        }
    }

    private bool TryResolveCommand(int classCode, out ResolvedCommand command)
    {
        if (commands.TryGetValue(classCode, out command))
            return true;

        return TryCreateDefaultCommand(classCode, out command);
    }

    private static bool TryCreateDefaultCommand(int classCode, out ResolvedCommand command)
    {
        command = default;
        switch (classCode)
        {
            case 1000: command = Command(GrazLegSide.Right, 90f); return true;
            case 1001: command = Command(GrazLegSide.Right, 60f); return true;
            case 1002: command = Command(GrazLegSide.Right, 45f); return true;
            case 1003: command = Command(GrazLegSide.Right, 30f); return true;
            case 1004: command = Command(GrazLegSide.Relaxed, 0f); return true;
            case 1010: command = Command(GrazLegSide.Left, 90f); return true;
            case 1011: command = Command(GrazLegSide.Left, 60f); return true;
            case 1012: command = Command(GrazLegSide.Left, 45f); return true;
            case 1013: command = Command(GrazLegSide.Left, 30f); return true;
            default: return false;
        }
    }

    private static ResolvedCommand Command(GrazLegSide side, float angle)
    {
        return new ResolvedCommand { side = side, targetAngle = angle };
    }

    private static bool TryParseFloat(string value, out float parsed)
    {
        return float.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out parsed)
            || float.TryParse(value, out parsed);
    }

    private void ReadCurrentAngles()
    {
        var player = PlayerController.instance;
        float[] inputs = player != null ? player.GetInputValues() : null;
        leftCommandAngle = inputs != null && inputs.Length > 0 ? Mathf.Clamp01(Mathf.Abs(inputs[0])) * 90f : 0f;
        rightCommandAngle = inputs != null && inputs.Length > 1 ? Mathf.Clamp01(Mathf.Abs(inputs[1])) * 90f : 0f;
    }

    private void ApplyGravityFall(float step)
    {
        leftCommandAngle = Mathf.MoveTowards(leftCommandAngle, 0f, step);
        rightCommandAngle = Mathf.MoveTowards(rightCommandAngle, 0f, step);
    }

    private void ApplyAnglesToPlayer()
    {
        var player = PlayerController.instance;
        if (player == null)
            return;

        player.SetInputLeft(Mathf.Clamp01(leftCommandAngle / 90f));
        player.SetInputRight(Mathf.Clamp01(rightCommandAngle / 90f));
    }

    private void StopMode(bool notify, bool resetLegs)
    {
        bool wasRunning = running;
        running = false;
        IsAnyRunning = false;
        overlay?.EndRuntimeFeedback();

        if (resetLegs)
        {
            leftCommandAngle = 0f;
            rightCommandAngle = 0f;
            ApplyAnglesToPlayer();
        }

        Action callback = stopped;
        receiver = null;
        overlay = null;
        stopped = null;

        if (notify && wasRunning)
            callback?.Invoke();
    }

    private void OnDestroy()
    {
        StopMode(false, false);
    }
}
