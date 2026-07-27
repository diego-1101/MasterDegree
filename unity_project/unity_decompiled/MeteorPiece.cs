using UnityEngine;

public class MeteorPiece : MonoBehaviour
{
	public Transform explosionOrigin;

	[Range(0f, 1f)]
	public float T;

	private Vector3 m_RelativePosition;

	private void Start()
	{
		m_RelativePosition = base.transform.position - explosionOrigin.position;
	}

	private void Update()
	{
		base.transform.position = explosionOrigin.position + m_RelativePosition + m_RelativePosition * (1f / m_RelativePosition.magnitude);
	}
}
