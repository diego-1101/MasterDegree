using UnityEngine;

public class WashiLightController : MonoBehaviour
{
	private enum ChannelIndex
	{
		r,
		g,
		b,
		a
	}

	[SerializeField]
	private Vector3 size = new Vector3(4f, 2f, 0.5f);

	[SerializeField]
	private Material referenceMaterial;

	private Material _instancedMaterial;

	[SerializeField]
	private Texture2D texture;

	[SerializeField]
	private ChannelIndex channelIndex;

	[SerializeField]
	private float minIntensity = 0.5f;

	[SerializeField]
	private float maxIntensity = 2f;

	[SerializeField]
	private float randomDuration = 1f;

	[SerializeField]
	private float presenceRandomDuration = 5f;

	[SerializeField]
	[Range(0f, 1f)]
	private float presenceAmount = 0.5f;

	[SerializeField]
	private Shader shaderSearchFilter;

	[SerializeField]
	private Renderer[] renderers;

	private Vector3 worldU = Vector3.right;

	private Vector3 worldV = Vector3.up;

	private Vector4 tilingOffset = new Vector4(1f, 1f, 0f, 0f);

	[SerializeField]
	private bool displayAffectedRenderers;

	private float intensitySeed;

	private float presenceSeed;

	private Material instancedMaterial
	{
		get
		{
			if (_instancedMaterial == null)
			{
				_instancedMaterial = Object.Instantiate(referenceMaterial);
				_instancedMaterial.name = base.gameObject.name + " " + referenceMaterial.name;
			}
			return _instancedMaterial;
		}
	}

	private void OnValidate()
	{
		size.x = Mathf.Abs(size.x);
		size.y = Mathf.Abs(size.y);
		size.z = Mathf.Abs(size.z);
	}

	private void Start()
	{
		ApplyTexture();
		intensitySeed = Random.value * 100f;
		presenceSeed = Random.value * 100f;
	}

	private void Update()
	{
		float value = Mathf.Lerp(minIntensity, maxIntensity, Mathf.PerlinNoise(Time.time / randomDuration, intensitySeed));
		bool flag = presenceAmount >= 1f || Mathf.PerlinNoise(Time.time / presenceRandomDuration, presenceSeed) < presenceAmount;
		instancedMaterial.SetFloat("_Intensity", value);
		Renderer[] array = renderers;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].enabled = flag;
		}
	}

	[ContextMenu("Apply Texture")]
	private void ApplyTexture()
	{
		base.transform.localScale = Vector3.one;
		worldU = base.transform.right;
		worldV = base.transform.up;
		Vector3 lhs = base.transform.TransformPoint(-0.5f * size);
		Vector3 lhs2 = base.transform.TransformPoint(0.5f * size);
		Vector2 vector = new Vector2(Vector3.Dot(lhs, worldU), Vector3.Dot(lhs, worldV));
		Vector2 vector2 = new Vector2(Vector3.Dot(lhs2, worldU), Vector3.Dot(lhs2, worldV));
		tilingOffset = new Vector4(vector2.x - vector.x, vector2.y - vector.y, vector.x, vector.y);
		instancedMaterial.SetVector("_World_U", worldU);
		instancedMaterial.SetVector("_World_V", worldV);
		instancedMaterial.SetVector("_TilingOffset", tilingOffset);
		instancedMaterial.SetTexture("_Texture", texture);
		instancedMaterial.SetFloat("_Texture_Channel", (float)channelIndex);
		Renderer[] array = renderers;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].sharedMaterial = instancedMaterial;
		}
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.yellow;
		if (displayAffectedRenderers)
		{
			Renderer[] array = renderers;
			foreach (Renderer renderer in array)
			{
				if (renderer.enabled && renderer.gameObject.activeSelf)
				{
					Mesh mesh = renderer.GetComponent<MeshFilter>()?.sharedMesh;
					if (mesh != null)
					{
						Gizmos.matrix = renderer.transform.localToWorldMatrix;
						Gizmos.DrawWireMesh(mesh);
					}
				}
			}
		}
		Gizmos.matrix = Matrix4x4.TRS(base.transform.position, base.transform.rotation, Vector3.one);
		Gizmos.DrawWireCube(Vector3.zero, size);
	}
}
