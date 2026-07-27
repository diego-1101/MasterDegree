using UnityEngine;

[ExecuteAlways]
public class SpeedParticles : MonoBehaviour
{
	public Transform Ship;

	private ParticleSystem m_SpeedParticles;

	private Vector3 m_LastShipPos;

	private void Start()
	{
	}

	private void Update()
	{
		if (m_SpeedParticles == null)
		{
			m_SpeedParticles = GetComponent<ParticleSystem>();
		}
		float num = (Ship.position - m_LastShipPos).magnitude / Time.deltaTime;
		ParticleSystem.MainModule main = m_SpeedParticles.main;
		main.startSpeed = num * 2f;
		m_LastShipPos = Ship.position;
	}
}
