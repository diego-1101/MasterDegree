using LSL4Unity.Utils;
using UnityEngine;

namespace LSL4Unity.Samples.Complex;

public class PoseInlet : AFloatInlet
{
	public Vector3 Offset = new Vector3(1f, 1f, 0f);

	private PoseFormat _transformFormat = PoseFormat.PosQuat7D;

	public PoseFormat TransformFormat => _transformFormat;

	private void Reset()
	{
		StreamName = "Unity.Pose";
	}

	protected override void OnStreamAvailable()
	{
		if (ChannelCount == 7)
		{
			_transformFormat = PoseFormat.PosQuat7D;
		}
		else if (ChannelCount == 6)
		{
			_transformFormat = PoseFormat.PosEul6D;
		}
		else if (ChannelCount == 12)
		{
			_transformFormat = PoseFormat.Transform12D;
		}
	}

	protected override void Process(float[] newSample, double timestamp)
	{
		if (TransformFormat == PoseFormat.Transform12D)
		{
			base.gameObject.transform.position = new Vector3(newSample[3], newSample[7], newSample[11]);
		}
		else
		{
			base.gameObject.transform.position = new Vector3(newSample[0], newSample[1], newSample[2]);
			if (TransformFormat == PoseFormat.PosQuat7D)
			{
				base.gameObject.transform.rotation = new Quaternion(newSample[3], newSample[4], newSample[5], newSample[6]);
			}
		}
		base.gameObject.transform.position += Offset;
	}
}
