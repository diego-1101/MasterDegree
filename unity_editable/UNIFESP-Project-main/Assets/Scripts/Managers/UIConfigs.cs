using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using SimpleFileBrowser;
using System.IO;
using System;
using System.Collections.Generic;
using UnityEngine.Events;

// Classe para armazenar dados de configura��o
[System.Serializable]
public class Data
{
    public bool isDefault; // Se usa configura��es padr�o

    public int trials; // N�mero de trials

    // Experimento 1
    public float ballVelocity; // Velocidade da bola
    public float ballInterval; // Intervalo entre bolas

    public bool isRandomicPos; // Posi��es randomicas
    public float[] positions; // Array de posi��es

    // Experimento 2
    public string pernaEscolhida; // Perna escolhida
    public bool isRandomicAngle; // Angulos randomicos
    public int[] variations; // Varia��es

    public float gravidade; // Configura��o de gravidade

    public float margem; // Margem de erro
    public bool isSplitTasks; // Divis�o de tarefas
    public int totalDiv; // Total de divis�es
    public float[] tempoEspera; // Tempos de espera
}

// Enum para modos de jogo
public enum ModoJogo
{
    PRINCIPAL,
    TREINO
}

// Enum para tipos de UI
public enum UIModo
{
    Um,
    Dois
}

// Classe principal de controle de UI e configura��es
public class UIConfigs : MonoBehaviour
{
    #region SINGLETON

    private static UIConfigs _instance;

    public static UIConfigs instance
    {
        get
        {
            if (_instance == null)
                _instance = FindAnyObjectByType<UIConfigs>();

            return _instance;
        }
    }

    #endregion

    [HideInInspector] public PlayerController playerController;
    [HideInInspector] public StartGame startGame;
    [HideInInspector] public Dictionary<string, List<string>> dataInfos;

    public UIModo _ui;
    [HideInInspector] public ModoJogo _mode;
    public LegType _legType; // Tipo de perna selecionada

    [Space, Header("UI"), Space]
    public GameObject spaceBar; // Indicador da UI da barra de espa�o
    public CanvasGroup painelConfigs; // Grupo de canvas das configura��es
    public GameObject configsUI; // UI de configura��es

    public StatusUIController statusUI; // Controlador de status da UI

    public MenuBtnOperation confirmButton; // Bot�o de confirma��o

    // Campos para configura��o LSL
    public TMP_InputField inputStreamName,
        inputStreamType;

    // Frase de acerto/erro
    private string fraseAcerto,
        fraseErro;

    [Space, Header("Graz"), Space]
    public TMP_InputField _baselineField; // Input de dura��o da baseline
    public TMP_InputField _fixationField; // Input de dura��o da fixa��o
    public TMP_Text _refFixationText; // Texto de refer�ncia da fixa��o
    //public TMP_Text _refCueText;
    public TMP_InputField _feedbackField; // Input de dura��o do feedback
    public TMP_InputField _minPauseField; // Input de pausa m�nima
    public TMP_InputField _maxPauseField; // Input de pausa m�xima
    [SerializeField] private Image colorRefAcerto; // Cor do acerto
    [SerializeField] private Image colorRefErro; // Cor do erro

    // Sistema de configura��es de Cue
    [SerializeField] private RectTransform _inputsTransformCue;
    public List<GameObject> cueConfigs; // Lista de configura��es de cue
    [SerializeField] private GameObject curInputObj; // Prefab do cue
    public int cueCount = 1; // Contador de cues

    [Space, Header("Trials"), Space]
    public TMP_InputField trialsField;
    [HideInInspector] public int trialsCont = 0;
    [HideInInspector] public int trialsAngleCont = 0;

    #region CLIENTE

    [HideInInspector] public MenuBtnOperation actSkin; // Skin ativa

    #endregion

    [Space, Header("Path"), Space]
    [SerializeField] protected TextMeshProUGUI _pathMesh; // Texto do caminho
    private string archiveName; // Nome do arquivo

    [Header("Default"), Space]
    public Toggle isDefault; // Toggle de configura��o padr�o
    [SerializeField] private int defaultTrials = 25; // Trials padr�o

    [HideInInspector] public InputRandomCS inputRandom;
    protected ExperimentalSessionLogger experimentalLogger;
    protected ExperimentalLogSessionConfig experimentalLogConfig;

    public virtual void Awake()
    {
        startGame = FindAnyObjectByType<StartGame>();
        startGame.statusUI = statusUI;
        playerController = FindAnyObjectByType<PlayerController>();
        playerController.StartPlayerController(statusUI);
        playerController.CameraSetup();

        var lsl = FindAnyObjectByType<LSLReceiver>();
        inputStreamName.text = lsl.streamName;
        inputStreamType.text = lsl.streamType;
        EnsureExperimentalLogger();
        ReleaseCursorForUi();
    }

