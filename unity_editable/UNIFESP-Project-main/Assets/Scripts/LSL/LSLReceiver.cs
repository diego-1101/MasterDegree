using UnityEngine;
using TMPro;
using LSL;
using System.Collections;

public class LSLReceiver : MonoBehaviour
{
    private static LSLReceiver _instance;
    public static LSLReceiver instance 
    {
        get
        {
            if (_instance == null)
                _instance = FindAnyObjectByType<LSLReceiver>();
            return _instance;
        }
    }

    [Header("Configura��es LSL")]
    public string streamName = "Python_EEG"; // Nome do stream LSL
    public string streamType = "EEG"; // Tipo do stream

    private StreamInfo[] streamInfos; // Informa��es do stream
    private StreamInlet inlet; // Inlet para receber dados
    private float[] sample; // Amostra atual
    private int channelCount; // N�mero de canais
    private float sampleRate; // Taxa de amostragem

    [Space, Header("Painel")]
    public TMP_Text debugConection; // Texto de debug da conex�o
    private Animator debugAnimator; // Animator do painel de debug

    private void Start()
    {
        debugAnimator = debugConection.GetComponentInParent<Animator>();
    }

    // Inicializa conex�o LSL
    public void CallInitialize(UIConfigs configs)
    {
        StartCoroutine(Initialize(configs));
    }

    // Corrotina de inicializa��o
    private IEnumerator Initialize(UIConfigs configs)
    {
        debugAnimator.Play("Show_debug");

        yield return StartCoroutine(ResolveStream());

        if (streamInfos != null)
        {
            inlet = new StreamInlet(streamInfos[0]);
            channelCount = inlet.info().channel_count();
            sampleRate = (float)inlet.info().nominal_srate();
            sample = new float[channelCount];

            debugConection.text = $"Conectado a {streamName} - {channelCount} canais a {sampleRate}Hz";
            debugConection.color = Color.green;
        }
        else
        {
            // Bypass para permitir testes sem LSL conectado
            debugConection.text = "LSL no encontrado. Entrando em modo offline...";
            debugConection.color = Color.yellow;
        }

        // Chama SetarConfigs independentemente do LSL para liberar a interface
        configs.SetarConfigs();

        yield return new WaitForSeconds(1.5f);
        debugAnimator.Play("Hide_debug");
    }

    // Corrotina para encontrar stream LSL
    private IEnumerator ResolveStream()
    {
        streamInfos = null;
        float timeout = 5f; // TODO: Timeout
        float startTime = Time.time;

        debugConection.text = $"Procurando stream {streamName}...";
        debugConection.color = new Color32(207, 125, 0, 255);

        // Loop de busca do stream
        while (streamInfos == null || streamInfos.Length == 0)
        {
            streamInfos = LSL.LSL.resolve_stream("name", streamName, 1, 1.0);

            // Verifica timeout
            if (Time.time - startTime > timeout)
            {
                streamInfos = null;
                debugConection.text = "Stream n�o encontrado dentro do tempo limite!";
                debugConection.color = Color.red;

                yield return new WaitForSeconds(1f);
                debugAnimator.Play("Hide_debug");

                yield break;
            }

            yield return new WaitForSeconds(0.5f);
        }
    }

    void Update()
    {
        // Puxa amostras do stream LSL
        if (inlet != null)
        {
            double timestamp = inlet.pull_sample(sample, 0.0f);
            if (timestamp != 0.0)
            {
                var experimentalLogger = ExperimentalSessionLogger.Instance;
                if (experimentalLogger != null)
                    experimentalLogger.RegisterClassifierSample(sample, timestamp);
                ProcessData(sample);
            }
        }
    }

    public PlayerController _player;

    // Processa dados EEG recebidos
    void ProcessData(float[] data)
    {
        if (!StartGame.instance.GetStats()) return;
        _player.UpdateLegValues(data[0], data[1], data[2]);
    }

    // Fecha conex�o LSL
    public void StopInlet()
    {
        if (inlet != null)
            inlet.close_stream();
    }
}
