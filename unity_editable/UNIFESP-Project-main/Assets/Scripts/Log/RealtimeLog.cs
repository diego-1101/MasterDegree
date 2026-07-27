using UnityEngine;
using System.IO;
using System.Diagnostics;
using System.Globalization;
using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;

// Classe para representar dados de log
public class LogData
{
    public LogData(string eegTime, string unityTime, string classifier, string rightAngle, string leftAngle, string xPos, string yPos, string zPos, string num, string marcador, string acertos, string classe, string tentativa, string target)
    {
        TempoEEG = eegTime;
        TempoUNITY = unityTime;
        Classifier = classifier;
        AngleLegR = rightAngle;
        AngleLegL = leftAngle;
        xBall = xPos;
        yBall = yPos;
        zBall = zPos;
        Marcadornum = num;
        Marcador = marcador;
        Acertos = acertos;
        Classe = classe;
        Tentativa = tentativa;
    }

    // Propriedades dos dados de log
    public string TempoEEG { get; set; }
    public string TempoUNITY { get; set; }

    public string Classifier { get; set; }

    public string AngleLegR { get; set; }
    public string AngleLegL { get; set; }

    public string xBall { get; set; }
    public string yBall { get; set; }
    public string zBall { get; set; }

    public string Marcadornum { get; set; }
    public string Marcador { get; set; }

    public string Acertos { get; set; }
    public string Classe { get; set; }
    public string Tentativa { get; set; }
    public string Target { get; set; }

    // Conversão implícita para string
    public static implicit operator string(LogData log)
        => $"{log.TempoEEG}|{log.TempoUNITY}|{log.Classifier}|{log.AngleLegR}|{log.AngleLegL}|{log.xBall}|{log.yBall}|{log.zBall}|{log.Marcadornum}|{log.Marcador}|{log.Acertos}|{log.Classe}|{log.Tentativa}|{log.Target}";

    // Conversão implícita de string para LogData
    public static implicit operator LogData(string line)
    {
        var data = line.Split("|");
        
        return new LogData(
            data[0],
            data[1],
            data[2],
            data[3],
            data[4],
            data[5],
            data[6],
            data[7],
            data[8],
            data[9],
            data[10],
            data[11],
            data[12],
            data[13]
            );
    }
}

// Extensão para conversão de string para DateTime
public static class StringExtension
{
    public static DateTime ToDateTime(this string value)
    {
        var data = value.Split("-");
        return new DateTime(int.Parse(data[0]), int.Parse(data[1]), int.Parse(data[2]));
    }
}

// Sistema principal de logging em tempo real
public class RealtimeLog : MonoBehaviour
{
    #region SINGLETON

    private static RealtimeLog _instance;

    public static RealtimeLog instance
    {
        get
        {
            if (_instance == null)
                _instance = FindAnyObjectByType<RealtimeLog>();
            return _instance;
        }
    }

    #endregion

    #region VARIAVEIS

    [HideInInspector] public float _classifier;
    public Vector2 _pernaAngles; // Angulos das pernas
    [HideInInspector] public Vector3 _ballPosition; // Posição da bola
    [HideInInspector] public int _marcadorNum = 1; // Número do marcador
    [HideInInspector] public string _marcador; // Tipo de marcador

    [HideInInspector] public Vector2 _inputPernas; // Input das pernas
    [HideInInspector] public string _collisionTime; // Tempo de colisão

    [HideInInspector] public string _acerto; // Acerto/Erro
    [HideInInspector] public int _classe;
    [HideInInspector] public int _tentativa; // Número da tentativa

    [HideInInspector] public string _target; // Alvo
    
    private float _eegTime; // Tempo EEG
    private float _unityTime; // Tempo Unity

    #endregion

    public string _filePath; // Caminho do arquivo
    private StreamWriter _writer; // Escritor do arquivo
    private FileStream _fileStream;

    public float _updateTime = .1f; // Intervalo de atualização
    private float _nextUpdateTime; // Próxima atualização

    #region TAMANHO COLUNAS

    private const int ballPosWidth = 22;
    private const int inputWidth = 15;
    private const int timeColisionWidth = 17;
    private const int acertoWidth = 15;

    private const int timeWidth = 15;

    private const int angleWidth = 15;
    private const int gravityWidth = 15;

    #endregion

    private Process notepadProcess;
    private bool isStarted; // Flag de inicialização

    [SerializeField] private bool InRealtime; // Se está em tempo real

    public List<LogData> logs = new List<LogData>(); // Lista de logs

    public enum ExperienciaChoice
    {
        Um,
        Dois
    }

    [HideInInspector] public ExperienciaChoice experienciaChoice;

