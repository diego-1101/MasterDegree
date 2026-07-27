using UnityEngine;
using UnityEngine.Experimental.Rendering;

public class ScreenCamera : MonoBehaviour
{
	[SerializeField]
	private float m_RenderTextureScale = 1f;

	private Camera m_Camera;

	private void Awake()
	{
		m_Camera = GetComponent<Camera>();
		UpdateTarget();
	}

	private void UpdateTarget()
	{
		m_Camera.targetTexture = new RenderTexture((int)((float)m_Camera.scaledPixelWidth * m_RenderTextureScale), (int)((float)m_Camera.scaledPixelHeight * m_RenderTextureScale), GraphicsFormat.R16G16B16A16_SFloat, GraphicsFormat.D24_UNorm_S8_UInt);
	}
}
