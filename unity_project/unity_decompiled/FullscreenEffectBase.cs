using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[ExecuteAlways]
public class FullscreenEffectBase<T> : MonoBehaviour where T : FullscreenPassBase<FullscreenPassDataBase>, new()
{
	private T _pass;

	[SerializeField]
	private string _passName = "Fullscreen Pass";

	[SerializeField]
	private Material _material;

	[SerializeField]
	private RenderPassEvent _injectionPoint = RenderPassEvent.BeforeRenderingTransparents;

	[SerializeField]
	private int _injectionPointOffset;

	[SerializeField]
	private ScriptableRenderPassInput _inputRequirements = ScriptableRenderPassInput.Color;

	[SerializeField]
	private CameraType _cameraType = CameraType.Game | CameraType.SceneView;

	private void OnEnable()
	{
		SetupPass();
		RenderPipelineManager.beginCameraRendering += OnBeginCamera;
	}

	private void OnDisable()
	{
		RenderPipelineManager.beginCameraRendering -= OnBeginCamera;
	}

	public virtual void SetupPass()
	{
		if (_pass == null)
		{
			_pass = new T();
		}
		_pass.renderPassEvent = _injectionPoint + _injectionPointOffset;
		_pass.material = _material;
		if (_material != null)
		{
			_pass.hasYFlipKeyword = _material.shader.keywordSpace.keywordNames.Contains("_FLIPY");
			if (_pass.hasYFlipKeyword)
			{
				_pass.yFlipKeyword = new LocalKeyword(_material.shader, "_FLIPY");
			}
		}
		_pass.passName = _passName;
		_pass.inputRequirements = _inputRequirements;
		_pass.ConfigureInput(_inputRequirements);
	}

	public virtual void OnBeginCamera(ScriptableRenderContext ctx, Camera cam)
	{
		if (_pass != null && !(_material == null) && (cam.cameraType & _cameraType) != 0)
		{
			cam.GetUniversalAdditionalCameraData().scriptableRenderer.EnqueuePass(_pass);
		}
	}

	private void OnValidate()
	{
		SetupPass();
	}
}
