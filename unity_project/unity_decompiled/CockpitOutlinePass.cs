using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public class CockpitOutlinePass : FullscreenPassBase<FullscreenPassDataBase>
{
	private bool IsOutlineEnabled()
	{
		return VolumeManager.instance.stack.GetComponent<OutlineVolumeComponent>().Enabled.value;
	}

	public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
	{
		if (IsOutlineEnabled())
		{
			base.Execute(context, ref renderingData);
		}
	}

	public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
	{
		if (IsOutlineEnabled())
		{
			base.RecordRenderGraph(renderGraph, frameData);
		}
	}
}
