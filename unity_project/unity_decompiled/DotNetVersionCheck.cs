using System;
using UnityEngine;

public class DotNetVersionCheck : MonoBehaviour
{
	private void Start()
	{
		Debug.Log($"Versão .NET em uso: {Environment.Version}");
		Debug.Log("Versão Unity: " + Application.unityVersion);
		try
		{
			Debug.Log((Type.GetType("Mono.Runtime") != null) ? "Mono está disponível" : "Mono não está disponível");
		}
		catch
		{
			Debug.Log("Mono não detectado");
		}
	}
}
