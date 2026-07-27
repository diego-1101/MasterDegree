using UnityEngine;
using UnityEngine.Playables;

[RequireComponent(typeof(PlayableDirector))]
public class TimelineLooper : MonoBehaviour
{
	public float LoopPoint;

	private PlayableDirector m_PlayableDirector;

	private void Start()
	{
		m_PlayableDirector = GetComponent<PlayableDirector>();
	}

	private void Update()
	{
		if (m_PlayableDirector.state == PlayState.Paused)
		{
			m_PlayableDirector.time = LoopPoint;
			m_PlayableDirector.Play();
		}
	}
}
