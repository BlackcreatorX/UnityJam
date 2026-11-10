using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(AudioSource))]
public class MinigameManager : MonoBehaviour
{
    [Header("Referencias de Cartas")]
    public GameObject cardPrefab;
    public Sprite[] cardFrontSprites; 
    public Sprite cardBackSprite;     

    [Header("Posiciones en Pantalla")]
    public Transform[] linePositions; 
    public Transform playerPosition;  
    public Transform enemyPosition;   

    [Header("UI")]
    public TextMeshProUGUI resultText;

    [Header("Audio")]
    public AudioClip sonidoMovimientoCarta;
    public AudioClip sonidoVictoria;      
    public AudioClip sonidoDerrota;       
    private AudioSource audioSource;

    [Header("Ajustes de Tiempos")]
    public float tiempoMemorizacion = 1.0f;  
    public float tiempoAnimacionVolteo = 0.4f; 
    public int cantidadDeRevoltijos = 15;    
    public float velocidadRevoltijo = 0.1f;  
    public float velocidadResolucion = 0.2f; 
    public float tiempoSuspenso = 0.2f;      

    private List<Card> activeCards = new List<Card>();
    private Card playerCard;
    private Card enemyCard;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        resultText.text = "";
        StartCoroutine(GameRoutine());
    }

    private void Update()
    {
        if (Pointer.current != null && Pointer.current.press.wasPressedThisFrame)
        {
            Vector2 pointerScreenPos = Pointer.current.position.ReadValue();
            Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(pointerScreenPos);
            RaycastHit2D hit = Physics2D.Raycast(mouseWorldPos, Vector2.zero);

            if (hit.collider != null)
            {
                Card clickedCard = hit.collider.GetComponent<Card>();
                if (clickedCard != null)
                {
                    clickedCard.Select();
                }
            }
        }
    }

    private void ReproducirSonidoCarta()
    {
        if (sonidoMovimientoCarta != null && audioSource != null)
        {
            audioSource.pitch = Random.Range(0.9f, 1.1f); 
            audioSource.PlayOneShot(sonidoMovimientoCarta);
        }
    }

    private void ReproducirSonidoResultado(bool victoria)
    {
        if (audioSource != null)
        {
            audioSource.pitch = 1f; 
            AudioClip clipAReproducir = victoria ? sonidoVictoria : sonidoDerrota;
            if (clipAReproducir != null) audioSource.PlayOneShot(clipAReproducir);
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
                ReproducirSonidoCarta();

                StartCoroutine(activeCards[indexA].MoveToPosition(posB, velocidadRevoltijo));
                yield return StartCoroutine(activeCards[indexB].MoveToPosition(posA, velocidadRevoltijo));
            }
        }

        foreach (Card c in activeCards) c.isSelectable = true;
    }

    private void HandleCardSelected(Card selectedCard)
    {
        if (playerCard != null) return;
        foreach (Card c in activeCards) c.isSelectable = false;
        
        playerCard = selectedCard;
        activeCards.Remove(playerCard); 
        StartCoroutine(ResolveGame());
    }

    private IEnumerator ResolveGame()
    {
        ReproducirSonidoCarta();
        yield return StartCoroutine(playerCard.MoveToPosition(playerPosition.position, velocidadResolucion));

        int randomIndex = Random.Range(0, activeCards.Count);
        enemyCard = activeCards[randomIndex];
        activeCards.Remove(enemyCard);

        ReproducirSonidoCarta();
        yield return StartCoroutine(enemyCard.MoveToPosition(enemyPosition.position, velocidadResolucion));
        yield return new WaitForSeconds(tiempoSuspenso);

        StartCoroutine(playerCard.FlipAnimated(true, tiempoAnimacionVolteo));
        StartCoroutine(enemyCard.FlipAnimated(true, tiempoAnimacionVolteo));
        yield return new WaitForSeconds(tiempoAnimacionVolteo);

        if (playerCard.value > enemyCard.value)
        {
            resultText.text = "¡VICTORIA!";
            resultText.color = Color.green;
            ReproducirSonidoResultado(true); 
        }
        else
        {
            resultText.text = "¡DERROTA!";
            resultText.color = Color.red;
            ReproducirSonidoResultado(false); 
        }
    }
}