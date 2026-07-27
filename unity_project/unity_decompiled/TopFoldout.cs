using UnityEngine;

[ExecuteAlways]
public class TopFoldout : MonoBehaviour
{
	public float RotationSpeed;

	public float PieceOffset;

	private Transform[] children;

	private void Start()
	{
		children = base.transform.GetComponentsInChildren<Transform>();
	}

	private void Update()
	{
		for (int i = 1; i < children.Length; i++)
		{
			Transform transform = children[i];
			Vector3 localPosition = transform.localPosition;
			localPosition.y = 0f;
			localPosition.x = 0f;
			localPosition.z = 0f;
			localPosition += base.transform.InverseTransformDirection(transform.up) * PieceOffset;
			transform.localPosition = localPosition;
		}
		base.transform.Rotate(Vector3.up, RotationSpeed * Time.deltaTime);
	}
}
