using UnityEngine;

[ExecuteAlways]
public class DynamicLightController : MonoBehaviour
{
	[SerializeField]
	private float maxDisplayDistance = 50f;

	[SerializeField]
	private float attenuationDistance = 5f;

	[SerializeField]
	private AnimationCurve intensityCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

	[SerializeField]
	private AnimationCurve rangeCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

	private Light m_light;

	private LightFlicker m_flicker;

	private float m_origIntensity;

	private float m_origRange;

	private void Awake()
	{
		m_light = GetComponent<Light>();
		m_origIntensity = m_light.intensity;
		m_origRange = m_light.range;
		m_flicker = m_light.GetComponent<LightFlicker>();
		DynamicLightsManager.AddLight(this);
	}

	private void OnDestroy()
	{
		DynamicLightsManager.RemoveLight(this);
	}

	public void UpdateForCamera(Camera _camera)
	{
		if (base.enabled)
		{
			float num = Vector3.Distance(_camera.transform.position, base.transform.position);
			if (num < maxDisplayDistance)
			{
				m_light.enabled = true;
				float time = 1f - Mathf.InverseLerp(maxDisplayDistance - attenuationDistance, maxDisplayDistance, num);
				m_light.intensity = intensityCurve.Evaluate(time) * ((m_flicker != null) ? m_flicker.modifiedIntensity : m_origIntensity);
				m_light.range = rangeCurve.Evaluate(time) * m_origRange;
			}
			else
			{
				m_light.enabled = false;
			}
		}
	}

	public void Reset()
	{
		m_light.enabled = base.enabled;
		m_light.intensity = ((m_flicker != null) ? m_flicker.modifiedIntensity : m_origIntensity);
		m_light.range = m_origRange;
	}
}
