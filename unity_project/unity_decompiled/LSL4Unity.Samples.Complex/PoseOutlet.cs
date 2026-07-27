using System.Collections.Generic;
using LSL4Unity.Utils;
using UnityEngine;

namespace LSL4Unity.Samples.Complex;

public class PoseOutlet : AFloatOutlet
{
	public PoseFormat transformFormat = PoseFormat.PosQuat7D;

	public override List<string> ChannelNames
	{
		get
		{
			List<string> list = new List<string>();
			if (transformFormat == PoseFormat.PosEul6D || transformFormat == PoseFormat.PosQuat7D)
			{
				list.AddRange(new string[3] { "PosX", "PosY", "PosZ" });
				if (transformFormat == PoseFormat.PosEul6D)
				{
					list.AddRange(new string[3] { "Pitch", "Yaw", "Roll" });
				}
				else
				{
					list.AddRange(new string[4] { "RotX", "RotY", "RotZ", "RotW" });
				}
			}
			else if (transformFormat == PoseFormat.Transform12D)
			{
				_ = base.gameObject.transform.localToWorldMatrix;
				for (int i = 0; i < 3; i++)
				{
					for (int j = 0; j < 4; j++)
					{
						list.Add($"{i},{j}");
					}
				}
			}
			return list;
		}
	}

	public void Reset()
	{
		StreamName = "Unity.Pose";
		StreamType = "Unity.Transform";
		moment = MomentForSampling.FixedUpdate;
	}

	protected override void ExtendHash(Hash128 hash)
	{
		hash.Append(transformFormat.ToString());
	}

	protected override bool BuildSample()
	{
		if (transformFormat == PoseFormat.PosEul6D || transformFormat == PoseFormat.PosQuat7D)
		{
			Vector3 position = base.gameObject.transform.position;
			sample[0] = position.x;
			sample[1] = position.y;
			sample[2] = position.z;
			if (transformFormat == PoseFormat.PosEul6D)
			{
				Vector3 eulerAngles = base.gameObject.transform.eulerAngles;
				sample[3] = eulerAngles.x;
				sample[4] = eulerAngles.y;
				sample[5] = eulerAngles.z;
			}
			else
			{
				Quaternion rotation = base.gameObject.transform.rotation;
				sample[3] = rotation.x;
				sample[4] = rotation.y;
				sample[5] = rotation.z;
				sample[6] = rotation.w;
			}
		}
		else if (transformFormat == PoseFormat.Transform12D)
		{
			Matrix4x4 localToWorldMatrix = base.gameObject.transform.localToWorldMatrix;
			for (int i = 0; i < 3; i++)
			{
				for (int j = 0; j < 4; j++)
				{
					sample[j + 4 * i] = localToWorldMatrix[i, j];
				}
			}
		}
		return true;
	}
}
