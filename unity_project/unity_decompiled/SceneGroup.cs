using System;
using System.Collections.Generic;
using System.Linq;

[Serializable]
public class SceneGroup
{
	public string groupName = "New Scene Group";

	public List<SceneData> data;

	public string FindSceneNameByType(SceneType sceneType)
	{
		return data.FirstOrDefault((SceneData scene) => scene.sceneType == sceneType)?.sceneReference.Name;
	}
}
