using UnityEngine;
using UnityEngine.InputSystem; 
using TMPro;
using UnityEngine.UI;
using System.Collections;
using DialogueSystem;
using UnityEngine.SceneManagement;

public class RuletaPremios : MonoBehaviour
{
    private const int MaxTiros = 4;
    private const string RevanchaKey = "RuletaGame_Revancha";

    [Header("Número de la Suerte (Sistema Externo)")]
    public int numeroDeLaSuerte = 0;

    [Header("UI Instrucciones")]
    public GameObject panelInstrucciones;
    public GameObject botonIniciar;
    [SerializeField] private DialogueManager dialogueManager;
    [SerializeField] private DialogueSequence winSequence;
    [SerializeField] private DialogueSequence loseSequence;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TextMeshProUGUI gameOverText;
    [Min(1)] [SerializeField] private int puntuacionObjetivo = 25;

    [Header("Configuración Visual")]
    public Transform ruedaTransform; 
    public string[] premios; 

    [Header("Ajustes de Giro")]
    public float tiempoGiro = 4f;
    public int vueltasMinimas = 5;
    public float ajusteAngulo = 0f; 

    [Header("UI Principal")]
    public TextMeshProUGUI textoResultado;
    public Button botonGirar;

    [Header("Audio")]
    public AudioClip sonidoGiroTick; 
    public AudioClip sonidoPremio;

    private bool estaGirando = false;
    private bool tutorialDesbloqueado;
    private int frameDesbloqueoTutorial;
    private int puntuacion;
    private int rachaSuerte;
    private int tirosRealizados;
    private int aciertosSuerte;
    private bool secuenciaResultadoActiva;
    private bool partidaGanada;
    private bool asistenciaRevancha;

    private void OnEnable()
    {
        if (dialogueManager != null)
            dialogueManager.DialogueEnded += MostrarTutorial;

        if (panelInstrucciones != null) panelInstrucciones.SetActive(false);
    }

    private void OnDisable()
    {
        if (dialogueManager != null)
            dialogueManager.DialogueEnded -= MostrarTutorial;
    }

    void Start()
    {
        botonGirar.onClick.AddListener(IniciarGiro);
        
        // Keep the tutorial hidden until the opening dialogue has finished.
        if (panelInstrucciones != null) panelInstrucciones.SetActive(false);
        if (botonIniciar != null) botonIniciar.SetActive(false);
        botonGirar.gameObject.SetActive(false);
        textoResultado.text = "";
        puntuacion = 0;
        rachaSuerte = 0;
        tirosRealizados = 0;
        aciertosSuerte = 0;
        asistenciaRevancha = PlayerPrefs.GetInt(RevanchaKey, 0) == 1;
        if (asistenciaRevancha)
        {
            PlayerPrefs.DeleteKey(RevanchaKey);
            PlayerPrefs.Save();
        }
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
    }

    void Update()
    {
        if (tutorialDesbloqueado && Time.frameCount > frameDesbloqueoTutorial && panelInstrucciones.activeSelf)
        {
            bool clickMouse = Pointer.current != null && Pointer.current.press.wasPressedThisFrame;
            bool teclaE = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;

            if (clickMouse || teclaE)
            {
                EmpezarJuego();
            }
        }
    }

    private void MostrarTutorial()
    {
        if (secuenciaResultadoActiva)
        {
            secuenciaResultadoActiva = false;
            if (partidaGanada)
                CargarSiguienteEscena();
            else if (gameOverPanel != null)
                MostrarGameOver();
            return;
        }

        tutorialDesbloqueado = true;
        frameDesbloqueoTutorial = Time.frameCount;
        if (panelInstrucciones != null) panelInstrucciones.SetActive(true);
        if (botonIniciar != null) botonIniciar.SetActive(true);
    }

    // Función pública para pasar el dato del número de la suerte
    public void EstablecerNumeroDeLaSuerte(int numero)
    {
        numeroDeLaSuerte = numero;
        Debug.Log($"[{gameObject.name}] Número de la suerte establecido en: {numeroDeLaSuerte}");
    }

