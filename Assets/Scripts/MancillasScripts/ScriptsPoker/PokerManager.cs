using UnityEngine;
using UnityEngine.InputSystem; 
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

[RequireComponent(typeof(AudioSource))]
public class PokerManager : MonoBehaviour
{
    [Header("UI Instrucciones")]
    public GameObject panelInstrucciones;
    public GameObject botonIniciar;

    [Header("Referencias Generales")]
    public GameObject cardPrefab;
    public Sprite cardBackSprite;
    public Sprite[] todosLosSpritesFrontales; 

    [Header("Zonas de la Mesa")]
    public Transform deckPosition;     
    public Transform discardPosition;  
    public Transform[] playerSlots;    
    public Transform[] enemySlots;     

    [Header("UI Principal")]
    public TextMeshProUGUI resultText;
    public TextMeshProUGUI scoreText; 
    public GameObject botonDescarte;    
    public GameObject actionButton;     
    private TextMeshProUGUI actionButtonText; 

    [Header("Audio")]
    public AudioClip sonidoMovimientoCarta;
    public AudioClip sonidoVictoriaRonda;      
    public AudioClip sonidoDerrotaRonda;
    public AudioClip sonidoVictoriaDefinitiva; 
    public AudioClip sonidoDerrotaDefinitiva;  
    public AudioClip sonidoEmpate;             
    private AudioSource audioSource;

    [Header("Ajustes Visuales")]
    public float velocidadReparto = 0.2f;
    public float tiempoGiro = 0.4f;

    private enum EstadoJuego { PausaInicial, Descartando, EsperandoShowdown, EsperandoSiguienteRonda, FinDePartida, Ocupado }
    private EstadoJuego estadoActual = EstadoJuego.PausaInicial;

    private int victoriasJugador = 0;
    private int victoriasEnemigo = 0;
    private const int VICTORIAS_PARA_GANAR = 3;

    private List<Card> mazo = new List<Card>();
    private Card[] manoJugador = new Card[5];
    private Card[] manoEnemigo = new Card[5];
    private List<Card> todasLasCartasInstanciadas = new List<Card>();

    public enum ClasificacionMano { CartaAlta, Par, DoblePar, Tercia, Escalera, Color, FullHouse, Poker, EscaleraColor, EscaleraReal }

    private class ResultadoMano : IComparable<ResultadoMano>
    {
        public ClasificacionMano Clasificacion;
        public List<int> Desempates;

        public int CompareTo(ResultadoMano otraMano)
        {
            if (this.Clasificacion != otraMano.Clasificacion) return this.Clasificacion.CompareTo(otraMano.Clasificacion);
            for (int i = 0; i < this.Desempates.Count; i++)
            {
                if (this.Desempates[i] != otraMano.Desempates[i]) return this.Desempates[i].CompareTo(otraMano.Desempates[i]);
            }
            return 0; 
        }
    }

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (actionButton != null) actionButtonText = actionButton.GetComponentInChildren<TextMeshProUGUI>();
        
        panelInstrucciones.SetActive(true);
        if (botonIniciar != null) botonIniciar.SetActive(false);
        if (botonDescarte != null) botonDescarte.SetActive(false);
        if (actionButton != null) actionButton.SetActive(false);
        
