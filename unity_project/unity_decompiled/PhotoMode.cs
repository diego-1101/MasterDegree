using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using SimpleFileBrowser;
using TMPro;
using UnityEngine;

public class PhotoMode : MonoBehaviour
{
	public bool modeActive;

	[Header("Path")]
	[Space]
	[SerializeField]
	private string _pathFolder;

	[SerializeField]
	private string _folderName = "Screenshots";

	[SerializeField]
	private TMP_Text _pathMesh;

	[Space]
	[Header("Canvas Group")]
	[Space]
	public CanvasGroup configsGroup;

	public CanvasGroup iniciarGroup;

	public CanvasGroup statusGroup;

	public GameObject btnPhoto;

	public GameObject zoomOBJ;

	public GameObject inputLeft;

	public GameObject inputRight;

	[HideInInspector]
	public Dictionary<string, List<string>> pathDataInfos;

	private void Start()
	{
		if (PlayerPrefs.HasKey("PathFolder"))
		{
			_pathFolder = PlayerPrefs.GetString("PathFolder");
			_pathMesh.text = _pathFolder;
		}
	}

	public void EntryPhotoMode()
	{
		if (!string.IsNullOrEmpty(_pathFolder))
		{
			UIConfigs.instance.configsUI.SetActive(value: true);
			btnPhoto.SetActive(value: true);
			inputLeft.SetActive(value: true);
			inputRight.SetActive(value: true);
			zoomOBJ.SetActive(value: false);
			configsGroup.alpha = 0f;
			iniciarGroup.alpha = 0f;
			modeActive = true;
		}
	}

	public void SwitchCam()
	{
		PlayerController.instance.SwitchCam();
	}

	public void LeavePhotoMode()
	{
		btnPhoto.SetActive(value: false);
		inputLeft.SetActive(value: false);
		inputRight.SetActive(value: false);
		zoomOBJ.SetActive(value: true);
		UIConfigs.instance.configsUI.SetActive(value: false);
		configsGroup.alpha = 1f;
		iniciarGroup.alpha = 1f;
		modeActive = false;
		PlayerController instance = PlayerController.instance;
		instance.mainCam.gameObject.SetActive(value: true);
		instance.lateralCam.gameObject.SetActive(value: false);
	}

	public void TakeScreenshot()
	{
		if (!Directory.Exists(_pathFolder))
		{
			Directory.CreateDirectory(_pathFolder);
		}
		StartCoroutine(ScreenshotCoroutine());
	}

	private IEnumerator ScreenshotCoroutine()
	{
		statusGroup.alpha = 0f;
		yield return new WaitForEndOfFrame();
		string path = $"screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png";
		ScreenCapture.CaptureScreenshot(Path.Combine(_pathFolder, path));
		statusGroup.alpha = 1f;
	}

	public void SelecionarPath()
	{
		FileBrowser.SetFilters(showAllFilesFilter: false);
		StartCoroutine(SelectPathCoroutine());
	}

	private IEnumerator SelectPathCoroutine()
	{
		yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.Folders, allowMultiSelection: false, null, null, "Selecione o caminho", "Selecionar");
		if (FileBrowser.Success)
		{
			for (int i = 0; i < FileBrowser.Result.Length; i++)
			{
				_pathFolder = OnFilesSelected(FileBrowser.Result);
			}
			_pathFolder = Path.Combine(_pathFolder, _folderName);
			_pathMesh.text = _pathFolder;
			PlayerPrefs.SetString("PathFolder", _pathFolder);
		}
	}

	private string OnFilesSelected(string[] filePaths)
	{
		return filePaths[0];
	}
}
