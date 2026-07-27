using UnityEngine;

public class ParticleSpawnSystem : MonoBehaviour
{
	public ParticleSystem PS;

	public float speed = 10f;

	public void Spawn(Vector3 positionWS, Vector3 directionWS, Vector3 endPoint)
	{
		ParticleSystem.EmitParams emitParams = default(ParticleSystem.EmitParams);
		emitParams.position = positionWS;
		emitParams.rotation3D = Quaternion.LookRotation(directionWS).eulerAngles;
		float magnitude = (endPoint - positionWS).magnitude;
		emitParams.velocity = directionWS * speed;
		emitParams.startLifetime = magnitude / speed;
		PS.Emit(emitParams, 1);
	}
}