        resultText.text = "";
    }

    private void Update()
    {
        if (estadoActual == EstadoJuego.PausaInicial)
        {
            bool clickMouse = Pointer.current != null && Pointer.current.press.wasPressedThisFrame;
            bool teclaE = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;

            if (clickMouse || teclaE)
            {
                EmpezarJuego();
            }
            return;
        }

        if (Pointer.current != null && Pointer.current.press.wasPressedThisFrame && estadoActual == EstadoJuego.Descartando)
        {
            Vector2 pointerScreenPos = Pointer.current.position.ReadValue();
            Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(pointerScreenPos);
            RaycastHit2D hit = Physics2D.Raycast(mouseWorldPos, Vector2.zero);

            if (hit.collider != null)
            {
                Card clickedCard = hit.collider.GetComponent<Card>();
                if (clickedCard != null && manoJugador.Contains(clickedCard))
                {
                    clickedCard.ToggleDiscardSelection();
                    ReproducirSonidoCarta();
                }
            }
        }
    }

    public void EmpezarJuego()
    {
        if (estadoActual == EstadoJuego.PausaInicial)
        {
            panelInstrucciones.SetActive(false);
            ReiniciarJuegoCompleto();
        }
    }

    public void OnBotonDescarteClicked()
    {
        if (estadoActual == EstadoJuego.Descartando)
        {
            ReproducirSonidoCarta();
            estadoActual = EstadoJuego.Ocupado;
            botonDescarte.SetActive(false); 
            actionButton.SetActive(false);  
            foreach (Card c in manoJugador) if (c != null) c.isSelectable = false;
            StartCoroutine(ProcesarIntercambioCartas(saltarDescarteJugador: false));
        }
    }

    public void OnActionButtonClicked()
    {
        ReproducirSonidoCarta();
        
        if (estadoActual == EstadoJuego.Descartando)
        {
            estadoActual = EstadoJuego.Ocupado;
            botonDescarte.SetActive(false); 
            actionButton.SetActive(false);
            foreach (Card c in manoJugador) if (c != null) c.isSelectable = false;
            StartCoroutine(ProcesarIntercambioCartas(saltarDescarteJugador: true));
        }
        else if (estadoActual == EstadoJuego.EsperandoShowdown)
        {
            estadoActual = EstadoJuego.Ocupado;
            actionButton.SetActive(false);
            StartCoroutine(ProcesarShowdown()); 
        }
        else if (estadoActual == EstadoJuego.EsperandoSiguienteRonda) IniciarSiguienteRonda();
        else if (estadoActual == EstadoJuego.FinDePartida) ReiniciarJuegoCompleto();
    }

    private void IniciarSiguienteRonda()
    {
        estadoActual = EstadoJuego.Ocupado;
        actionButton.SetActive(false);
        botonDescarte.SetActive(false);
        resultText.text = "";
        LimpiarMesa();
        StartCoroutine(RutinaInicialPoker());
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
        foreach (Card c in todasLasCartasInstanciadas) if (c != null) Destroy(c.gameObject);
        todasLasCartasInstanciadas.Clear();
        mazo.Clear();
        for (int i = 0; i < 5; i++) { manoJugador[i] = null; manoEnemigo[i] = null; }
    }

    private void ActualizarTextoMarcador()
    {
        if (scoreText != null) scoreText.text = $"Jugador: {victoriasJugador}  |  Mammon: {victoriasEnemigo}";
    }

    private void ReproducirSonidoCarta()
    {
        if (audioSource != null && sonidoMovimientoCarta != null)
        {
            audioSource.pitch = UnityEngine.Random.Range(0.9f, 1.1f);
            audioSource.PlayOneShot(sonidoMovimientoCarta);
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

    private IEnumerator RutinaInicialPoker()
    {
        GenerarMazo();
        BarajarMazo();

        for (int i = 0; i < 5; i++)
        {
            yield return StartCoroutine(RobarCarta(i, manoJugador, playerSlots[i], true));
            yield return StartCoroutine(RobarCarta(i, manoEnemigo, enemySlots[i], false));
        }

        estadoActual = EstadoJuego.Descartando;
        foreach (Card c in manoJugador) if (c != null) c.isSelectable = true;
        
        botonDescarte.SetActive(true); 
        actionButtonText.text = "Mostrar Resultados";
        actionButton.SetActive(true);
    }

    private void GenerarMazo()
    {
        int spriteIndex = 0;
        foreach (Card.Palo palo in System.Enum.GetValues(typeof(Card.Palo)))
        {
            for (int rango = 2; rango <= 14; rango++)
            {
                GameObject obj = Instantiate(cardPrefab, deckPosition.position, Quaternion.identity);
                Card nuevaCarta = obj.GetComponent<Card>();
                nuevaCarta.Initialize(rango, palo, todosLosSpritesFrontales[spriteIndex], cardBackSprite);
                mazo.Add(nuevaCarta);
                todasLasCartasInstanciadas.Add(nuevaCarta); 
                spriteIndex++;
            }
        }
    }

    private void BarajarMazo()
    {
        for (int i = 0; i < mazo.Count; i++)
        {
            Card temp = mazo[i];
            int randomIndex = UnityEngine.Random.Range(i, mazo.Count);
            mazo[i] = mazo[randomIndex];
            mazo[randomIndex] = temp;
        }
    }

    private IEnumerator RobarCarta(int slotIndex, Card[] mano, Transform slotDestino, bool voltearBocaArriba)
    {
        if (mazo.Count == 0) yield break;
        Card cartaRobada = mazo[0];
        mazo.RemoveAt(0);
        mano[slotIndex] = cartaRobada;
        ReproducirSonidoCarta();
        yield return StartCoroutine(cartaRobada.MoveToPosition(slotDestino.position, velocidadReparto));
        if (voltearBocaArriba) yield return StartCoroutine(cartaRobada.FlipAnimated(true, tiempoGiro));
    }

    private IEnumerator ProcesarIntercambioCartas(bool saltarDescarteJugador)
    {
        if (!saltarDescarteJugador)
        {
            for (int i = 0; i < 5; i++)
            {
                if (manoJugador[i] != null && manoJugador[i].isSelectedForDiscard)
                {
                    Card c = manoJugador[i];
                    manoJugador[i] = null; 
                    c.spriteRenderer.color = Color.white; 
                    yield return StartCoroutine(c.MoveToPosition(discardPosition.position, velocidadReparto));
                }
            }
            for (int i = 0; i < 5; i++) if (manoJugador[i] == null) yield return StartCoroutine(RobarCarta(i, manoJugador, playerSlots[i], true));
        }
        else
        {
            foreach (Card c in manoJugador) if (c != null && c.isSelectedForDiscard) c.ToggleDiscardSelection();
        }

        for (int i = 0; i < 5; i++)
        {
            if (manoEnemigo[i] != null && manoEnemigo[i].rangoCarta < 11) 
            {
                Card c = manoEnemigo[i];
                manoEnemigo[i] = null; 
                yield return StartCoroutine(c.MoveToPosition(discardPosition.position, velocidadReparto));
            }
        }
        for (int i = 0; i < 5; i++) if (manoEnemigo[i] == null) yield return StartCoroutine(RobarCarta(i, manoEnemigo, enemySlots[i], false));

        if (saltarDescarteJugador) StartCoroutine(ProcesarShowdown());
        else
        {
            estadoActual = EstadoJuego.EsperandoShowdown;
            actionButtonText.text = "Mostrar Resultados";
            actionButton.SetActive(true);
        }
    }

    private IEnumerator ProcesarShowdown()
    {
        for (int i = 0; i < 5; i++) if (manoEnemigo[i] != null) StartCoroutine(manoEnemigo[i].FlipAnimated(true, tiempoGiro));
        yield return new WaitForSeconds(tiempoGiro);
        DeterminarGanador();
    }

    private ResultadoMano EvaluarMano(Card[] mano)
    {
        var cartas = mano.Where(c => c != null).ToList();
        if (cartas.Count != 5) return new ResultadoMano { Clasificacion = ClasificacionMano.CartaAlta, Desempates = new List<int>() };

        var rangos = cartas.Select(c => c.rangoCarta).OrderByDescending(r => r).ToList();
        var palos = cartas.Select(c => c.paloCarta).ToList();

        bool esColor = palos.Distinct().Count() == 1;
        bool esEscalera = true;

        for (int i = 0; i < 4; i++) if (rangos[i] - 1 != rangos[i+1]) { esEscalera = false; break; }
        
        if (!esEscalera && rangos[0] == 14 && rangos[1] == 5 && rangos[2] == 4 && rangos[3] == 3 && rangos[4] == 2) 
        {
            esEscalera = true;
            rangos = new List<int> { 5, 4, 3, 2, 1 };
        }

        var grupos = rangos.GroupBy(r => r).Select(g => new { Rango = g.Key, Cantidad = g.Count() })
                           .OrderByDescending(g => g.Cantidad).ThenByDescending(g => g.Rango).ToList();

        var resultado = new ResultadoMano();
        resultado.Desempates = grupos.SelectMany(g => Enumerable.Repeat(g.Rango, g.Cantidad)).ToList();

        if (esEscalera && esColor) resultado.Clasificacion = rangos[0] == 14 ? ClasificacionMano.EscaleraReal : ClasificacionMano.EscaleraColor;
        else if (grupos[0].Cantidad == 4) resultado.Clasificacion = ClasificacionMano.Poker;
        else if (grupos[0].Cantidad == 3 && grupos[1].Cantidad == 2) resultado.Clasificacion = ClasificacionMano.FullHouse;
        else if (esColor) resultado.Clasificacion = ClasificacionMano.Color;
        else if (esEscalera) resultado.Clasificacion = ClasificacionMano.Escalera;
        else if (grupos[0].Cantidad == 3) resultado.Clasificacion = ClasificacionMano.Tercia;
        else if (grupos[0].Cantidad == 2 && grupos[1].Cantidad == 2) resultado.Clasificacion = ClasificacionMano.DoblePar;
        else if (grupos[0].Cantidad == 2) resultado.Clasificacion = ClasificacionMano.Par;
        else resultado.Clasificacion = ClasificacionMano.CartaAlta;

        return resultado;
    }

    private void DeterminarGanador()
    {
        ResultadoMano poderJugador = EvaluarMano(manoJugador);
        ResultadoMano poderEnemigo = EvaluarMano(manoEnemigo);
        int comparacion = poderJugador.CompareTo(poderEnemigo);

        if (comparacion > 0)
        {
            resultText.text = $"¡GANASTE LA RONDA!\n({poderJugador.Clasificacion})";
            resultText.color = Color.green;
            victoriasJugador++; 
        }
        else if (comparacion < 0)
        {
            resultText.text = $"¡PERDISTE LA RONDA!\n({poderEnemigo.Clasificacion})";
            resultText.color = Color.red;
            victoriasEnemigo++; 
        }
        else
        {
            resultText.text = $"¡EMPATE EXACTO!\nAmbos con {poderJugador.Clasificacion}";
            resultText.color = Color.yellow;
        }

        ActualizarTextoMarcador();

        if (victoriasJugador >= VICTORIAS_PARA_GANAR)
        {
            resultText.text += "\n\n<color=#00FF00>¡GANASTE LA PARTIDA!</color>";
            estadoActual = EstadoJuego.FinDePartida;
            actionButtonText.text = "Reiniciar Juego";
            ReproducirSonidoResultado(sonidoVictoriaDefinitiva);
        }
        else if (victoriasEnemigo >= VICTORIAS_PARA_GANAR)
        {
            resultText.text += "\n\n<color=#FF0000>¡MAMMON GANA LA PARTIDA!</color>";
            estadoActual = EstadoJuego.FinDePartida;
            actionButtonText.text = "Reiniciar Juego";
            ReproducirSonidoResultado(sonidoDerrotaDefinitiva);
        }
        else
        {
            estadoActual = EstadoJuego.EsperandoSiguienteRonda;
            actionButtonText.text = "Siguiente Ronda";
            if (comparacion > 0) ReproducirSonidoResultado(sonidoVictoriaRonda);
            else if (comparacion < 0) ReproducirSonidoResultado(sonidoDerrotaRonda);
            else ReproducirSonidoResultado(sonidoEmpate);
        }

        actionButton.SetActive(true); 
    }
}