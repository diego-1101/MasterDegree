using UnityEngine;
using UnityEngine.UI;

public class DeviceTypeUI : MonoBehaviour
{
	private void Start()
	{
		string graphicsDeviceName = SystemInfo.graphicsDeviceName;
		string text = SystemInfo.graphicsDeviceType.ToString();
		GetComponent<Text>().text = graphicsDeviceName + "\n" + text;
	}
}
