using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public class OasisFogPass : FullscreenPassBase<FullscreenPassDataBase>
{
	private void UpdateVolumeSettings()
	{
		OasisFogVolumeComponent component = VolumeManager.instance.stack.GetComponent<OasisFogVolumeComponent>();
		float value = component.Density.value;
		if (!(value < Mathf.Epsilon))
		{
			float value2 = component.StartDistance.value;
			Color value3 = component.Tint.value;
			float value4 = component.SunScatteringIntensity.value;
			Vector2 value5 = component.HeightRange.value;
			material.SetColor("_Tint", value3);
			material.SetFloat("_Density", value);
			material.SetFloat("_StartDistance", value2);
			material.SetFloat("_SunScatteringIntensity", value4);
			material.SetVector("_Height_Range", value5);
		}
	}

	public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
	{
		UpdateVolumeSettings();
		base.Execute(context, ref renderingData);
	}

	public override void ExecuteRenderGraph(FullscreenPassDataBase passData, RasterGraphContext rgContext)
	{
		UpdateVolumeSettings();
		base.ExecuteRenderGraph(passData, rgContext);
	}
}
