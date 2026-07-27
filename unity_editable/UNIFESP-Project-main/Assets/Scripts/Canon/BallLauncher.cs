using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallLauncher : MonoBehaviour
{
    #region SINGLETON

    private static BallLauncher _instance;

    public static BallLauncher instance
    {
        get
        {
            if (_instance == null)
                _instance = FindAnyObjectByType<BallLauncher>();
            return _instance;
        }
    }

    #endregion

    #region TRANSFORMS

    private Transform origin; // Origem do lançamento
    [SerializeField] private Transform[] targets; // Array com os alvos
    private Transform currentTarget; // Alvo atual selecionado

    #endregion

    #region NOISE

    private Vector2[] noises; // Array de ruídos para variação de trajetória
    private int noiseIndex; // Índice atual do ruído

    #endregion

    [Header("Configurações")]
    public GameObject ballPrefab; // Prefab da bola a ser lançada
    public int resolution = 30; // Resolução da linha de trajetória
    public bool isRendEnable = true; // Controle de render da trajetória (se a linha aparece ou não)

    private float ballSpeed = 1f; // Velocidade da bola
    private LineRenderer trajectoryLine; // Renderer da linha de trajetória
    private float currentHorizontalPeak; // Pico horizontal atual da trajetória
    private float currentHeight; // Altura atual da trajetória
    private Coroutine ballCoroutine; // Corrotina do movimento da bola
    [HideInInspector] public bool isLaunching = false; // Flag de controle de lançamento

    private void Start()
    {
        origin = transform;
        trajectoryLine = GetComponent<LineRenderer>();
    }

    void Update()
    {
        // Só atualiza se o jogo estiver ativo
        if (!StartGame.instance.GetStats()) return;

        // Atualiza a visualização da trajetória
        UpdateTrajectoryVisualization();

        // Sincroniza o estado de renderização com a configuração
        if (isRendEnable != trajectoryLine.enabled)
            trajectoryLine.enabled = isRendEnable;
    }

    // Configura os parametros do lançador
    public void SetParameters(float _moveSpeed, Vector2[] _noise)
    {
        ballSpeed = _moveSpeed;
        noises = _noise;
    }

    // Lança uma única bola
    public GameObject LaunchSingleBall()
    {
        // Seleciona aleatoriamente um alvo
        var option = Random.Range(0, targets.Length - 1);

        var graz = GrazController.instance;

        // Configura a direção do Cue e atualiza o log baseado no alvo
        switch (option)
        {
            case 0:
                graz.SetCueImageDirection(180f);
                RealtimeLog.instance._target = "ESQUERDA";
                break;
            case 1:
                graz.SetCueImageDirection(-90f);
                RealtimeLog.instance._target = "CENTRO";
                break;
            case 2:
                graz.SetCueImageDirection(0f);
                RealtimeLog.instance._target = "DIREITA";
                break;
            case 3:
                graz.SetCueImageDirection(90f);
                RealtimeLog.instance._target = "FORA";
                break;
        }

        currentTarget = targets[option];
        GeneratePeak(); // Gera picos de trajetória

        // Instancia e move a bola
        GameObject ball = Instantiate(ballPrefab, origin.position, Quaternion.identity);
        ballCoroutine = StartCoroutine(MoveBallCoroutine(ball));

        return ball;
    }

    // Seta os valores da trajetória baseado nos ruídos
    private void GeneratePeak()
    {
        if (noises != null && noises.Length > 0)
        {
            currentHorizontalPeak = noises[noiseIndex].x;
            currentHeight = noises[noiseIndex].y;
            noiseIndex = (noiseIndex + 1) % noises.Length;
        }
    }

    // Atualiza a visualização da linha de trajetória
    private void UpdateTrajectoryVisualization()
    {
        if (origin == null || currentTarget == null) return;

        // Calcula pontos da trajetória
        Vector3[] points = new Vector3[resolution];
        for (int i = 0; i < resolution; i++)
        {
            float t = (float)i / (resolution - 1);
            points[i] = CalculateBallPosition(t); // Calcula posição
        }

        // Aplica pontos no LineRenderer
        trajectoryLine.positionCount = resolution;
        trajectoryLine.SetPositions(points);
    }

    // Calcula a posição da bola em um ponto específico da trajetória
    public Vector3 CalculateBallPosition(float t)
    {
        // Interpolação entre a origem e o alvo
        Vector3 basePos = Vector3.Lerp(origin.position, currentTarget.position, t);

        // Calcula a direção principal e perpendicular para a curva
        Vector3 direction = (currentTarget.position - origin.position).normalized;
        Vector3 perpendicular = Vector3.Cross(Vector3.up, direction).normalized;

        float parabola = 4 * currentHeight * t * (1 - t);

        float horizontalEffect = Mathf.Sin(t * Mathf.PI) * currentHorizontalPeak;

        return basePos + perpendicular * horizontalEffect + Vector3.up * parabola;
    }

    private IEnumerator MoveBallCoroutine(GameObject ball)
    {
        float distance = Vector3.Distance(origin.position, currentTarget.position);

        // Ajusta velocidade baseado no protocolo Graz se ativo
        if (GrazController.instance.inGrazProtocol)
            ballSpeed = distance / (GrazController.instance.cueDuration + 1);

        // Tempo total de movimento
        float duration = distance / ballSpeed;
        float elapsed = 0f;

        // Adiciona controller a bola
        BallController controller = ball.AddComponent<BallController>();
        controller.SetCoroutine(ballCoroutine);
        controller.origin = origin;
        controller.currentTarget = currentTarget;
        controller.currentHeight = currentHeight;
        controller.currentHorizontalPeak = currentHorizontalPeak;
        controller.ballSpeed = ballSpeed;

        // Desativa física durante movimento controlado
        controller.DisablePhysics();

        // Loop de movimento suave
        while (elapsed < duration && ball != null && !controller.hasCollided)
        {
            float t = elapsed / duration;
            ball.transform.position = controller.CalculateBallPosition(t);

            // Rotaciona a bola na direção do movimento
            if (t > 0.1f && t < 0.9f)
            {
                Vector3 lookDir = controller.CalculateBallPosition(t + 0.05f) - ball.transform.position;
                ball.transform.rotation = Quaternion.LookRotation(lookDir);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Reativa física ao final do movimento
        controller.EnablePhysics();
        controller.ApplyFinalVelocity();

        // Desativa linha de trajetória se não estiver mais lançando
        if (!isLaunching)
            trajectoryLine.enabled = false;
    }
}

// Classe para controlar o comportamento individual de cada bola
public class BallController : MonoBehaviour
{
    private Rigidbody rb;
    public bool hasReached = false; // Flag se atingiu o alvo
    public bool hasCollided = false; // Flag se colidiu com algo
    private Coroutine ballCoroutine; // Referência a corrotina de movimento

    // Parametros de trajetória
    public int targetCont;
    public Transform origin;
    public Transform currentTarget;
    public float currentHeight;
    public float currentHorizontalPeak;
    public float ballSpeed;

    RealtimeLog log; // Referência para sistema de logs

    // Setters
    public void SetCoroutine(Coroutine ball) => ballCoroutine = ball;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        log = RealtimeLog.instance;
    }

    private void Update()
    {
        log._ballPosition = transform.position; // Atualiza log de posição

        if (!hasReached && !rb.isKinematic && Vector3.Distance(transform.position, currentTarget.position) < .5f)
        {
            hasReached = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            StartCoroutine(DestroyDelay(2f)); // Destroi após delay
        }
    }

    // Desativa física para movimento controlado
    public void DisablePhysics()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    // Reativa física para comportamento realista
    public void EnablePhysics()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.useGravity = true;
    }

    // Aplica velocidade final em direção ao alvo
    public void ApplyFinalVelocity()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();
        var direction = (currentTarget.position - transform.position).normalized;
        rb.linearVelocity = direction * ballSpeed;
    }

    // Calcula posição da bola (mesma lógica do BallLauncher)
    public Vector3 CalculateBallPosition(float t)
    {
        Vector3 basePos = Vector3.Lerp(origin.position, currentTarget.position, t);

        Vector3 direction = (currentTarget.position - origin.position).normalized;
        Vector3 perpendicular = Vector3.Cross(Vector3.up, direction).normalized;

        float parabola = 4 * currentHeight * t * (1 - t);

        float horizontalEffect = Mathf.Sin(t * Mathf.PI) * currentHorizontalPeak;

        return basePos + perpendicular * horizontalEffect + Vector3.up * parabola;
    }

    // Destroi a bola após delay
    private IEnumerator DestroyDelay(float _delay)
    {
        yield return new WaitForSeconds(_delay);
        Destroy(gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Só processa colisão se for com chão ou jogador
        if (collision.gameObject.tag == "Ground" || collision.gameObject.tag == "Player")
        {
            if (!hasCollided)
            {
                hasCollided = true;

                // Para corrotina de movimento se existir
                if (ballCoroutine != null) 
                    StopCoroutine(ballCoroutine);
                StartCoroutine(DestroyDelay(5f)); // Timer para destruir a bola

                // Registra tempo da colisão
                System.DateTime now = System.DateTime.Now;
                string timeString = now.ToString("HH:mm:ss");
                RealtimeLog.instance._collisionTime = timeString;

                // Processa acerto/erro
                if (collision.gameObject.tag == "Player")
                {
                    RealtimeLog.instance._acerto = "ACERTOU";
                    FeedbackController.instance.PlayParabens();
                }
                else
                {
                    RealtimeLog.instance._acerto = "ERROU";
                    FeedbackController.instance.PlayErrou();
                }

                EnablePhysics(); // Garante que física está ativa após colisão
            }
        }
    }
}