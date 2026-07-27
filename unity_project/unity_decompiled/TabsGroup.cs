using System.Collections.Generic;
using UnityEngine;

public class TabsGroup : MonoBehaviour
{
	public List<TabButtons> _tabs;

	public Color _idleColor;

	public Color _hoverColor;

	public Color _activeColor;

	private TabButtons _selectedBtn;

	[SerializeField]
	private List<GameObject> _objectsView;

	public void Subscribe(TabButtons tab)
	{
		if (_tabs == null)
		{
			_tabs = new List<TabButtons>();
		}
		_tabs.Add(tab);
		if (int.Parse(tab.transform.name.Substring(tab.name.Length - 1)) - 1 == 0)
		{
			OnTabSelect(tab);
		}
		else
		{
			OnTabExit(tab);
		}
	}

	public void OnTabEnter(TabButtons tab)
	{
		ResetTabs();
		if (_selectedBtn == null || tab != _selectedBtn)
		{
			tab._background.color = _hoverColor;
		}
	}

	public void OnTabExit(TabButtons tab)
	{
		ResetTabs();
	}

	public void OnTabSelect(TabButtons tab)
	{
		if (_selectedBtn != null)
		{
			_selectedBtn.Deselect();
		}
		_selectedBtn = tab;
		_selectedBtn.Select();
		ResetTabs();
		tab._background.color = _activeColor;
		int num = int.Parse(tab.transform.name.Substring(tab.name.Length - 1));
		for (int i = 0; i < _objectsView.Count; i++)
		{
			if (i == num - 1)
			{
				_objectsView[i].SetActive(value: true);
			}
			else
			{
				_objectsView[i].SetActive(value: false);
			}
		}
	}

	public void ResetTabs()
	{
		foreach (TabButtons tab in _tabs)
		{
			if (!(_selectedBtn != null) || !(tab == _selectedBtn))
			{
				tab._background.color = _idleColor;
			}
		}
	}
}
