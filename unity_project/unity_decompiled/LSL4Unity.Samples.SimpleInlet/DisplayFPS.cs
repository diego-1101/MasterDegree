using UnityEngine;
using UnityEngine.UI;

namespace LSL4Unity.Samples.SimpleInlet;

public class DisplayFPS : MonoBehaviour
{
	private Text m_Text;

	private void Start()
	{
		m_Text = GetComponent<Text>();
		InvokeRepeating("GetFPS", 1f, 1f);
	}

	private void GetFPS()
	{
		float num = (int)(1f / Time.unscaledDeltaTime);
		m_Text.text = "FPS: " + num;
	}
}
