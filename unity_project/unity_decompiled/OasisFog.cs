using UnityEngine;
using UnityEngine.Rendering;

public class OasisFog : FullscreenEffectBase<OasisFogPass>
{
	public override void OnBeginCamera(ScriptableRenderContext ctx, Camera cam)
	{
		if (!SceneTransitionManager.IsAvailable() || !SceneTransitionManager.IsInTerminal() || !(cam != SceneTransitionManager.GetMainCamera().GetComponent<Camera>()))
		{
			base.OnBeginCamera(ctx, cam);
		}
	}
}
