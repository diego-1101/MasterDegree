using LSL;
using UnityEngine;

namespace LSL4Unity.Samples.SimplePhysicsEvent;

public class SimpleOutletTriggerEvent : MonoBehaviour
{
	private string StreamName = "LSL4Unity.Samples.SimpleCollisionEvent";

	private string StreamType = "Markers";

	private StreamOutlet outlet;

	private string[] sample = new string[1] { "" };

	private void Start()
	{
		Hash128 hash = default(Hash128);
		hash.Append(StreamName);
		hash.Append(StreamType);
		hash.Append(base.gameObject.GetInstanceID());
		StreamInfo info = new StreamInfo(StreamName, StreamType, 1, 0.0, channel_format_t.cf_string, hash.ToString());
		outlet = new StreamOutlet(info);
	}

	private void OnTriggerEnter(Collider other)
	{
		if (outlet != null)
		{
			sample[0] = "TriggerEnter " + base.gameObject.GetInstanceID();
			outlet.push_sample(sample);
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (outlet != null)
		{
			sample[0] = "TriggerExit " + base.gameObject.GetInstanceID();
			outlet.push_sample(sample);
		}
	}
}
