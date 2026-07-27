using System.Linq;
using System.Text;
using UnityEngine;

public class QualityLevelToggle : MonoBehaviour
{
	[SerializeField]
	private int m_Mask;

	[SerializeField]
	private string[] m_ValidLevels;

	private void Start()
	{
		UpdateActiveState();
	}

	private void UpdateActiveState()
	{
		string value = QualitySettings.names[QualitySettings.GetQualityLevel()];
		base.gameObject.SetActive(m_ValidLevels.Contains(value));
	}

	private void PrintValidLevels()
	{
		StringBuilder stringBuilder = new StringBuilder("Valid Levels: ");
		string[] validLevels = m_ValidLevels;
		foreach (string value in validLevels)
		{
			stringBuilder.Append(value);
			stringBuilder.Append(", ");
		}
		Debug.Log(stringBuilder.ToString());
	}
}
