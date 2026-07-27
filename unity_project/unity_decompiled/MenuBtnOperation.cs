using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

public class MenuBtnOperation : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler, IPointerClickHandler, IDeselectHandler
{
	[SerializeField]
	private UnityEvent _myEvent;

	[Space]
	[Header("Outline")]
	public Image _outlineImage;

	[Space]
	[Header("Handler")]
	public Color _colorHandler;

	public Color _colorNormal;

	public Color _colorClick;

	[HideInInspector]
	public bool isSelected;

	[SerializeField]
	private bool OnSkin;

	private void Start()
	{
		_outlineImage.color = _colorNormal;
	}

	private void OnValidate()
	{
	}

	public void OnDeselect(BaseEventData eventData)
	{
		isSelected = false;
		_outlineImage.color = _colorNormal;
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		if (eventData.button != 0)
		{
			return;
		}
		if (base.transform.parent.TryGetComponent<Toggle>(out var _))
		{
			StartCoroutine(ClickToggleColor());
			_myEvent.Invoke();
			return;
		}
		if (OnSkin && UIConfigs.instance.actSkin != this)
		{
			if (UIConfigs.instance.actSkin != null)
			{
				UIConfigs.instance.actSkin.isSelected = false;
				UIConfigs.instance.actSkin._outlineImage.color = UIConfigs.instance.actSkin._colorNormal;
			}
			UIConfigs.instance.actSkin = this;
		}
		isSelected = true;
		StartCoroutine(ClickColor());
		_myEvent.Invoke();
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		if (!isSelected)
		{
			_outlineImage.color = _colorHandler;
		}
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		if (!isSelected)
		{
			_outlineImage.color = _colorNormal;
		}
	}

	public void Toggled(Toggle toggle)
	{
		toggle.isOn = !toggle.isOn;
	}

	private IEnumerator ClickColor()
	{
		_outlineImage.color = _colorHandler;
		yield return new WaitForSeconds(0.1f);
		_outlineImage.color = _colorClick;
	}

	private IEnumerator ClickToggleColor()
	{
		_outlineImage.color = _colorHandler;
		yield return new WaitForSeconds(0.1f);
		_outlineImage.color = _colorClick;
		yield return new WaitForSeconds(0.1f);
		_outlineImage.color = _colorNormal;
	}
}