    public void EmpezarJuego()
    {
        if (panelInstrucciones.activeSelf)
        {
            panelInstrucciones.SetActive(false);
            botonGirar.gameObject.SetActive(true);
            textoResultado.text = asistenciaRevancha
                ? $"Second chance! A score boost is active. Reach {puntuacionObjetivo} points and discover the lucky number. Your final spin will guide you to it if needed."
                : $"Score {puntuacionObjetivo} points and discover the lucky number in {MaxTiros} spins to win!";
        }
    }

    private void ReproducirSonido(AudioClip clip, bool variarTono)
    {
        if (clip == null || AudioManager.Instance == null)
            return;

        if (variarTono)
            AudioManager.Instance.PlaySFXRandomPitch(clip);
        else
            AudioManager.Instance.PlaySFX(clip);
    }

    public void IniciarGiro()
    {
        if (!estaGirando && tirosRealizados < MaxTiros && premios.Length > 0)
        {
            tirosRealizados++;
            StartCoroutine(GirarRuleta());
        }
    }

    private IEnumerator GirarRuleta()
    {
        estaGirando = true;
        botonGirar.interactable = false;
        textoResultado.text = "Spinning...";

        float anguloInicial = ruedaTransform.eulerAngles.z;
        float rotacionAleatoria = Random.Range(0f, 360f);
        float rotacionTotal = -(360f * vueltasMinimas + rotacionAleatoria);
        float anguloObjetivo = anguloInicial + rotacionTotal;
        float gradosPorPremio = 360f / premios.Length;
        if (asistenciaRevancha && tirosRealizados >= MaxTiros && aciertosSuerte == 0)
        {
            int indiceNumeroSuerte = System.Array.IndexOf(premios, numeroDeLaSuerte.ToString());
            if (indiceNumeroSuerte >= 0)
            {
                float anguloSuerte = Mathf.Repeat((indiceNumeroSuerte + 0.5f) * gradosPorPremio - ajusteAngulo, 360f);
                anguloObjetivo += Mathf.DeltaAngle(Mathf.Repeat(anguloObjetivo, 360f), anguloSuerte);
            }
        }

        float tiempoTranscurrido = 0f;
        float anguloUltimoFrame = anguloInicial;
        float rotacionAcumuladaParaSonido = 0f;

        while (tiempoTranscurrido < tiempoGiro)
        {
            tiempoTranscurrido += Time.deltaTime;
            
            float t = tiempoTranscurrido / tiempoGiro;
            float tSuavizado = 1f - Mathf.Pow(1f - t, 3f); 

            float anguloActual = Mathf.Lerp(anguloInicial, anguloObjetivo, tSuavizado);
            ruedaTransform.eulerAngles = new Vector3(0, 0, anguloActual);

            float deltaRotacion = Mathf.Abs(anguloActual - anguloUltimoFrame);
            rotacionAcumuladaParaSonido += deltaRotacion;
            
            if (rotacionAcumuladaParaSonido >= gradosPorPremio)
            {
                ReproducirSonido(sonidoGiroTick, true);
                rotacionAcumuladaParaSonido -= gradosPorPremio; 
            }
            anguloUltimoFrame = anguloActual;

            yield return null;
        }

        ruedaTransform.eulerAngles = new Vector3(0, 0, anguloObjetivo);
        
        ReproducirSonido(sonidoPremio, false);
        DeterminarPremio();

        estaGirando = false;
        botonGirar.interactable = tirosRealizados < MaxTiros;
        if (tirosRealizados >= MaxTiros)
        {
            bool gano = puntuacion >= puntuacionObjetivo && aciertosSuerte > 0;
            textoResultado.text += $"\nNo spins left! Final score: {puntuacion}/{puntuacionObjetivo}. Lucky number discovered: {(aciertosSuerte > 0 ? "Yes" : "No")}.";
            IniciarSecuenciaResultado(gano);
        }
        else
            textoResultado.text += "\nSpins left: " + (MaxTiros - tirosRealizados);
    }

