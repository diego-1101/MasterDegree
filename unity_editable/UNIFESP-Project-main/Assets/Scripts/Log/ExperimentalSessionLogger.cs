using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

[Serializable]
public class ExperimentalLogSessionConfig
{
    public string experimentName = "Experiment 2";
    public string mode = "graz_feedback";
    public string subjectId = "S01";
    public string sessionId = "01";
    public string run = "1";
    public string presetName = "";
    public string logPath = "";
    public string inputStreamName = "";
    public string inputStreamType = "";
    public string markerStreamName = "";
    public string markerStreamType = "";
    public int totalTrials;
    public float baselineDuration;
    public float fixationDuration;
    public float cueDuration;
    public float feedbackDuration;
    public float minPauseDuration;
    public float maxPauseDuration;
    public float gravity;
    public float defaultTolerance;
    public float defaultHoldTime;
    public string classificationPayload = "channel_0=class_code;channel_1=probability";
    public string movementControlMode = "discrete_progressive_probability_speed";
    public float maxMovementSpeed;
    public float inputStaleSeconds;
    public bool showFeedbackGauge;
    public bool showFeedbackSideView;
    public bool showGhostTarget;
    public bool showResultFeedback;
    public float successFeedbackDuration;
    public float failureFeedbackDuration;
    public bool shuffleTrials;
    public string randomSeed = "";
}

public class ExperimentalSessionLogger : MonoBehaviour
{
    private static ExperimentalSessionLogger instance;

    public static ExperimentalSessionLogger Instance
    {
        get
        {
            if (instance == null)
                instance = FindAnyObjectByType<ExperimentalSessionLogger>();
            return instance;
        }
    }

    [SerializeField] private float sampleInterval = 0.1f;
    [SerializeField] private bool logRealtimeSamples = true;

    private ExperimentalLogSessionConfig config;
    private StreamWriter eventWriter;
    private StreamWriter sampleWriter;
    private string sessionDirectory;
    private string eventPath;
    private string samplePath;
    private string metadataPath;
    private float sessionStartRealtime;
    private float nextSampleTime;
    private bool active;

    private string currentPhase = "idle";
    private int currentMarkerCode;
    private string currentMarkerLabel = "";
    private int currentTrialIndex = -1;
    private int currentClassId = -1;
    private string currentClassLabel = "";
    private float currentTargetAngle;
    private float currentTolerance;
    private float currentHoldTime;
    private bool lastSuccess;
    private string lastResult = "";
    private string currentTargetSide = "";
    private float currentFeedbackAngle;
    private float currentSignedError;
    private bool currentWithinTarget;
    private float currentAccumulatedHoldTime;

    private double lastLslTimestamp;
    private float[] lastClassifierSample = new float[0];

    public bool IsActive => active;
    public string SessionDirectory => sessionDirectory;
    public string EventPath => eventPath;
    public string SamplePath => samplePath;
    public string MetadataPath => metadataPath;

    private void Awake()
    {
        if (instance == null)
            instance = this;
    }

    private void Update()
    {
        if (!active || !logRealtimeSamples)
            return;

        if (Time.realtimeSinceStartup < nextSampleTime)
            return;

        WriteSample("periodic");
        nextSampleTime = Time.realtimeSinceStartup + sampleInterval;
    }

    public void Configure(ExperimentalLogSessionConfig newConfig)
    {
        config = newConfig ?? new ExperimentalLogSessionConfig();
    }

    public void BeginSession(string reason = "session_start")
    {
        if (config == null)
            config = new ExperimentalLogSessionConfig();

        EndSession("restart_before_new_session");

        sessionDirectory = ResolveSessionDirectory(config);
        Directory.CreateDirectory(sessionDirectory);

        string prefix = MakeFileSafe($"{config.experimentName}_{config.mode}_{config.subjectId}_S{config.sessionId}_R{config.run}_{DateTime.Now:yyyyMMdd_HHmmss}");
        eventPath = Path.Combine(sessionDirectory, prefix + "_events.csv");
        samplePath = Path.Combine(sessionDirectory, prefix + "_samples.csv");
        metadataPath = Path.Combine(sessionDirectory, prefix + "_metadata.json");

        eventWriter = new StreamWriter(eventPath, false);
        sampleWriter = new StreamWriter(samplePath, false);

        eventWriter.WriteLine("event_index,wall_time,unity_realtime,session_elapsed,event_type,phase,trial_index,class_id,class_label,marker_code,marker_label,target_angle,tolerance,hold_time,success,result,note");
        sampleWriter.WriteLine("sample_index,wall_time,unity_realtime,session_elapsed,source,phase,trial_index,class_id,class_label,marker_code,marker_label,left_input,right_input,left_angle,right_angle,target_angle,tolerance,hold_time,target_side,feedback_angle,signed_error,absolute_error,within_target,accumulated_hold_time,last_success,last_result,lsl_timestamp,lsl_ch0,lsl_ch1,lsl_ch2,lsl_payload");

        File.WriteAllText(metadataPath, JsonUtility.ToJson(config, true));

        sessionStartRealtime = Time.realtimeSinceStartup;
        nextSampleTime = sessionStartRealtime;
        eventIndex = 0;
        sampleIndex = 0;
        active = true;

        LogEvent("session_start", currentPhase, 0, "", -1, -1, "", 0f, 0f, 0f, false, "", reason);
    }

