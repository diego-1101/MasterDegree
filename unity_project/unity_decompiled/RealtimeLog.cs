using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEngine;

public class RealtimeLog : MonoBehaviour
{
	public enum ExperienciaChoice
	{
		Um,
		Dois
	}

	private static RealtimeLog _instance;

	[HideInInspector]
	public float _classifier;

	public Vector2 _pernaAngles;

	[HideInInspector]
	public Vector3 _ballPosition;

	[HideInInspector]
	public int _marcadorNum = 1;

	[HideInInspector]
	public string _marcador;

	[HideInInspector]
	public Vector2 _inputPernas;

	[HideInInspector]
	public string _collisionTime;

	[HideInInspector]
	public string _acerto;

	[HideInInspector]
	public int _classe;

	[HideInInspector]
	public int _tentativa;

	[HideInInspector]
	public string _target;

	private float _eegTime;

	private float _unityTime;

	public string _filePath;

	private StreamWriter _writer;

	private FileStream _fileStream;

	public float _updateTime = 0.1f;

	private float _nextUpdateTime;

	private const int ballPosWidth = 22;

	private const int inputWidth = 15;

	private const int timeColisionWidth = 17;

	private const int acertoWidth = 15;

	private const int timeWidth = 15;

	private const int angleWidth = 15;

	private const int gravityWidth = 15;

	private Process notepadProcess;

	private bool isStarted;

	[SerializeField]
	private bool InRealtime;

	public List<LogData> logs = new List<LogData>();

	[HideInInspector]
	public ExperienciaChoice experienciaChoice;

	public static RealtimeLog instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = UnityEngine.Object.FindAnyObjectByType<RealtimeLog>();
			}
			return _instance;
		}
	}

	private void Start()
	{
		LogData item = new LogData("TempoEEG", "TempoUNITY", "Classifier", "AngleLegR", "AngleLegL", "xBall", "yBall", "zBall", "Marcadornum", "Marcador", "Acertos", "Classe", "Tentativa", "Target");
		logs.Add(item);
	}

	private void Update()
	{
		_eegTime = Time.time;
		if (isStarted && _writer != null && InRealtime)
		{
			_unityTime += Time.deltaTime;
			if (Time.time - _nextUpdateTime > _updateTime)
			{
				LogData item = new LogData(_eegTime.ToString(), _unityTime.ToString(), _inputPernas.ToString(), _pernaAngles.y.ToString(), _pernaAngles.x.ToString(), _ballPosition.x.ToString(), _ballPosition.y.ToString(), _ballPosition.z.ToString(), _marcadorNum.ToString(), _marcador, _acerto, _classe.ToString(), _tentativa.ToString(), _target);
				logs.Add(item);
				string value = _eegTime.ToString("F3").PadLeft(15) + " " + _unityTime.ToString("F3").PadLeft(15) + " " + _inputPernas.ToString("F3").PadLeft(15) + " " + _pernaAngles.y.ToString("F3").PadLeft(15) + " " + _pernaAngles.x.ToString("F3").PadLeft(15) + " " + _ballPosition.x.ToString("F3").PadLeft(15) + " " + _ballPosition.y.ToString("F3").PadLeft(15) + " " + _ballPosition.z.ToString("F3").PadLeft(15) + " " + _marcadorNum.ToString("F3").PadLeft(15) + " " + _marcador.PadLeft(15) + " " + _acerto.PadLeft(15) + " " + _classe.ToString("F3").PadLeft(15) + " " + _tentativa.ToString("F3").PadLeft(15) + " " + _target.PadLeft(15) + "\n";
				_writer.Write(value);
				_writer.Flush();
				_nextUpdateTime = Time.time;
			}
		}
	}

	public void StartPath(string path)
	{
		if (InRealtime)
		{
			_filePath = path;
			_writer = new StreamWriter(_filePath, append: false);
			string value = string.Format("{0,-22} ", "TempoEEG") + string.Format("{0,-15} ", "TempoUNITY") + string.Format("{0,-15} ", "Classifier") + string.Format("{0,-15} ", "AngleLegR") + string.Format("{0,-17} ", "AngleLegL") + string.Format("{0,-17} ", "xBall") + string.Format("{0,-17} ", "yBall") + string.Format("{0,-17} ", "zBall") + string.Format("{0,-17} ", "Marcadornum") + string.Format("{0,-17} ", "Marcador") + string.Format("{0,-17} ", "Acertos") + string.Format("{0,-17} ", "Classe") + string.Format("{0,-17} ", "Tentativa") + string.Format("{0,-17}\n", "Target");
			_writer.Write(value);
			_writer.Flush();
			isStarted = true;
			OpenLogFile();
		}
	}

	private void OpenLogFile()
	{
		if (File.Exists(_filePath))
		{
			try
			{
				notepadProcess = Process.Start("notepad++.exe", _filePath);
				UnityEngine.Debug.Log("Arquivo de log aberto automaticamente");
			}
			catch (Exception ex)
			{
				UnityEngine.Debug.LogError("Erro ao abrir arquivo: " + ex.Message);
			}
		}
	}

	private void LogTime()
	{
		string text = $"{Time.time:F2};{_pernaAngles:F2}";
		_writer.WriteLine(text);
		_writer.Flush();
		File.AppendAllText(_filePath, text);
	}

	public void EndLog()
	{
		if (!InRealtime)
		{
			return;
		}
		if (_writer != null)
		{
			_writer.Close();
			_writer.Dispose();
			_writer = null;
			File.Delete(_filePath);
			List<string> contents = ((IEnumerable<LogData>)logs).Select((Func<LogData, string>)((LogData log) => log)).ToList();
			File.WriteAllLines(_filePath, contents);
		}
		_unityTime = 0f;
	}

	private void OnApplicationQuit()
	{
		if (_writer != null)
		{
			_writer.Close();
			_writer.Dispose();
			File.Delete(_filePath);
			List<string> contents = ((IEnumerable<LogData>)logs).Select((Func<LogData, string>)((LogData log) => log)).ToList();
			File.WriteAllLines(_filePath, contents);
		}
	}
}