    public virtual void Start()
    {
        spaceBar.SetActive(false);
        startGame.Started = false;
        configsUI.SetActive(false);
        playerController.LegChoice("nenhuma"); // Inicia sem pr�teses
    }

    public virtual void Update()
    {
        ReleaseCursorForUi();

        // Tecla X para voltar ao menu de configura��es
        if (startGame != null)
        {
            if (startGame.Started)
                if (Input.GetKeyDown(KeyCode.X))
                    BackToMenuConfigs();
        }

        // Controla estado do bot�o de confirma��o baseado na skin ativa
        if (actSkin != null)
        {
            confirmButton.enabled = true;
            Image confirmImage = confirmButton.GetComponent<Image>();
            confirmImage.color = new Color32(255, 255, 255, 255);
        }
        else
        {
            confirmButton.enabled = false;
            Image confirmImage = confirmButton.GetComponent<Image>();
            confirmImage.color = new Color32(255, 255, 255, 160);
        }

        // Ajusta altura dos containers de UI
        AdjustContainerHeight(_inputsTransformCue);
        AdjustContainerHeight(_inputsTransformCue.parent.GetComponent<RectTransform>());
    }

    protected void ReleaseCursorForUi()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private PhotoMode photoMode;

    // Define o modo de jogo (treino/principal)
    public void SetMode(string mode) => _mode = mode == "treino" ? ModoJogo.TREINO : ModoJogo.PRINCIPAL;

    // Define qual perna vai usar baseado no dropdown
    public void SetLegType(TMP_Dropdown pernaDropdown)
    {
        var _leg = GetLeg(pernaDropdown);
        playerController.LegChoice(_leg);

        if (_leg.Equals("esquerda"))
            _legType = LegType.LEFT;
        else if (_leg.Equals("direita"))
            _legType = LegType.RIGHT;
        else
            _legType = LegType.BOTH;
    }

    // Obt�m texto da perna selecionada no dropdown
    public string GetLeg(TMP_Dropdown pernaDropdown)
    {
        return pernaDropdown.options[pernaDropdown.value].text.ToLower();
    }

    // Verifica se modo foto est� ativo
    public bool GetPhotoActive()
    {
        if (photoMode == null)
            photoMode = GetComponent<PhotoMode>();
        return photoMode.modeActive;
    }

    // Configura valores padr�o ou limpa campos
    public virtual void SetDefault(bool isDefault)
    {
        if (isDefault)
        {
            trialsField.text = defaultTrials.ToString();
            startGame.SetupDefault = true;
        }
        else
        {
            trialsField.text = "1";
        }
    }

    // Define n�mero de trials
    public virtual void SetTrials(string trials)
    {
        if (string.IsNullOrEmpty(trials))
            return;
        startGame.trials = int.Parse(trials);
    }

    // Adiciona novo input de cue a UI
    public void AddCueInput()
    {
        GameObject input = Instantiate(curInputObj);
        input.transform.parent = _inputsTransformCue;

        var config = input.GetComponent<CueConfigs>();
        config.minusBtn.onClick.AddListener(() =>
        {
            RemoveCue(input);
        });

        config.imageBtn.onClick.AddListener(() =>
        {
            CarregarImagem(config);
        });

        cueConfigs.Add(input);

        cueCount++;
    }

    // Remove cue da UI
    public void RemoveCue(GameObject obj)
    {
        Destroy(obj);

        AdjustContainerHeight(_inputsTransformCue);
        AdjustContainerHeight(_inputsTransformCue.parent.GetComponent<RectTransform>());

        cueConfigs.Remove(obj);

        cueCount--;
    }

    // Ajusta altura dos containers da UI
    public void AdjustContainerHeight(RectTransform rect)
    {
        float totalHeight = 0f;
        int activeChildren = 0;

        // Soma altura de todos os filhos ativos
        foreach (RectTransform child in rect)
        {
            if (!child.gameObject.activeSelf) continue;

            totalHeight += child.sizeDelta.y;
            activeChildren++;
        }

        // Adiciona espa�amento entre filhos
        if (activeChildren > 1)
        {
            totalHeight += (activeChildren - 1);
        }

        rect.sizeDelta = new Vector2(rect.sizeDelta.x, totalHeight);
    }

    // Prepara configura��es
    public virtual void SetarConfigs() 
    {
        painelConfigs.alpha = 0;
    }

    [HideInInspector] public string _path; // Caminho atual
    [HideInInspector] public string _pathFolder; // Pasta selecionada
    [HideInInspector] public string _pathLog; // Caminho do log

