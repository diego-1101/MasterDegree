using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SVImageControl : MonoBehaviour, IDragHandler, IEventSystemHandler, IPointerClickHandler
{
	[SerializeField]
	private Image selectImage;

	private RawImage svImage;

	private ColorChoiceController colorChoice;

	private RectTransform rectTransform;

	private RectTransform selectTransform;

	public void OnDrag(PointerEventData eventData)
	{
		UpdateColor(eventData);
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		UpdateColor(eventData);
	}

	private void Awake()
	{
		svImage = GetComponent<RawImage>();
		colorChoice = Object.FindAnyObjectByType<ColorChoiceController>();
		rectTransform = GetComponent<RectTransform>();
		selectTransform = selectImage.GetComponent<RectTransform>();
		selectTransform.position = new Vector2(0f - rectTransform.sizeDelta.x * 0.5f, 0f - rectTransform.sizeDelta.y * 0.5f);
	}

	private void UpdateColor(PointerEventData eventData)
	{
		Vector3 localPosition = rectTransform.InverseTransformPoint(eventData.position);
		float num = rectTransform.sizeDelta.x * 0.5f;
		float num2 = rectTransform.sizeDelta.y * 0.5f;
		if (localPosition.x < 0f - num)
		{
			localPosition.x = 0f - num;
		}
		else if (localPosition.x > num)
		{
			localPosition.x = num;
		}
		if (localPosition.y < 0f - num2)
		{
			localPosition.y = 0f - num2;
		}
		else if (localPosition.y > num2)
		{
			localPosition.y = num2;
		}
		float num3 = localPosition.x + num;
		float num4 = localPosition.y + num2;
		float s = num3 / rectTransform.sizeDelta.x;
		float num5 = num4 / rectTransform.sizeDelta.y;
		selectTransform.localPosition = localPosition;
		selectImage.color = Color.HSVToRGB(0f, 0f, 1f - num5);
		colorChoice.SetSV(s, num5);
	}
}
