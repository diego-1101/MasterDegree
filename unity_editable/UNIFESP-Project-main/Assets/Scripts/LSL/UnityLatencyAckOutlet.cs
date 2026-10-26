using LSL;
using UnityEngine;

public class UnityLatencyAckOutlet : MonoBehaviour
{
    public const int ReceivedStage = 1;
    public const int AppliedStage = 2;
    public const int FramePresentedStage = 3;

    private static UnityLatencyAckOutlet instance;
    private StreamOutlet outlet;
    private readonly double[] sample = new double[9];

    public static UnityLatencyAckOutlet Instance
    {
        get
        {
            if (instance != null)
                return instance;

            instance = FindAnyObjectByType<UnityLatencyAckOutlet>();
            if (instance != null)
                return instance;

            var host = new GameObject("Unity Latency ACK Outlet");
            DontDestroyOnLoad(host);
            instance = host.AddComponent<UnityLatencyAckOutlet>();
            return instance;
        }
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    public void SendReceived(ClassificationPrediction prediction)
    {
        Send(ReceivedStage, prediction, 0f, 0f);
    }

    public void Publish()
    {
        EnsureOutlet();
    }

    public void SendApplied(ClassificationPrediction prediction, float leftAngle, float rightAngle)
    {
        Send(AppliedStage, prediction, leftAngle, rightAngle);
    }

    public void SendFramePresented(ClassificationPrediction prediction, float leftAngle, float rightAngle)
    {
        Send(FramePresentedStage, prediction, leftAngle, rightAngle);
    }

    private void Send(int stage, ClassificationPrediction prediction, float leftAngle, float rightAngle)
    {
        EnsureOutlet();
        if (outlet == null)
            return;

        sample[0] = stage;
        sample[1] = prediction.predictionId;
        sample[2] = prediction.sequence;
        sample[3] = prediction.classCode;
        sample[4] = prediction.probability;
        sample[5] = prediction.trialId;
        sample[6] = prediction.lslTimestamp;
        sample[7] = leftAngle;
        sample[8] = rightAngle;
        outlet.push_sample(sample);
    }

    private void EnsureOutlet()
    {
        if (outlet != null)
            return;

        var info = new StreamInfo(
            "Unity_Feedback_Ack",
            "LatencyAck",
            sample.Length,
            LSL.LSL.IRREGULAR_RATE,
            channel_format_t.cf_double64,
            "unity_feedback_latency_ack_v1");

        var channels = info.desc().append_child("channels");
        AddChannel(channels, "stage_code");
        AddChannel(channels, "prediction_id");
        AddChannel(channels, "unity_sequence");
        AddChannel(channels, "class_code");
        AddChannel(channels, "probability");
        AddChannel(channels, "trial_id");
        AddChannel(channels, "classifier_lsl_timestamp");
        AddChannel(channels, "left_command_deg");
        AddChannel(channels, "right_command_deg");
        info.desc().append_child_value("stage_1", "classification_received");
        info.desc().append_child_value("stage_2", "leg_command_applied");
        info.desc().append_child_value("stage_3", "end_of_frame_reached");
        outlet = new StreamOutlet(info);
    }

    private static void AddChannel(XMLElement channels, string label)
    {
        channels.append_child("channel").append_child_value("label", label);
    }
}
