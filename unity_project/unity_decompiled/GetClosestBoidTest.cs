using UnityEngine;

public class GetClosestBoidTest : MonoBehaviour
{
	private Boids.Boid boid;

	private void Update()
	{
		if ((bool)Boids.Instance)
		{
			boid = Boids.Instance.GetClosestBoid(base.transform.position);
		}
	}

	private void OnDrawGizmos()
	{
		Gizmos.DrawLine(base.transform.position, boid.position);
	}
}