    // Formata informa��es de arquivo para dicion�rio
    public Dictionary<string, List<string>> FormatInfos(string path)
    {
        Dictionary<string, string> infos = new Dictionary<string, string>();
        string currentKey = "";
        System.Text.StringBuilder currentText = new System.Text.StringBuilder();

        foreach (string line in File.ReadAllLines(path))
        {
            if (line.StartsWith("[") && line.EndsWith("]"))
            {
                if (currentKey != "")
                {
                    infos[currentKey] = currentText.ToString();
                    currentText.Clear();
                }
                currentKey = line.Trim('[', ']');
            }
            else
            {
                currentText.AppendLine(line);
            }
        }

        if (currentKey != "")
        {
            infos[currentKey] = currentText.ToString();
        }

        Dictionary<string, List<string>> finalInfos = new Dictionary<string, List<string>>();
        foreach (var info in infos)
        {
            StringReader reader = new StringReader(info.Value);

            List<string> linhas = new List<string>();
            string linha;
            while (!string.IsNullOrWhiteSpace((linha = reader.ReadLine())))
                linhas.Add(linha);

            List<string> value = new List<string>();
            
            foreach (var l in linhas)
                value.Add(l);
            
            finalInfos[info.Key] = value;
        }

        return finalInfos;
    }

    // Inicia o jogo
    public virtual void StartGame()
    {
        var lsl = LSLReceiver.instance;
        lsl.streamName = inputStreamName.text;
        lsl.streamType = inputStreamType.text;
        lsl.CallInitialize(this);
    }

    // Configura parametros do protocolo Graz
    public void SetGraz()
    {
        var graz = GrazController.instance;
        if (_mode == ModoJogo.TREINO) graz.ToggleCanvas(true);
        graz.baselineDuration = string.IsNullOrEmpty(_baselineField.text) ? 10f : float.Parse(_baselineField.text);
        graz.fixationDuration = string.IsNullOrEmpty(_fixationField.text) ? 3f : float.Parse(_fixationField.text);
        //graz.cueDuration = string.IsNullOrEmpty(_cueField.text) ? 5f : float.Parse(_cueField.text);
        graz.feedbackDuration = string.IsNullOrEmpty(_feedbackField.text) ? 3.75f : float.Parse(_feedbackField.text);
        graz.minPauseDuration = string.IsNullOrEmpty(_minPauseField.text) ? 1.5f : float.Parse(_minPauseField.text);
        graz.maxPauseDuration = string.IsNullOrEmpty(_maxPauseField.text) ? 3.5f : float.Parse(_maxPauseField.text);
    }

    // Volta para menu de configura��es
    public virtual void BackToMenuConfigs()
    {
        GrazController.instance.EndProtocol();
        painelConfigs.alpha = 1;
    }

    private RealtimeLog _realtime;

    // Inicia sistema de logging em tempo real
    public virtual void StartLogRealtime()
    {
        if (_realtime == null)
            _realtime = FindAnyObjectByType<RealtimeLog>();

        _realtime.StartPath(_pathLog);

        EnsureExperimentalLogger();
        ReleaseCursorForUi();
        if (experimentalLogConfig == null)
            ConfigureExperimentalLogger(BuildDefaultExperimentalLogConfig("legacy_flow"));
        experimentalLogger.BeginSession("StartLogRealtime");
    }

    public void ConfigureExperimentalLogger(ExperimentalLogSessionConfig config)
    {
        EnsureExperimentalLogger();
        ReleaseCursorForUi();
        experimentalLogConfig = config ?? BuildDefaultExperimentalLogConfig("unspecified");
        experimentalLogger.Configure(experimentalLogConfig);
    }

    protected ExperimentalSessionLogger EnsureExperimentalLogger()
    {
        if (experimentalLogger == null)
            experimentalLogger = FindAnyObjectByType<ExperimentalSessionLogger>();
        if (experimentalLogger == null)
            experimentalLogger = gameObject.AddComponent<ExperimentalSessionLogger>();
        return experimentalLogger;
    }

