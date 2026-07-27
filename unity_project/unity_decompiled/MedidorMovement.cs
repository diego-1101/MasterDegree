using UnityEngine;

public class MedidorMovement : MonoBehaviour
{
	public RectTransform _marker;

	public RectTransform _markerAngle;

	[HideInInspector]
	public float _tempoPrecisao;

	[HideInInspector]
	public float positionMinYM;

	[HideInInspector]
	public float positionMaxYM;

	[HideInInspector]
	public float positionMinYMM;

	[HideInInspector]
	public float positionMaxYMM;

	public Medidor _medidor;

	public bool entrouOver;

	private bool _waitingForMinimum;

	public float _delayMaxTime = 3f;

	private float _delayingTime;

	private bool _inDelay;

	private int contTempo;

	private float _overlappingTime;

	private bool _wasOverlapping;

	public float _heightMedidor => GetComponent<RectTransform>().rect.height;

	public float _sizeMarker => _marker.rect.height;

	public float _sizeMarkerAngle => _markerAngle.rect.height;

	private void Start()
	{
		_medidor = GetComponentInParent<Medidor>();
	}

	private void Update()
	{
		if (!StartGame.instance || !StartGame.instance.GetStats())
		{
			contTempo = 0;
		}
		else
		{
			if (!GrazController.instance.OnFeedbackPhase)
			{
				return;
			}
			if (_inDelay)
			{
				UpdatePosition(_marker, positionMinYM, positionMaxYM, 0f);
				_delayingTime += Time.deltaTime;
				if (_delayingTime >= _delayMaxTime)
				{
					_inDelay = false;
					_delayingTime = 0f;
				}
				return;
			}
			UpdatePosition(_marker, positionMinYM, positionMaxYM, _medidor.inputValue);
			bool flag = IsOverlapping();
			if (flag && !_wasOverlapping)
			{
				_overlappingTime = 0f;
			}
			if (flag)
			{
				entrouOver = true;
				if (!_waitingForMinimum)
				{
					_overlappingTime += Time.deltaTime;
				}
				if (_overlappingTime >= _tempoPrecisao)
				{
					_waitingForMinimum = true;
					ConfirmOverlapping();
					entrouOver = false;
					FeedbackController.instance.PlayParabens();
					RealtimeLog.instance._acerto = "ACERTOU";
				}
			}
			else if (entrouOver)
			{
				ConfirmOverlapping();
				entrouOver = false;
				FeedbackController.instance.PlayErrou();
				RealtimeLog.instance._acerto = "ERROU";
			}
			else
			{
				_waitingForMinimum = false;
			}
			_wasOverlapping = flag;
		}
	}

	public void ConfirmOverlapping()
	{
		UpdatePosition(_markerAngle, positionMinYMM, positionMaxYMM, GameUIDois.Instance._anguloPerna[_medidor.startGame.actTrials] / 180f);
		int count = GameUIDois.Instance._temposPrecisao.Count;
		contTempo = ((contTempo < count) ? (contTempo + 1) : 0);
		_tempoPrecisao = ((GameUIDois.Instance._temposPrecisao.Count > 1) ? GameUIDois.Instance._temposPrecisao[contTempo] : GameUIDois.Instance._temposPrecisao[0]);
		_overlappingTime = 0f;
		_waitingForMinimum = false;
		_inDelay = true;
	}

	public void UpdatePosition(RectTransform rt, float positionMin, float positionMax, float value)
	{
		float y = Mathf.Lerp(positionMin, positionMax, value);
		rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, y);
	}

	public void MarkerPosition()
	{
		positionMinYM = (0f - (_heightMedidor - _sizeMarker)) / 2f;
		positionMaxYM = (_heightMedidor - _sizeMarker) / 2f;
		positionMinYMM = (0f - (_heightMedidor - _sizeMarkerAngle)) / 2f;
		positionMaxYMM = (_heightMedidor - _sizeMarkerAngle) / 2f;
		UpdatePosition(_marker, positionMinYM, positionMaxYM, 0f);
		UpdatePosition(_markerAngle, positionMinYMM, positionMaxYMM, 0f);
	}

	public void SetMargemAcerto(float height)
	{
		_markerAngle.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
		MarkerPosition();
	}

	private bool IsOverlapping()
	{
		Rect rectScreenSpace = GetRectScreenSpace(_marker);
		Rect rectScreenSpace2 = GetRectScreenSpace(_markerAngle);
		float num = 5f;
		return new Rect(rectScreenSpace.x - num, rectScreenSpace.y - num, rectScreenSpace.width + 2f * num, rectScreenSpace.height + 2f * num).Overlaps(rectScreenSpace2);
	}

	private Rect GetRectScreenSpace(RectTransform rt)
	{
		Vector3[] array = new Vector3[4];
		rt.GetWorldCorners(array);
		Vector2 vector = RectTransformUtility.WorldToScreenPoint(Camera.main, array[0]);
		Vector2 vector2 = RectTransformUtility.WorldToScreenPoint(Camera.main, array[2]);
		return new Rect(vector.x, vector.y, vector2.x - vector.x, vector2.y - vector.y);
	}
}
