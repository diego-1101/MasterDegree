using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CueConfigs : MonoBehaviour
{
	public TMP_InputField field;

	public Button minusBtn;

	public Button imageBtn;

	public string path;

	public TMP_Dropdown dropdown;

	public void SetCueImage()
	{
		RawImage cueImageDirection = GrazController.instance.cueImageDirection;
		if (string.IsNullOrEmpty(path))
		{
			cueImageDirection.texture = GrazController.instance.defaultCueImage;
			return;
		}
		byte[] data = File.ReadAllBytes(path);
		Texture2D texture2D = new Texture2D(2, 2);
		texture2D.LoadImage(data);
		cueImageDirection.texture = texture2D;
	}
}