    private void IniciarSecuenciaResultado(bool gano)
    {
        partidaGanada = gano;
        DialogueSequence sequence = gano ? winSequence : loseSequence;
        if (botonGirar != null)
        {
            botonGirar.interactable = false;
            botonGirar.gameObject.SetActive(false);
        }

        if (dialogueManager != null && sequence != null)
        {
            secuenciaResultadoActiva = true;
            dialogueManager.StartDialogue(sequence);
            return;
        }

        Debug.LogWarning("Assign the win and lose dialogue sequences in the RuletaPremios Inspector.", this);
        if (gano)
            CargarSiguienteEscena();
        else if (gameOverPanel != null)
            MostrarGameOver();
    }

    private void MostrarGameOver()
    {
        if (!asistenciaRevancha)
        {
            PlayerPrefs.SetInt(RevanchaKey, 1);
            PlayerPrefs.Save();
        }

        if (gameOverText != null)
        {
            string reason = aciertosSuerte == 0
                ? "Lucky number not discovered."
                : $"Score target not reached ({puntuacionObjetivo}).";
            string retryText = asistenciaRevancha ? "No retries left." : "Click to retry (one assisted retry).";
            gameOverText.text = $"GAME OVER\nScore: {puntuacion}/{puntuacionObjetivo}\n{reason}\n{retryText}";
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            Button retryButton = gameOverPanel.GetComponent<Button>();
            if (retryButton != null)
                retryButton.interactable = !asistenciaRevancha;
        }
    }

    private void CargarSiguienteEscena()
    {
        int nextSceneIndex = SceneManager.GetActiveScene().buildIndex + 1;
        if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
            SceneManager.LoadScene(nextSceneIndex);
        else
            Debug.LogWarning("There is no scene after RuletaGame in Build Settings.", this);
    }

    public void Reintentar()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void DeterminarPremio()
    {
        float anguloFinalZ = ruedaTransform.eulerAngles.z % 360f;
        if (anguloFinalZ < 0) anguloFinalZ += 360f;

        float anguloCorregido = (anguloFinalZ + ajusteAngulo) % 360f;
        float gradosPorPremio = 360f / premios.Length;

        int indicePremio = Mathf.FloorToInt(anguloCorregido / gradosPorPremio);
        indicePremio = indicePremio % premios.Length;

        string premioGanado = premios[indicePremio];
        int puntosBase = int.TryParse(premioGanado, out int valorPremio) ? Mathf.Max(1, valorPremio) : 1;
        bool aciertoSuerte = numeroDeLaSuerte > 0 && premioGanado == numeroDeLaSuerte.ToString();

        if (aciertoSuerte)
        {
            aciertosSuerte++;
            rachaSuerte++;
            int multiplicador = 2 + rachaSuerte;
            int puntosGanados = AplicarBonificacionRevancha(puntosBase * multiplicador);
            puntuacion += puntosGanados;
            textoResultado.text = $"LUCKY HIT! You landed on {premioGanado}! x{multiplicador} bonus: +{puntosGanados} points. Streak: {rachaSuerte} | Score: {puntuacion}";
        }
        else
        {
            rachaSuerte = 0;
            int puntosGanados = AplicarBonificacionRevancha(puntosBase);
            puntuacion += puntosGanados;
            bool premioEsNumero = int.TryParse(premioGanado, out int numeroPremio);
            int distancia = numeroDeLaSuerte > 0 && premioEsNumero
                ? Mathf.Abs(numeroPremio - numeroDeLaSuerte)
                : int.MaxValue;
            string pista = distancia == 1 ? "So close! The lucky number is right next to this one."
                : distancia <= 3 ? "You're getting warm. Keep looking around this part of the wheel."
                : "Cold! The lucky number is farther away.";
            textoResultado.text = $"You won {puntosGanados} points. {pista} | Score: {puntuacion}";
        }
    }

    private int AplicarBonificacionRevancha(int puntosGanados)
    {
        if (!asistenciaRevancha)
            return puntosGanados;

        int tirosIncluyendoEste = MaxTiros - tirosRealizados + 1;
        int puntosNecesariosPorTiro = Mathf.CeilToInt((float)(puntuacionObjetivo - puntuacion) / tirosIncluyendoEste);
        return Mathf.Max(puntosGanados, puntosNecesariosPorTiro);
    }
}
