using UnityEngine;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
	private static PlayerController _instance;

	[Header("Personagem")]
	public GameObject[] _pernaNormal = new GameObject[2];

	public GameObject[] _pernaProtese = new GameObject[2];

	public GameObject[] _coxaProtese = new GameObject[2];

	public GameObject[] _peProtese = new GameObject[2];

	[Space]
	public Renderer[] _handRenderer;

	[Space]
	public Renderer[] _legRenderer;

	[Space]
	[Header("Camera")]
	[Space]
	[SerializeField]
	private bool isLocked;

	public Camera mainCam;

	[SerializeField]
	private float mainCamRotateMin = 29f;

	[SerializeField]
	private float mainCamRotateMax = 69f;

	[SerializeField]
	private float mainCamHorizontalMoveMin = -0.5f;

	[SerializeField]
	private float mainCamHorizontalMoveMax = 0.7f;

	[SerializeField]
	private float mainCamVerticalMoveMin = -1f;

	[SerializeField]
	private float mainCamVerticalMoveMax;

	public Camera lateralCam;

	[SerializeField]
	private float latCamRotateMin = -14f;

	[SerializeField]
	private float latCamRotateMax = 14f;

	[SerializeField]
	private float latCamZoomMin = 1f;

	[SerializeField]
	private float latCamZoomMax = 2.5f;

	[SerializeField]
	private float latCamHorizontalMoveMin = -0.3f;

	[SerializeField]
	private float latCamHorizontalMoveMax = 0.88f;

	[SerializeField]
	private float latCamVerticalMoveMin = -1.8f;

	[SerializeField]
	private float latCamVerticalMoveMax = -0.6f;

	private StatusUIController configsUI;

	private Slider camRotation;

	private Slider camHorizontalMove;

	private Slider camVerticalMove;

	private Slider camZoom;

	private Slider inputLeft;

	private Slider inputRight;

	private Camera _camera;

	private float rotationX;

	private float cameraZPosition;

	private float cameraYPosition;

	private float cameraXPosition;

	[Space]
	[Header("Legs")]
	[Space]
	[SerializeField]
	private RaiseLeg _leftLeg;

	[SerializeField]
	private RaiseLeg _rightLeg;

	public static PlayerController instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = Object.FindAnyObjectByType<PlayerController>();
			}
			return _instance;
		}
	}

	public void LegChoice(string leg)
	{
		if (!Object.FindAnyObjectByType<GameUIUm>())
		{
			switch (leg)
			{
			case "esquerda":
				_pernaNormal[0].SetActive(value: true);
				_pernaNormal[1].SetActive(value: false);
				_pernaProtese[0].SetActive(value: false);
				_pernaProtese[1].SetActive(value: true);
				_coxaProtese[0].SetActive(value: false);
				_coxaProtese[1].SetActive(value: true);
				_peProtese[0].SetActive(value: false);
				_peProtese[1].SetActive(value: true);
				break;
			case "direita":
				_pernaNormal[0].SetActive(value: false);
				_pernaNormal[1].SetActive(value: true);
				_pernaProtese[0].SetActive(value: true);
				_pernaProtese[1].SetActive(value: false);
				_coxaProtese[0].SetActive(value: true);
				_coxaProtese[1].SetActive(value: false);
				_peProtese[0].SetActive(value: true);
				_peProtese[1].SetActive(value: false);
				break;
			case "ambas":
				_pernaNormal[0].SetActive(value: false);
				_pernaNormal[1].SetActive(value: false);
				_pernaProtese[0].SetActive(value: true);
				_pernaProtese[1].SetActive(value: true);
				_coxaProtese[0].SetActive(value: true);
				_coxaProtese[1].SetActive(value: true);
				_peProtese[0].SetActive(value: true);
				_peProtese[1].SetActive(value: true);
				break;
			default:
				_pernaNormal[0].SetActive(value: true);
				_pernaNormal[1].SetActive(value: true);
				_pernaProtese[0].SetActive(value: false);
				_pernaProtese[1].SetActive(value: false);
				_coxaProtese[0].SetActive(value: false);
				_coxaProtese[1].SetActive(value: false);
				_peProtese[0].SetActive(value: false);
				_peProtese[1].SetActive(value: false);
				break;
			}
		}
	}

	public void SetInputLeft(float value)
	{
		_leftLeg.inputValue = value;
	}

	public void SetInputRight(float value)
	{
		_rightLeg.inputValue = value;
	}

	private void Start()
	{
		_camera = mainCam;
		mainCam.gameObject.SetActive(value: true);
		lateralCam.gameObject.SetActive(value: false);
	}

	private void Update()
	{
		RealtimeLog.instance._inputPernas = new Vector2(GetInputValues()[0], GetInputValues()[1]);
		RealtimeLog.instance._pernaAngles = GetBothLegsAngles();
	}

	private void OnSliderChanged(float value)
	{
		rotationX = value;
		UpdateCameraRot();
	}

	private void UpdateCameraRot()
	{
		if (!(_camera == null))
		{
			if (_camera == mainCam)
			{
				_camera.transform.localRotation = Quaternion.Euler(rotationX, 0f, 0f);
			}
			else
			{
				_camera.transform.localRotation = Quaternion.Euler(rotationX, -90f, 0f);
			}
		}
	}

	public void StartPlayerController(StatusUIController _configsUI)
	{
		configsUI = _configsUI;
		if (configsUI.camRotation != null)
		{
			camRotation = configsUI.camRotation;
			if (_camera == mainCam)
			{
				camRotation.minValue = mainCamRotateMin;
				camRotation.maxValue = mainCamRotateMax;
			}
			else
			{
				camRotation.minValue = latCamRotateMin;
				camRotation.maxValue = latCamRotateMax;
			}
			camRotation.onValueChanged.AddListener(OnSliderChanged);
		}
		if (configsUI.camHorizontalMove != null)
		{
			camHorizontalMove = configsUI.camHorizontalMove;
			camHorizontalMove.minValue = 0f;
			camHorizontalMove.maxValue = 1f;
			camHorizontalMove.onValueChanged.AddListener(OnMoveSliderChangedHO);
			if (_camera == mainCam)
			{
				cameraZPosition = Mathf.Lerp(mainCamHorizontalMoveMin, mainCamHorizontalMoveMax, camHorizontalMove.value);
			}
			else
			{
				cameraZPosition = Mathf.Lerp(latCamHorizontalMoveMin, latCamHorizontalMoveMax, camHorizontalMove.value);
			}
			UpdateHorizontalCameraPosition();
		}
		if (configsUI.camVerticalMove != null)
		{
			camVerticalMove = configsUI.camVerticalMove;
			camVerticalMove.minValue = 0f;
			camVerticalMove.maxValue = 1f;
			camVerticalMove.onValueChanged.AddListener(OnMoveSliderChangedVE);
			if (_camera == mainCam)
			{
				cameraYPosition = Mathf.Lerp(mainCamVerticalMoveMin, mainCamVerticalMoveMax, camVerticalMove.value);
			}
			else
			{
				cameraYPosition = Mathf.Lerp(latCamVerticalMoveMin, latCamVerticalMoveMax, camVerticalMove.value);
			}
			UpdateVerticalCameraPosition();
		}
		if (_camera != mainCam && configsUI.camZoom != null)
		{
			camZoom = configsUI.camZoom;
			camZoom.minValue = 0f;
			camZoom.maxValue = 1f;
			camZoom.onValueChanged.AddListener(OnZoomSliderChanged);
			cameraXPosition = Mathf.Lerp(latCamZoomMin, latCamZoomMax, camZoom.value);
			UpdateZoomCameraPosition();
		}
		if (configsUI.inputLeft != null)
		{
			inputLeft = configsUI.inputLeft;
			inputLeft.minValue = 0f;
			inputLeft.maxValue = 1f;
			inputLeft.onValueChanged.AddListener(SetInputLeft);
		}
		if (configsUI.inputRight != null)
		{
			inputRight = configsUI.inputRight;
			inputRight.minValue = 0f;
			inputRight.maxValue = 1f;
			inputRight.onValueChanged.AddListener(SetInputRight);
		}
	}

	public void SwitchCam()
	{
		StatusUIController statusUI = UIConfigs.instance.statusUI;
		if (_camera == mainCam)
		{
			_camera = lateralCam;
			mainCam.gameObject.SetActive(value: false);
			lateralCam.gameObject.SetActive(value: true);
			statusUI.camZoom.transform.parent.gameObject.SetActive(value: true);
		}
		else
		{
			_camera = mainCam;
			mainCam.gameObject.SetActive(value: true);
			lateralCam.gameObject.SetActive(value: false);
			statusUI.camZoom.transform.parent.gameObject.SetActive(value: false);
		}
		CameraSetup();
		StartPlayerController(statusUI);
	}

	public void CameraSetup()
	{
		if (_camera == mainCam)
		{
			if (camRotation != null)
			{
				rotationX = camRotation.value;
				UpdateCameraRot();
				configsUI.camRotation.value = 41.33333f;
			}
			if (camHorizontalMove != null)
			{
				cameraZPosition = Mathf.Lerp(mainCamHorizontalMoveMin, mainCamHorizontalMoveMax, camHorizontalMove.value);
				UpdateHorizontalCameraPosition();
				configsUI.camHorizontalMove.value = 0.2672153f;
			}
			if (camVerticalMove != null)
			{
				cameraYPosition = Mathf.Lerp(mainCamVerticalMoveMin, mainCamVerticalMoveMax, camVerticalMove.value);
				UpdateVerticalCameraPosition();
				configsUI.camVerticalMove.value = 1f;
			}
		}
		else
		{
			if (camRotation != null)
			{
				rotationX = camRotation.value;
				UpdateCameraRot();
				configsUI.camRotation.value = 0f;
			}
			if (camHorizontalMove != null)
			{
				cameraZPosition = Mathf.Lerp(latCamHorizontalMoveMin, latCamHorizontalMoveMax, camHorizontalMove.value);
				UpdateHorizontalCameraPosition();
				configsUI.camHorizontalMove.value = 0.3f;
			}
			if (camVerticalMove != null)
			{
				cameraYPosition = Mathf.Lerp(latCamVerticalMoveMin, latCamVerticalMoveMax, camVerticalMove.value);
				UpdateVerticalCameraPosition();
				configsUI.camVerticalMove.value = 1.27f;
			}
		}
	}

	private void OnMoveSliderChangedHO(float value)
	{
		if (_camera == mainCam)
		{
			cameraZPosition = Mathf.Lerp(mainCamHorizontalMoveMin, mainCamHorizontalMoveMax, value);
		}
		else
		{
			cameraZPosition = Mathf.Lerp(latCamHorizontalMoveMin, latCamHorizontalMoveMax, value);
		}
		UpdateHorizontalCameraPosition();
	}

	private void OnMoveSliderChangedVE(float value)
	{
		if (_camera == mainCam)
		{
			cameraYPosition = Mathf.Lerp(mainCamVerticalMoveMin, mainCamVerticalMoveMax, value);
		}
		else
		{
			cameraYPosition = Mathf.Lerp(latCamVerticalMoveMin, latCamVerticalMoveMax, value);
		}
		UpdateVerticalCameraPosition();
	}

	private void OnZoomSliderChanged(float value)
	{
		cameraXPosition = Mathf.Lerp(latCamZoomMin, latCamZoomMax, value);
		UpdateZoomCameraPosition();
	}

	private void UpdateHorizontalCameraPosition()
	{
		if (!(_camera == null))
		{
			Vector3 localPosition = _camera.transform.localPosition;
			localPosition.z = cameraZPosition;
			_camera.transform.localPosition = localPosition;
		}
	}

	private void UpdateVerticalCameraPosition()
	{
		if (!(_camera == null))
		{
			Vector3 localPosition = _camera.transform.localPosition;
			localPosition.y = cameraYPosition;
			_camera.transform.localPosition = localPosition;
		}
	}

	private void UpdateZoomCameraPosition()
	{
		if (!(_camera == null))
		{
			Vector3 localPosition = _camera.transform.localPosition;
			localPosition.x = cameraXPosition;
			_camera.transform.localPosition = localPosition;
		}
	}

	public void UpdateLegValues(float rightLeg, float bothLegs, float leftLeg)
	{
		_rightLeg.inputValue = rightLeg * -1f;
		_leftLeg.inputValue = leftLeg;
		RealtimeLog.instance._inputPernas = new Vector2(_leftLeg.inputValue, _rightLeg.inputValue);
	}

	public int GetLegsValue()
	{
		return (int)_rightLeg.currentAngle;
	}

	public float[] GetInputValues()
	{
		return new float[2] { _leftLeg.inputValue, _rightLeg.inputValue };
	}

	public float[] GetInputAngles()
	{
		float num = Mathf.Clamp01(((float)Random.Range(0, 65) - _leftLeg.minAngle) / (_leftLeg.maxAngle - _leftLeg.minAngle));
		float num2 = Mathf.Clamp01(((float)Random.Range(0, 65) - _rightLeg.minAngle) / (_rightLeg.maxAngle - _rightLeg.minAngle));
		RealtimeLog.instance._pernaAngles = new Vector2(num, num2);
		return new float[2] { num, num2 };
	}

	public float GetLeftLegAngle()
	{
		if (_leftLeg != null)
		{
			return Mathf.Clamp(_leftLeg.currentAngle, _leftLeg.maxAngle, _leftLeg.minAngle);
		}
		return 0f;
	}

	public float GetRightLegAngle()
	{
		if (_rightLeg != null)
		{
			return Mathf.Clamp(_rightLeg.currentAngle, _rightLeg.maxAngle, _rightLeg.minAngle);
		}
		return 0f;
	}

	public Vector2 GetBothLegsAngles()
	{
		return new Vector2(GetLeftLegAngle(), GetRightLegAngle());
	}

	public void ChangeSkinColor(Material handMat, Material legMat)
	{
		Renderer[] handRenderer = _handRenderer;
		for (int i = 0; i < handRenderer.Length; i++)
		{
			handRenderer[i].material = handMat;
		}
		handRenderer = _legRenderer;
		for (int i = 0; i < handRenderer.Length; i++)
		{
			handRenderer[i].material = legMat;
		}
	}

	public void ChangeGen(float value)
	{
		SkinnedMeshRenderer[] array = new SkinnedMeshRenderer[2];
		SkinnedMeshRenderer[] array2 = new SkinnedMeshRenderer[2];
		for (int i = 0; i < 2; i++)
		{
			array[i] = _pernaNormal[i].GetComponent<SkinnedMeshRenderer>();
			array2[i] = _peProtese[i].GetComponent<SkinnedMeshRenderer>();
		}
		for (int j = 0; j < 2; j++)
		{
			array[j].SetBlendShapeWeight(2, value);
			array2[j].SetBlendShapeWeight(0, value);
		}
	}
}
