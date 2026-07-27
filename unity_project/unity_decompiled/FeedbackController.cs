using System.Collections;
using TMPro;
using UnityEngine;

public class FeedbackController : MonoBehaviour
{
	private static FeedbackController _instance;

	[SerializeField]
	private Animator parabensAnim;

	[SerializeField]
	private Animator errouAnim;

	[SerializeField]
	private TMP_Text parabensText;

	[SerializeField]
	private TMP_Text errouText;

	public static FeedbackController instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = Object.FindAnyObjectByType<FeedbackController>();
			}
			return _instance;
		}
	}

	public void PlayParabens()
	{
		StartCoroutine(InAnimator(parabensAnim));
	}

	public void PlayErrou()
	{
		StartCoroutine(InAnimator(errouAnim));
	}

	public void SetParabens(string str, Color col)
	{
		parabensText.text = str;
		parabensText.color = col;
	}

	public void SetErrou(string str, Color col)
	{
		errouText.text = str;
		errouText.color = col;
	}

	private IEnumerator InAnimator(Animator anim)
	{
		anim.gameObject.SetActive(value: true);
		anim.Play("InFeedback");
		yield return new WaitForSeconds(5f);
		anim.Play("OutFeedback");
		yield return new WaitForSeconds(0.5f);
		anim.gameObject.SetActive(value: false);
	}
}