    private void Start()
    {
        // Adiciona header inicial
        var log = new LogData("TempoEEG", "TempoUNITY", "Classifier", "AngleLegR", "AngleLegL", "xBall", "yBall", "zBall", "Marcadornum", "Marcador", "Acertos", "Classe", "Tentativa", "Target");
        logs.Add(log);
    }

    private void Update()
    {
        _eegTime = Time.time;

        if (!isStarted || _writer == null) return;

        if (!InRealtime)
            return;

        _unityTime += Time.deltaTime;

        string line;

        // Atualiza em intervalos regulares
        if (Time.time - _nextUpdateTime > _updateTime)
        {
            // Cria novo log com dados atuais
            var log = new LogData(
                _eegTime.ToString(),
                _unityTime.ToString(),
                _inputPernas.ToString(),
                _pernaAngles.y.ToString(),
                _pernaAngles.x.ToString(),
                _ballPosition.x.ToString(),
                _ballPosition.y.ToString(),
                _ballPosition.z.ToString(),
                _marcadorNum.ToString(),
                _marcador,
                _acerto,
                _classe.ToString(),
                _tentativa.ToString(),
                _target);

            logs.Add(log);

            // Formata linha para arquivo
            line = $"{_eegTime.ToString("F3").PadLeft(timeWidth)} " +
            $"{_unityTime.ToString("F3").PadLeft(timeWidth)} " +
            $"{_inputPernas.ToString("F3").PadLeft(inputWidth)} " +
            $"{_pernaAngles.y.ToString("F3").PadLeft(timeWidth)} " +
            $"{_pernaAngles.x.ToString("F3").PadLeft(acertoWidth)} " +
            $"{_ballPosition.x.ToString("F3").PadLeft(acertoWidth)} " +
            $"{_ballPosition.y.ToString("F3").PadLeft(acertoWidth)} " +
            $"{_ballPosition.z.ToString("F3").PadLeft(acertoWidth)} " +
            $"{_marcadorNum.ToString("F3").PadLeft(acertoWidth)} " +
            $"{_marcador.PadLeft(acertoWidth)} " +
            $"{_acerto.PadLeft(acertoWidth)} " +
            $"{_classe.ToString("F3").PadLeft(acertoWidth)} " +
            $"{_tentativa.ToString("F3").PadLeft(acertoWidth)} " +
            $"{_target.PadLeft(acertoWidth)}\n";

            _writer.Write(line);
            _writer.Flush();

            _nextUpdateTime = Time.time;
        }
    }

    #region SETTERS



    #endregion

    // Inicia sistema de logging
    public void StartPath(string path)
    {
        if (!InRealtime)
            return;

        _filePath = path;

        _writer = new StreamWriter(_filePath, false);

        // Escreve header do arquivo
        string header;

        header = $"{ "TempoEEG",-ballPosWidth} " +
                $"{ "TempoUNITY",-inputWidth} " +
                $"{ "Classifier",-timeWidth} " +
                $"{ "AngleLegR",-acertoWidth} " +
                $"{ "AngleLegL",-timeColisionWidth} " +
                $"{ "xBall",-timeColisionWidth} " +
                $"{ "yBall",-timeColisionWidth} " +
                $"{ "zBall",-timeColisionWidth} " +
                $"{ "Marcadornum",-timeColisionWidth} " +
                $"{ "Marcador",-timeColisionWidth} " +
                $"{ "Acertos",-timeColisionWidth} " +
                $"{ "Classe",-timeColisionWidth} " +
                $"{ "Tentativa",-timeColisionWidth} " +
                $"{ "Target",-timeColisionWidth}\n";

        _writer.Write(header);
        _writer.Flush();

        isStarted = true;

        OpenLogFile();
    }

    // Abre arquivo de log automaticamente
    private void OpenLogFile()
    {
        if (File.Exists(_filePath))
        {
            try
            {
                notepadProcess = Process.Start("notepad++.exe", _filePath);
                UnityEngine.Debug.Log("Arquivo de log aberto automaticamente");
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError("Erro ao abrir arquivo: " + e.Message);
            }
        }
    }

    // Finaliza sistema de logging
    public void EndLog()
    {
        if (!InRealtime)
            return;

        if (_writer != null)
        {
            //File.Delete(_filePath);
            _writer.Close();
            _writer.Dispose();
            _writer = null;

            File.Delete(_filePath);

            // Reescreve arquivo com todos os logs formatados
            var data = logs.Select(log => (string)log).ToList();
            File.WriteAllLines(_filePath, data);
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

            var data = logs.Select(log => (string)log).ToList();
            File.WriteAllLines(_filePath, data);
        }
    }
}
