using UnityEngine;

public class LightFlicker : MonoBehaviour
{
	[SerializeField]
	public float m_PositionJitterScale;

	[SerializeField]
	public float m_RotationJitterScale;

	[SerializeField]
	public float m_IntensityJitterScale;

	[SerializeField]
	public float m_Timescale;

	private Vector3 m_InitialPosition;

	private float m_InitialIntensity;

	private Quaternion m_InitialRotation;

	private float m_XSeed;

	private float m_YSeed;

	private float m_ZSeed;

	private Light m_Light;

	private float m_flickerIntensityOffset = 1f;

	public float flickerIntensityOffset => m_flickerIntensityOffset;

	public float modifiedIntensity => m_InitialIntensity + m_flickerIntensityOffset;

	private void Start()
	{
		Random.InitState(base.gameObject.GetInstanceID());
		m_XSeed = Random.value * 248f;
		m_YSeed = Random.value * 248f;
		m_ZSeed = Random.value * 248f;
		m_Light = GetComponent<Light>();
		m_InitialIntensity = m_Light.intensity;
		m_InitialPosition = base.transform.position;
		m_InitialRotation = base.transform.rotation;
	}

	private void Update()
	{
		float x = Time.time * m_Timescale + m_XSeed;
		float y = Time.time * m_Timescale + m_YSeed;
		float z = Time.time * m_Timescale + m_ZSeed;
		Vector3 vector = PerlinNoise3D(new Vector3(x, y, z), 2, 1f);
		vector = vector * 2f - Vector3.one;
		base.transform.SetPositionAndRotation(m_InitialPosition + vector * m_PositionJitterScale, m_InitialRotation * Quaternion.Euler(vector * m_RotationJitterScale));
		m_flickerIntensityOffset = vector.x * m_IntensityJitterScale;
		m_Light.intensity = modifiedIntensity;
	}

	private Vector3 PerlinNoise3D(Vector3 uv, int Octaves, float freq)
	{
		Vector3 zero = Vector3.zero;
		for (int i = 0; i < Octaves; i++)
		{
			zero.x += Mathf.PerlinNoise1D(uv.x * freq * (float)(i + 1));
			zero.y += Mathf.PerlinNoise1D(uv.y * freq * (float)(i + 1));
			zero.z += Mathf.PerlinNoise1D(uv.z * freq * (float)(i + 1));
		}
		return zero;
	}
}
