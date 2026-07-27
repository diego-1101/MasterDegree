using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class UIVirtualButton : MonoBehaviour, IPointerDownHandler, IEventSystemHandler, IPointerUpHandler, IPointerClickHandler
{
	[Serializable]
	public class BoolEvent : UnityEvent<bool>
	{
	}

	[Serializable]
	public class Event : UnityEvent
	{
	}

	[Header("Output")]
	public BoolEvent buttonStateOutputEvent;

	public Event buttonClickOutputEvent;

	public void OnPointerDown(PointerEventData eventData)
	{
		OutputButtonStateValue(buttonState: true);
	}

	public void OnPointerUp(PointerEventData eventData)
	{
		OutputButtonStateValue(buttonState: false);
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		OutputButtonClickEvent();
	}

	private void OutputButtonStateValue(bool buttonState)
	{
		buttonStateOutputEvent.Invoke(buttonState);
	}

	private void OutputButtonClickEvent()
	{
		buttonClickOutputEvent.Invoke();
	}
}
