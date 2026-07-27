using System;
using Eflatun.SceneReference;

[Serializable]
public class SceneData
{
	public SceneReference sceneReference;

	public SceneType sceneType;

	public string _name => sceneReference.Name;
}
