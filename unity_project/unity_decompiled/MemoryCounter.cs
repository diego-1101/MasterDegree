using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UI;

[RequireComponent(typeof(Text))]
public class MemoryCounter : MonoBehaviour
{
	private Text m_GuiText;

	private void Start()
	{
		m_GuiText = GetComponent<Text>();
	}

	private void Update()
	{
		Debug.Log("Mono used size" + Profiler.GetMonoUsedSizeLong() / 1000000 + "Bytes");
	}
}
