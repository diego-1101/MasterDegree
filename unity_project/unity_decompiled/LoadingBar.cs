using UnityEngine;

public class LoadingBar : MonoBehaviour
{
	[SerializeField]
	private bool m_Armed;

	[SerializeField]
	private Transform m_LookAtTransform;

	[SerializeField]
	private float m_ActivationDistance = 3f;

	[SerializeField]
	private float m_LookSize;

	[SerializeField]
	private bool m_AlwaysOn;

	private bool m_Loading;

	private GameObject m_BaseCam;

	private Animator m_ControlPanelAnimator;

	private void Start()
	{
		if (SceneTransitionManager.IsAvailable())
		{
			m_BaseCam = SceneTransitionManager.GetMainCamera();
			m_ControlPanelAnimator = GetComponent<Animator>();
		}
		else
		{
			Object.Destroy(this);
		}
	}

	private void OnEnable()
	{
		if (m_AlwaysOn)
		{
			TurnOn();
		}
	}

	private void Update()
	{
		Vector3 forward = m_BaseCam.transform.forward;
		Vector3 position = m_BaseCam.transform.position;
		UpdateRotation(position);
		if (m_Armed && PointOfViewWithinArea(position, forward))
		{
			StartLoading();
		}
		else if (m_Loading)
		{
			StopLoading();
		}
	}

	private void UpdateRotation(Vector3 cameraPosition)
	{
		if (m_LookAtTransform != null)
		{
			Quaternion b = Quaternion.LookRotation(cameraPosition - m_LookAtTransform.position);
			m_LookAtTransform.rotation = Quaternion.Slerp(m_LookAtTransform.rotation, b, 1f * Time.deltaTime);
		}
	}

	private bool PointOfViewWithinArea(Vector3 cameraPosition, Vector3 cameraLookDirection)
	{
		float num = Vector3.Distance(m_LookAtTransform.position, cameraPosition);
		if (num > m_ActivationDistance)
		{
			return false;
		}
		float num2 = Mathf.Atan(m_LookSize * 0.5f / num) * 57.2957f;
		if (Vector3.Angle((m_LookAtTransform.position - m_BaseCam.transform.position).normalized, cameraLookDirection) < num2)
		{
			return true;
		}
		return false;
	}

	public void StartLoading()
	{
		if (!m_Loading)
		{
			if (m_ControlPanelAnimator != null)
			{
				m_ControlPanelAnimator.SetBool("Loading", value: true);
			}
			m_Loading = true;
			SceneTransitionManager.StartTransition();
		}
	}

	public void StopLoading()
	{
		if (m_Loading)
		{
			if (m_ControlPanelAnimator != null)
			{
				m_ControlPanelAnimator.SetBool("Loading", value: false);
			}
			m_Loading = false;
			SceneTransitionManager.StopTransition();
		}
	}

	public void TurnOn()
	{
		if (m_ControlPanelAnimator != null)
		{
			m_ControlPanelAnimator.SetBool("On", value: true);
		}
	}

	public void TurnOff()
	{
		if (m_ControlPanelAnimator != null)
		{
			m_ControlPanelAnimator.SetBool("On", value: false);
		}
	}
}
