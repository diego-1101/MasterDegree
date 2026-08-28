using UnityEngine;

// Script que controla as pernas do jogador
public class RaiseLeg : MonoBehaviour
{
    [Header("Environment Variables")]
    [Range(0f, 1f)] public float inputValue = 0f; // Valor de input
    public float minAngle = 65f; // Angulo mínimo
    public float maxAngle = 0f; // Angulo máximo
    public float legForce = 300f; // Força da perna
    public float smoothVelocity; // Velocidade suavizada

    public float maxTorque = 50f; // Torque máximo
    public float smoothTime = 0.1f; // Tempo de suavização

    [Space, Header("Física da Perna")]
    public Transform _bone; // Osso da perna
    public Rigidbody hipRb; // Rigidbody da perna

    public float currentAngle = 0f; // Angulo atual

    private void Update()
    {
        float inputValue = 0f;

        if (UIConfigs.instance == null)
            return;

        if (GrazController.instance.OnFeedbackPhase || UIConfigs.instance.GetPhotoActive() || FreeLslFamiliarizationController.IsAnyRunning)
            inputValue = this.inputValue;
        else
            inputValue = 0;

        // Calcula angulo atual
        currentAngle = _bone.localEulerAngles.x;
        if (currentAngle > 180f) 
            currentAngle -= 360f;

        // Aplica torque se houver input significativo
        if (inputValue > .1f)
        {
            float targetAngle = Mathf.Lerp(minAngle, maxAngle, inputValue);

            // Aplica torque para atingir o angulo alvo
            float torque = (targetAngle - currentAngle) * legForce * Time.fixedDeltaTime;
            hipRb.AddTorque(_bone.right * torque, ForceMode.VelocityChange);
        }
    }
}
