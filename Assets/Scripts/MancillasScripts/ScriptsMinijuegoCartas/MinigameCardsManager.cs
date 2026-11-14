using UnityEngine;
using UnityEngine.InputSystem; 
using TMPro;
using System.Collections;
using System.Collections.Generic;
using DialogueSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MinigameManager : MonoBehaviour
{
    [Header("Número de la Suerte (Sistema Externo)")]
    public int numeroDeLaSuerte = 0;
    
    [Header("UI Instrucciones y Rondas")]
    public GameObject panelInstrucciones;
    public GameObject botonIniciar;
    public TextMeshProUGUI scoreText; 
    public GameObject actionButton; 
    private TextMeshProUGUI actionButtonText; 

    [Header("Referencias de Cartas")]
    public GameObject cardPrefab;
    public Sprite[] cardFrontSprites; 
    public Sprite cardBackSprite;     

    [Header("Posiciones en Pantalla")]
    public Transform[] linePositions; 
    public Transform playerPosition;  
    public Transform enemyPosition;   

    [Header("UI Resultado")]
    public TextMeshProUGUI resultText;
    [SerializeField] private DialogueManager dialogueManager;
    [SerializeField] private DialogueSequence victoryDialogue;
    [SerializeField] private DialogueSequence defeatDialogue;

    [Header("Audio")]
    public AudioClip sonidoMovimientoCarta;
    public AudioClip sonidoVictoriaRonda;      
    public AudioClip sonidoDerrotaRonda;
    public AudioClip sonidoVictoriaDefinitiva; 
    public AudioClip sonidoDerrotaDefinitiva;  
    public AudioClip sonidoEmpate;             

    [Header("Ajustes de Tiempos")]
    public float tiempoMemorizacion = 1.0f;  
    public float tiempoAnimacionVolteo = 0.4f; 
    public int cantidadDeRevoltijos = 15;    
    public float velocidadRevoltijo = 0.1f;  
    public float velocidadResolucion = 0.2f; 
    public float tiempoSuspenso = 0.2f;      

    private enum EstadoJuego { PausaInicial, Mezclando, EsperandoSeleccion, Resolviendo, EsperandoSiguienteRonda, FinDePartida }
    private EstadoJuego estadoActual = EstadoJuego.PausaInicial;

    private int victoriasJugador = 0;
    private int victoriasEnemigo = 0;
    private const int VICTORIAS_PARA_GANAR = 3;
    private bool tutorialDesbloqueado;
    private bool secuenciaResultadoActiva;
    private bool partidaGanada;

    private List<Card> activeCards = new List<Card>();
    private List<Card> cartasEnMesa = new List<Card>(); 
    private Card playerCard;
    private Card enemyCard;

    private void OnEnable()
    {
        if (dialogueManager != null)
            dialogueManager.DialogueEnded += HandleDialogueEnded;
    }

    private void OnDisable()
    {
        if (dialogueManager != null)
            dialogueManager.DialogueEnded -= HandleDialogueEnded;
    }

    private void Start()
    {
        LuckyNumberProgress.RecordCards(numeroDeLaSuerte);
        if (actionButton != null) actionButtonText = actionButton.GetComponentInChildren<TextMeshProUGUI>();
        
        if (panelInstrucciones != null) panelInstrucciones.SetActive(false);
        if (botonIniciar != null) botonIniciar.SetActive(false);
        if (actionButton != null) actionButton.SetActive(false);
        if (resultText != null) resultText.text = "";

        if (dialogueManager == null)
            PrepararInicioJuego();
        else
            StartCoroutine(ComprobarDialogoInicial());
    }

    private IEnumerator ComprobarDialogoInicial()
    {
        // DialogueManager starts its initial sequence in Start; check after all Start methods ran.
        yield return null;
        if (!dialogueManager.IsDialogueActive && !tutorialDesbloqueado && !secuenciaResultadoActiva)
            PrepararInicioJuego();
    }

    private void HandleDialogueEnded()
    {
        if (secuenciaResultadoActiva)
        {
            secuenciaResultadoActiva = false;
            if (partidaGanada)
                CargarSiguienteEscena();
            else
                MostrarGameOver();
            return;
        }

        PrepararInicioJuego();
    }

    private void PrepararInicioJuego()
    {
        if (tutorialDesbloqueado || estadoActual != EstadoJuego.PausaInicial) return;

        tutorialDesbloqueado = true;
        if (panelInstrucciones != null) panelInstrucciones.SetActive(false);
        if (botonIniciar != null) botonIniciar.SetActive(false);
        ReiniciarJuegoCompleto();
    }

    private void Update()
    {
        if (Pointer.current != null && Pointer.current.press.wasPressedThisFrame && estadoActual == EstadoJuego.EsperandoSeleccion)
        {
            Vector2 pointerScreenPos = Pointer.current.position.ReadValue();
            Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(pointerScreenPos);
            RaycastHit2D hit = Physics2D.Raycast(mouseWorldPos, Vector2.zero);

            if (hit.collider != null)
            {
                Card clickedCard = hit.collider.GetComponent<Card>();
                if (clickedCard != null && activeCards.Contains(clickedCard)) clickedCard.Select();
            }
        }
    }

    // Función pública para pasar el dato del número de la suerte
    public void EstablecerNumeroDeLaSuerte(int numero)
    {
        numeroDeLaSuerte = numero;
        LuckyNumberProgress.RecordCards(numeroDeLaSuerte);
        Debug.Log($"[{gameObject.name}] Número de la suerte establecido en: {numeroDeLaSuerte}");
    }

    public void EmpezarJuego()
    {
        // Kept for existing UI button hookups; the game now starts as soon as the opening dialogue ends.
        if (estadoActual == EstadoJuego.PausaInicial && tutorialDesbloqueado)
        {
            if (panelInstrucciones != null) panelInstrucciones.SetActive(false);
            if (botonIniciar != null) botonIniciar.SetActive(false);
            ReiniciarJuegoCompleto();
        }
    }

    public void OnActionButtonClicked()
    {
        ReproducirSonidoGenerico(sonidoMovimientoCarta);
        
        if (estadoActual == EstadoJuego.EsperandoSiguienteRonda) IniciarSiguienteRonda();
        else if (estadoActual == EstadoJuego.FinDePartida) ReiniciarJuegoCompleto();
    }

    private void IniciarSiguienteRonda()
    {
        estadoActual = EstadoJuego.Mezclando;
        actionButton.SetActive(false);
        resultText.text = "";
        LimpiarMesa();
        StartCoroutine(GameRoutine());
    }

    private void ReiniciarJuegoCompleto()
    {
        victoriasJugador = 0;
        victoriasEnemigo = 0;
        ActualizarTextoMarcador();
        IniciarSiguienteRonda();
    }

    private void LimpiarMesa()
    {
        foreach (Card c in cartasEnMesa) if (c != null) Destroy(c.gameObject);
        cartasEnMesa.Clear();
        activeCards.Clear();
        playerCard = null;
        enemyCard = null;
    }

    private void ActualizarTextoMarcador()
    {
        if (scoreText != null) scoreText.text = $"Player: {victoriasJugador}  |  Mammon: {victoriasEnemigo}";
    }

    private void ReproducirSonidoGenerico(AudioClip clip)
    {
        if (clip == null || AudioManager.Instance == null) return;

        AudioManager.Instance.PlaySFXRandomPitch(clip);
    }

    private void ReproducirSonidoResultado(AudioClip clip)
    {
        if (clip == null || AudioManager.Instance == null) return;

        AudioManager.Instance.PlaySFX(clip);
    }

    private IEnumerator GameRoutine()
    {
        for (int i = 0; i < 5; i++)
        {
            GameObject newCardObj = Instantiate(cardPrefab, linePositions[i].position, Quaternion.identity);
            Card card = newCardObj.GetComponent<Card>();
            card.Initialize(i + 1, cardFrontSprites[i], cardBackSprite);
            card.OnCardClicked += HandleCardSelected;
            activeCards.Add(card);
            cartasEnMesa.Add(card);
        }

        yield return new WaitForSeconds(tiempoMemorizacion);

        foreach (Card c in activeCards) StartCoroutine(c.FlipAnimated(false, tiempoAnimacionVolteo));
        yield return new WaitForSeconds(tiempoAnimacionVolteo + 0.1f);

        for (int i = 0; i < cantidadDeRevoltijos; i++)
        {
            int indexA = Random.Range(0, activeCards.Count);
            int indexB = Random.Range(0, activeCards.Count);
            
            if (indexA != indexB)
            {
                Vector3 posA = activeCards[indexA].transform.position;
                Vector3 posB = activeCards[indexB].transform.position;
                ReproducirSonidoGenerico(sonidoMovimientoCarta);

                StartCoroutine(activeCards[indexA].MoveToPosition(posB, velocidadRevoltijo));
                yield return StartCoroutine(activeCards[indexB].MoveToPosition(posA, velocidadRevoltijo));
            }
        }

        estadoActual = EstadoJuego.EsperandoSeleccion;
        foreach (Card c in activeCards) c.isSelectable = true;
    }

    private void HandleCardSelected(Card selectedCard)
    {
        if (playerCard != null || estadoActual != EstadoJuego.EsperandoSeleccion) return;
        
        estadoActual = EstadoJuego.Resolviendo;
        foreach (Card c in activeCards) c.isSelectable = false;
        
        playerCard = selectedCard;
        activeCards.Remove(playerCard); 
        StartCoroutine(ResolveGame());
    }

    private IEnumerator ResolveGame()
    {
        ReproducirSonidoGenerico(sonidoMovimientoCarta);
        yield return StartCoroutine(playerCard.MoveToPosition(playerPosition.position, velocidadResolucion));

        int randomIndex = Random.Range(0, activeCards.Count);
        enemyCard = activeCards[randomIndex];
        activeCards.Remove(enemyCard);

        ReproducirSonidoGenerico(sonidoMovimientoCarta);
        yield return StartCoroutine(enemyCard.MoveToPosition(enemyPosition.position, velocidadResolucion));
        yield return new WaitForSeconds(tiempoSuspenso);

        StartCoroutine(playerCard.FlipAnimated(true, tiempoAnimacionVolteo));
        StartCoroutine(enemyCard.FlipAnimated(true, tiempoAnimacionVolteo));
        yield return new WaitForSeconds(tiempoAnimacionVolteo);

        bool ganaRonda = playerCard.value > enemyCard.value;
        bool pierdeRonda = playerCard.value < enemyCard.value;

        if (ganaRonda)
        {
            resultText.text = "ROUND WON!";
            resultText.color = Color.green;
            victoriasJugador++;
        }
        else if (pierdeRonda)
        {
            resultText.text = "ROUND LOST!";
            resultText.color = Color.red;
            victoriasEnemigo++;
        }
        else 
        {
            resultText.text = "TIE!";
            resultText.color = Color.yellow;
        }

        ActualizarTextoMarcador();

        if (victoriasJugador >= VICTORIAS_PARA_GANAR)
        {
            resultText.text += "\n\n<color=#00FF00>YOU WON THE GAME!</color>";
            estadoActual = EstadoJuego.FinDePartida;
            ReproducirSonidoResultado(sonidoVictoriaDefinitiva); 
            IniciarSecuenciaResultado(true);
        }
        else if (victoriasEnemigo >= VICTORIAS_PARA_GANAR)
        {
            resultText.text += "\n\n<color=#FF0000>MAMMON WON THE GAME!</color>";
            estadoActual = EstadoJuego.FinDePartida;
            ReproducirSonidoResultado(sonidoDerrotaDefinitiva); 
            IniciarSecuenciaResultado(false);
        }
        else
        {
            estadoActual = EstadoJuego.EsperandoSiguienteRonda;
            if (actionButtonText != null) actionButtonText.text = "Next Round";
            if (ganaRonda) ReproducirSonidoResultado(sonidoVictoriaRonda);
            else if (pierdeRonda) ReproducirSonidoResultado(sonidoDerrotaRonda);
            else ReproducirSonidoResultado(sonidoEmpate);
        }

        if (estadoActual != EstadoJuego.FinDePartida && actionButton != null)
            actionButton.SetActive(true);
    }

    private void IniciarSecuenciaResultado(bool gano)
    {
        partidaGanada = gano;
        if (actionButton != null) actionButton.SetActive(false);

        DialogueSequence sequence = gano ? victoryDialogue : defeatDialogue;
        if (dialogueManager != null && sequence != null)
        {
            secuenciaResultadoActiva = true;
            dialogueManager.StartDialogue(sequence);
            return;
        }

        Debug.LogWarning("Assign the victory and defeat dialogue sequences to MinigameManager.", this);
        if (gano) CargarSiguienteEscena();
        else MostrarGameOver();
    }

    private void MostrarGameOver()
    {
        estadoActual = EstadoJuego.FinDePartida;
        string score = $"Player: {victoriasJugador}  |  Mammon: {victoriasEnemigo}";

        GameObject canvasObject = new GameObject("GameOverCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

        GameObject panel = new GameObject("GameOverPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvasObject.transform, false);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.92f);

        GameObject labelObject = new GameObject("GameOverText", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(panel.transform, false);
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0.1f, 0.25f);
        labelRect.anchorMax = new Vector2(0.9f, 0.75f);
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        if (resultText != null) label.font = resultText.font;
        label.text = $"GAME OVER\n\nMammon won the card game.\n{score}";
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = 56f;
        label.color = Color.white;
        label.enableAutoSizing = true;
        label.fontSizeMin = 28f;
        label.fontSizeMax = 56f;
    }

    private void CargarSiguienteEscena()
    {
        int nextSceneIndex = SceneManager.GetActiveScene().buildIndex + 1;
        if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
            SceneManager.LoadScene(nextSceneIndex);
        else
            SceneManager.LoadScene("MainMenu");
    }
}
