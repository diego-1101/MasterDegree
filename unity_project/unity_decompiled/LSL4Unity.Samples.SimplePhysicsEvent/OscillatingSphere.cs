using UnityEngine;

namespace LSL4Unity.Samples.SimplePhysicsEvent;

public class OscillatingSphere : MonoBehaviour
{
	private void Update()
	{
		base.gameObject.transform.position = new Vector3(Mathf.Sin(Time.time), 0f, 0f);
	}
}
