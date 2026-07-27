using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class TabButtons : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler, IPointerClickHandler
{
	private TabsGroup _group;

	[HideInInspector]
	public Image _background;

	public UnityEvent onTabSelected;

	public UnityEvent onTabDeselected;

	private void Start()
	{
		_background = GetComponent<Image>();
		_group = GetComponentInParent<TabsGroup>();
		_group.Subscribe(this);
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		_group.OnTabEnter(this);
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		_group.OnTabExit(this);
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		_group.OnTabSelect(this);
	}

	public void Select()
	{
		onTabSelected?.Invoke();
	}

	public void Deselect()
	{
		onTabDeselected?.Invoke();
	}
}
