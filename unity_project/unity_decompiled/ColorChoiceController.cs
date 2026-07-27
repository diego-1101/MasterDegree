using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ColorChoiceController : MonoBehaviour
{
	public float currentHue;

	public float currentSat;

	public float currentVal;

	[SerializeField]
	private RawImage hueImage;

	[SerializeField]
	private RawImage satValImage;

	[SerializeField]
	private RawImage outputImage;

	[SerializeField]
	private Slider hueSlider;

	[SerializeField]
	private TMP_InputField hexInputField;

	private Texture2D hueTexture;

	private Texture2D svTexture;

	private Texture2D outputTexture;

	[SerializeField]
	private string defaultHex;

	[SerializeField]
	private Image currentImage;

	private void Start()
	{
		CreateHueImage();
		CreateSVImage();
		CreateOutputImage();
		UpdateOutputImage();
		hexInputField.text = defaultHex;
		OnTextInput();
	}

	private void CreateHueImage()
	{
		hueTexture = new Texture2D(1, 16);
		hueTexture.wrapMode = TextureWrapMode.Clamp;
		hueTexture.name = "HueTexture";
		for (int i = 0; i < hueTexture.height; i++)
		{
			hueTexture.SetPixel(0, i, Color.HSVToRGB((float)i / (float)hueTexture.height, 1f, 1f));
		}
		hueTexture.Apply();
		currentHue = 0f;
		hueImage.texture = hueTexture;
	}

	private void CreateSVImage()
	{
		svTexture = new Texture2D(16, 16);
		svTexture.wrapMode = TextureWrapMode.Clamp;
		svTexture.name = "SatValTexture";
		for (int i = 0; i < svTexture.height; i++)
		{
			for (int j = 0; j < svTexture.width; j++)
			{
				svTexture.SetPixel(j, i, Color.HSVToRGB(currentHue, (float)j / (float)svTexture.width, (float)i / (float)svTexture.height));
			}
		}
		svTexture.Apply();
		currentSat = 0f;
		currentVal = 0f;
		satValImage.texture = svTexture;
	}

	private void CreateOutputImage()
	{
		outputTexture = new Texture2D(1, 16);
		outputTexture.wrapMode = TextureWrapMode.Clamp;
		outputTexture.name = "OutputTexture";
		Color color = Color.HSVToRGB(currentHue, currentSat, currentVal);
		for (int i = 0; i < outputTexture.height; i++)
		{
			outputTexture.SetPixel(0, i, color);
		}
		outputTexture.Apply();
		outputImage.texture = outputTexture;
	}

	private void UpdateOutputImage()
	{
		Color color = Color.HSVToRGB(currentHue, currentSat, currentVal);
		for (int i = 0; i < outputTexture.height; i++)
		{
			outputTexture.SetPixel(0, i, color);
		}
		outputTexture.Apply();
		hexInputField.text = ColorUtility.ToHtmlStringRGB(color);
		currentImage.color = color;
	}

	public void SetSV(float s, float v)
	{
		currentSat = s;
		currentVal = v;
		UpdateOutputImage();
	}

	public void UpdateSVImage()
	{
		currentHue = hueSlider.value;
		for (int i = 0; i < svTexture.height; i++)
		{
			for (int j = 0; j < svTexture.width; j++)
			{
				svTexture.SetPixel(j, i, Color.HSVToRGB(currentHue, (float)j / (float)svTexture.width, (float)i / (float)svTexture.height));
			}
		}
		svTexture.Apply();
		UpdateOutputImage();
	}

	public void OnTextInput()
	{
		if (hexInputField.text.Length >= 6)
		{
			if (ColorUtility.TryParseHtmlString("#" + hexInputField.text, out var color))
			{
				Color.RGBToHSV(color, out currentHue, out currentSat, out currentVal);
			}
			hueSlider.value = currentHue;
			hexInputField.text = "";
			UpdateOutputImage();
		}
	}
}
