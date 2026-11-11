using UnityEngine;
using UnityEngine.InputSystem; 
using TMPro;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(AudioSource))]
public class MinigameManager : MonoBehaviour
{
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

    [Header("Audio")]
    public AudioClip sonidoMovimientoCarta;
    public AudioClip sonidoVictoriaRonda;      
    public AudioClip sonidoDerrotaRonda;
    public AudioClip sonidoVictoriaDefinitiva;
    public AudioClip sonidoDerrotaDefinitiva;
    public AudioClip sonidoEmpate;
    private AudioSource audioSource;

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

    private List<Card> activeCards = new List<Card>();
    private List<Card> cartasEnMesa = new List<Card>();
    private Card playerCard;
    private Card enemyCard;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (actionButton != null) actionButtonText = actionButton.GetComponentInChildren<TextMeshProUGUI>();
        
        panelInstrucciones.SetActive(true);
        botonIniciar.SetActive(false);
        if (actionButton != null) actionButton.SetActive(false);
        resultText.text = "";
        
        StartCoroutine(EsperarParaMostrarBoton());
    }

    private IEnumerator EsperarParaMostrarBoton()
    {
        yield return new WaitForSeconds(3f);
        botonIniciar.SetActive(true);
    }

    public void EmpezarJuego()
    {
        panelInstrucciones.SetActive(false);
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
        if (scoreText != null) scoreText.text = $"Jugador: {victoriasJugador}  |  Mammon: {victoriasEnemigo}";
    }

    private void ReproducirSonidoGenerico(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.pitch = Random.Range(0.9f, 1.1f);
            audioSource.PlayOneShot(clip);
        }
    }

    private void ReproducirSonidoResultado(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.pitch = 1f; 
            audioSource.PlayOneShot(clip);
        }
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

        //Evaluacion de ganador
        bool ganaRonda = playerCard.value > enemyCard.value;
        bool pierdeRonda = playerCard.value < enemyCard.value;

        if (ganaRonda)
        {
            resultText.text = "¡VICTORIA EN LA RONDA!";
            resultText.color = Color.green;
            victoriasJugador++;
        }
        else if (pierdeRonda)
        {
            resultText.text = "¡DERROTA EN LA RONDA!";
            resultText.color = Color.red;
            victoriasEnemigo++;
        }
        else 
        {
            resultText.text = "¡EMPATE!";
            resultText.color = Color.yellow;
        }

        ActualizarTextoMarcador();

        // Verificar si alguien ganó la partida completa (3 victorias)
        if (victoriasJugador >= VICTORIAS_PARA_GANAR)
        {
            resultText.text += "\n\n<color=#00FF00>¡GANASTE LA PARTIDA!</color>";
            estadoActual = EstadoJuego.FinDePartida;
            actionButtonText.text = "Reiniciar Juego";
            ReproducirSonidoResultado(sonidoVictoriaDefinitiva);
        }
        else if (victoriasEnemigo >= VICTORIAS_PARA_GANAR)
        {
            resultText.text += "\n\n<color=#FF0000>¡LA IA GANA LA PARTIDA!</color>";
            estadoActual = EstadoJuego.FinDePartida;
            actionButtonText.text = "Reiniciar Juego";
            ReproducirSonidoResultado(sonidoDerrotaDefinitiva);
        }
        else
        {
            estadoActual = EstadoJuego.EsperandoSiguienteRonda;
            actionButtonText.text = "Siguiente Ronda";
            
            // Sonidos de Ronda normal
            if (ganaRonda) ReproducirSonidoResultado(sonidoVictoriaRonda);
            else if (pierdeRonda) ReproducirSonidoResultado(sonidoDerrotaRonda);
            else ReproducirSonidoResultado(sonidoEmpate);
        }

        actionButton.SetActive(true); 
    }
}