    public void EndSession(string reason = "session_end")
    {
        if (!active && eventWriter == null && sampleWriter == null)
            return;

        if (active)
        {
            WriteSample("session_end");
            LogEvent("session_end", currentPhase, currentMarkerCode, currentMarkerLabel, currentTrialIndex, currentClassId, currentClassLabel, currentTargetAngle, currentTolerance, currentHoldTime, lastSuccess, lastResult, reason);
        }

        active = false;
        FlushAndClose(eventWriter);
        FlushAndClose(sampleWriter);
        eventWriter = null;
        sampleWriter = null;
    }

    public void RegisterClassifierSample(float[] sample, double timestamp)
    {
        if (sample == null)
            return;

        lastClassifierSample = new float[sample.Length];
        Array.Copy(sample, lastClassifierSample, sample.Length);
        lastLslTimestamp = timestamp;

        if (active)
            WriteSample("lsl");
    }

    public void LogTrialStart(int trialIndex, int classId, string classLabel, int markerCode, float targetAngle, float tolerance, float holdTime)
    {
        currentTrialIndex = trialIndex;
        currentClassId = classId;
        currentClassLabel = classLabel ?? "";
        currentTargetAngle = targetAngle;
        currentTolerance = tolerance;
        currentHoldTime = holdTime;
        lastSuccess = false;
        lastResult = "";
        currentTargetSide = "";
        currentFeedbackAngle = 0f;
        currentSignedError = 0f;
        currentWithinTarget = false;
        currentAccumulatedHoldTime = 0f;
        LogEvent("trial_start", currentPhase, markerCode, markerCode.ToString(), trialIndex, classId, currentClassLabel, targetAngle, tolerance, holdTime, false, "", "");
    }

    public void LogTrialEnd(string result, bool success)
    {
        lastSuccess = success;
        lastResult = result ?? "";
        LogEvent("trial_end", currentPhase, currentMarkerCode, currentMarkerLabel, currentTrialIndex, currentClassId, currentClassLabel, currentTargetAngle, currentTolerance, currentHoldTime, success, lastResult, "");
    }

    public void LogPhaseStart(string phase, int markerCode, string markerLabel, string note = "")
    {
        currentPhase = string.IsNullOrWhiteSpace(phase) ? "unknown" : phase;
        currentMarkerCode = markerCode;
        currentMarkerLabel = string.IsNullOrWhiteSpace(markerLabel) ? markerCode.ToString() : markerLabel;
        LogEvent("phase_start", currentPhase, markerCode, currentMarkerLabel, currentTrialIndex, currentClassId, currentClassLabel, currentTargetAngle, currentTolerance, currentHoldTime, lastSuccess, lastResult, note);
    }

    public void LogPhaseEnd(string phase, int markerCode, string markerLabel, string note = "")
    {
        LogEvent("phase_end", string.IsNullOrWhiteSpace(phase) ? currentPhase : phase, markerCode, markerLabel, currentTrialIndex, currentClassId, currentClassLabel, currentTargetAngle, currentTolerance, currentHoldTime, lastSuccess, lastResult, note);
    }

    public void LogClassMarker(int markerCode, int classId, string classLabel)
    {
        currentMarkerCode = markerCode;
        currentMarkerLabel = markerCode.ToString();
        currentClassId = classId;
        currentClassLabel = classLabel ?? "";
        LogEvent("class_marker", currentPhase, markerCode, currentMarkerLabel, currentTrialIndex, classId, currentClassLabel, currentTargetAngle, currentTolerance, currentHoldTime, false, "", "");
    }

    public void LogResult(bool success, string result, string note = "")
    {
        lastSuccess = success;
        lastResult = result ?? "";
        LogEvent("result", currentPhase, currentMarkerCode, currentMarkerLabel, currentTrialIndex, currentClassId, currentClassLabel, currentTargetAngle, currentTolerance, currentHoldTime, success, lastResult, note);
    }

    public void RegisterFeedbackEvaluation(string targetSide, float feedbackAngle, float signedError, bool withinTarget, float accumulatedHoldTime)
    {
        currentTargetSide = targetSide ?? "";
        currentFeedbackAngle = feedbackAngle;
        currentSignedError = signedError;
        currentWithinTarget = withinTarget;
        currentAccumulatedHoldTime = accumulatedHoldTime;
    }

