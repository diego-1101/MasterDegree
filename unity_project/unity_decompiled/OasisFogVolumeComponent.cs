using UnityEngine;
using UnityEngine.Rendering;

[VolumeComponentMenu("Sample Scene/Oasis Fog")]
public class OasisFogVolumeComponent : VolumeComponent
{
	public ClampedFloatParameter Density = new ClampedFloatParameter(0f, 0f, 0.05f);

	public MinFloatParameter StartDistance = new MinFloatParameter(0f, 0f);

	public Vector2Parameter HeightRange = new Vector2Parameter(new Vector2(0f, 50f));

	public ColorParameter Tint = new ColorParameter(Color.white, hdr: true, showAlpha: false, showEyeDropper: false);

	public MinFloatParameter SunScatteringIntensity = new MinFloatParameter(2f, 0f);
}
