using Unity.Cinemachine;
using UnityEngine;

public class DollyOffset : MonoBehaviour
{
	public CinemachineSplineCart cart;

	public CinemachineSplineCart targetCart;

	public float offset;

	private void LateUpdate()
	{
		if (!(cart == null) && !(targetCart == null))
		{
			cart.SplinePosition = targetCart.SplinePosition + offset;
		}
	}
}