    private int eventIndex;
    private int sampleIndex;

    private void LogEvent(string eventType, string phase, int markerCode, string markerLabel, int trialIndex, int classId, string classLabel, float targetAngle, float tolerance, float holdTime, bool success, string result, string note)
    {
        if (!active || eventWriter == null)
            return;

        eventIndex++;
        eventWriter.WriteLine(string.Join(",",
            eventIndex,
            Csv(DateTime.Now.ToString("O", CultureInfo.InvariantCulture)),
            F(Time.realtimeSinceStartup),
            F(Time.realtimeSinceStartup - sessionStartRealtime),
            Csv(eventType),
            Csv(phase),
            trialIndex,
            classId,
            Csv(classLabel),
            markerCode,
            Csv(markerLabel),
            F(targetAngle),
            F(tolerance),
            F(holdTime),
            success ? "1" : "0",
            Csv(result),
            Csv(note)));
        eventWriter.Flush();
    }

    private void WriteSample(string source)
    {
        if (!active || sampleWriter == null)
            return;

        sampleIndex++;

        float leftInput = 0f;
        float rightInput = 0f;
        float leftAngle = 0f;
        float rightAngle = 0f;

        var player = PlayerController.instance;
        if (player != null)
        {
            float[] inputs = player.GetInputValues();
            if (inputs != null && inputs.Length >= 2)
            {
                leftInput = inputs[0];
                rightInput = inputs[1];
            }

            Vector2 angles = player.GetBothLegsAngles();
            leftAngle = angles.x;
            rightAngle = angles.y;
        }

        sampleWriter.WriteLine(string.Join(",",
            sampleIndex,
            Csv(DateTime.Now.ToString("O", CultureInfo.InvariantCulture)),
            F(Time.realtimeSinceStartup),
            F(Time.realtimeSinceStartup - sessionStartRealtime),
            Csv(source),
            Csv(currentPhase),
            currentTrialIndex,
            currentClassId,
            Csv(currentClassLabel),
            currentMarkerCode,
            Csv(currentMarkerLabel),
            F(leftInput),
            F(rightInput),
            F(leftAngle),
            F(rightAngle),
            F(currentTargetAngle),
            F(currentTolerance),
            F(currentHoldTime),
            Csv(currentTargetSide),
            F(currentFeedbackAngle),
            F(currentSignedError),
            F(Mathf.Abs(currentSignedError)),
            currentWithinTarget ? "1" : "0",
            F(currentAccumulatedHoldTime),
            lastSuccess ? "1" : "0",
            Csv(lastResult),
            F(lastLslTimestamp),
            F(GetClassifierValue(0)),
            F(GetClassifierValue(1)),
            F(GetClassifierValue(2)),
            Csv(BuildClassifierPayload())));
        sampleWriter.Flush();
    }

    private float GetClassifierValue(int index)
    {
        return lastClassifierSample != null && lastClassifierSample.Length > index ? lastClassifierSample[index] : 0f;
    }

    private string BuildClassifierPayload()
    {
        if (lastClassifierSample == null || lastClassifierSample.Length == 0)
            return "";

        var values = new List<string>(lastClassifierSample.Length);
        for (int i = 0; i < lastClassifierSample.Length; i++)
            values.Add(F(lastClassifierSample[i]));
        return string.Join(";", values);
    }

    private string ResolveSessionDirectory(ExperimentalLogSessionConfig sessionConfig)
    {
        if (!string.IsNullOrWhiteSpace(sessionConfig.logPath))
        {
            string directory = Path.HasExtension(sessionConfig.logPath)
                ? Path.GetDirectoryName(sessionConfig.logPath)
                : sessionConfig.logPath;

            if (!string.IsNullOrWhiteSpace(directory))
                return directory;
        }

        return Path.Combine(Application.persistentDataPath, "ExperimentalLogs", MakeFileSafe(sessionConfig.subjectId));
    }

    private void FlushAndClose(StreamWriter writer)
    {
        if (writer == null)
            return;

        writer.Flush();
        writer.Close();
        writer.Dispose();
    }

    private string F(float value)
    {
        return value.ToString("0.######", CultureInfo.InvariantCulture);
    }

    private string F(double value)
    {
        return value.ToString("0.######", CultureInfo.InvariantCulture);
    }

    private string Csv(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        if (value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r"))
            return "\"" + value.Replace("\"", "\"\"") + "\"";

        return value;
    }

    private string MakeFileSafe(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "session";

        foreach (char invalid in Path.GetInvalidFileNameChars())
            value = value.Replace(invalid, '_');

        return value.Replace(" ", "_");
    }
}
