using UnityEngine;

public class TrailRendererClear : MonoBehaviour
{
	private TrailRenderer _trailRenderer;

	private void OnEnable()
	{
		if (_trailRenderer == null)
		{
			TryGetComponent<TrailRenderer>(out _trailRenderer);
		}
		if ((bool)_trailRenderer)
		{
			_trailRenderer.Clear();
		}
	}
}
