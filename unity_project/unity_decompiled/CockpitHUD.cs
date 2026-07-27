using UnityEngine;

public class CockpitHUD : MonoBehaviour
{
	[Header("Fighter Ship")]
	public Transform fighterShip;

	private void Update()
	{
		fighterShip.forward = Vector3.forward;
	}
}