    protected virtual ExperimentalLogSessionConfig BuildDefaultExperimentalLogConfig(string mode)
    {
        var lsl = LSLReceiver.instance;
        var graz = GrazController.instance;
        return new ExperimentalLogSessionConfig
        {
            experimentName = _ui == UIModo.Dois ? "Experiment 2" : "Experiment 1",
            mode = mode,
            subjectId = "S01",
            sessionId = "01",
            run = "1",
            logPath = _pathLog,
            inputStreamName = lsl != null ? lsl.streamName : "",
            inputStreamType = lsl != null ? lsl.streamType : "",
            markerStreamName = LSLMarkerOutlet.Instance != null ? LSLMarkerOutlet.Instance.StreamName : "",
            markerStreamType = LSLMarkerOutlet.Instance != null ? LSLMarkerOutlet.Instance.StreamType : "",
            totalTrials = startGame != null ? startGame.trials : 0,
            baselineDuration = graz != null ? graz.baselineDuration : 0f,
            fixationDuration = graz != null ? graz.fixationDuration : 0f,
            cueDuration = graz != null ? graz.cueDuration : 0f,
            feedbackDuration = graz != null ? graz.feedbackDuration : 0f,
            minPauseDuration = graz != null ? graz.minPauseDuration : 0f,
            maxPauseDuration = graz != null ? graz.maxPauseDuration : 0f
        };
    }

    #region PATH

    // Carrega informa��es de arquivo JSON
    public void CarregarInfos()
    {
        FileBrowser.SetFilters(true, new FileBrowser.Filter("JSON Files", ".json"));
        FileBrowser.SetDefaultFilter(".json");
        StartCoroutine(ShowLoadDialogCoroutine(EndCoroutine));
    }

    [HideInInspector] public Data loadedData;

    // Corrotina para carregar o arquivo
    private IEnumerator ShowLoadDialogCoroutine(Action onComplete)
    {
        yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.Files, false, null, null, "Selecione uma arquivo JSON", "Selecionar");
        if (FileBrowser.Success)
        {
            for (int i = 0; i < FileBrowser.Result.Length; i++)
                _path = FileBrowser.Result[i];

            string jsonContent = File.ReadAllText(_path);
            loadedData = JsonUtility.FromJson<Data>(jsonContent);

            onComplete?.Invoke();
        }
    }

    // Carrega imagem para cue
    public void CarregarImagem(CueConfigs cue)
    {
        FileBrowser.SetFilters(true, new FileBrowser.Filter("Images", ".jpg", ".png", ".jpeg"));
        FileBrowser.SetDefaultFilter(".jpg");
        FileBrowser.SetExcludedExtensions(".lnk", ".tmp", ".zip", ".rar", ".exe");

        StartCoroutine(ShowLoadImageCoroutine(cue));
    }

    // Corrotina para carregar imagem
    private IEnumerator ShowLoadImageCoroutine(CueConfigs cue)
    {
        yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.Files, false, null, null, "Selecione uma imagem", "Carregar");
        if (FileBrowser.Success)
        {
            for (int i = 0; i < FileBrowser.Result.Length; i++)
                _path = FileBrowser.Result[i];

            cue.path = _path; // Define caminho no cue
        }
    }

    // Callback quando carregamento termina
    public virtual void EndCoroutine() { }

    // Seta o nome do arquivo
    public void SetArchiveName(string name) => archiveName = name;

    // Seleciona pasta para salvar dados
    public void SelecionarPath()
    {
        if (string.IsNullOrEmpty(archiveName))
            return;
        FileBrowser.SetFilters(false);
        StartCoroutine(SelectPathCoroutine());
    }

    [HideInInspector] public Dictionary<string, List<string>> pathDataInfos;

    // Corrotina para selecionar pasta
    private IEnumerator SelectPathCoroutine()
    {
        yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.Folders, false, null, null, "Selecione uma Pasta", "Selecionar");
        if (FileBrowser.Success)
        {
            for (int i = 0; i < FileBrowser.Result.Length; i++)
            {
                _pathFolder = OnFilesSelected(FileBrowser.Result);
            }

            string archive = $"{_pathFolder}/{archiveName}.csv";

            _pathMesh.text = archive;
            _pathLog = archive;
        }
    }

    // Processa arquivos selecionados
    private string OnFilesSelected(string[] filePaths)
    {
        string filePath = filePaths[0];

        return filePath;
    }

    #endregion

    // Atualiza frase de feedback de acerto
    public void UpdateFeedbackAcerto(string str) 
    {
        fraseAcerto = str;
    }

    // Atualiza frase de feedback de erro
    public void UpdateFeedbackErro(string str)
    {
        fraseErro = str;
    }

    // Verifica e aplica frases de feedback
    public void CheckFeedbackFrases()
    {
        if (string.IsNullOrEmpty(fraseAcerto))
            fraseAcerto = "Parab�ns";
        if (string.IsNullOrEmpty(fraseErro))
            fraseErro = "Tente novamente";

        var feedback = FeedbackController.instance;
        feedback.SetParabens(fraseAcerto, colorRefAcerto.color);
        feedback.SetErrou(fraseErro, colorRefErro.color);
    }

    // Altera o genero do personagem
    public void ChangeGen(int value)
    {
        PlayerController.instance.ChangeGen(value * 100);
    }
}
