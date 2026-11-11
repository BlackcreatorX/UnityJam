using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
public class Card : MonoBehaviour
{
    public int value;
    public bool isSelectable = false;
    public Action<Card> OnCardClicked;

    public enum Palo { Corazones, Diamantes, Treboles, Picas }
    public Palo paloCarta;
    public int rangoCarta;
    public bool isSelectedForDiscard = false;

    public SpriteRenderer spriteRenderer; 
    private Sprite frontSprite;
    private Sprite backSprite;
    private BoxCollider2D col2D;

    [Header("Efecto de Flotación (Espera)")]
    public float velocidadFlotacion = 3.0f;
    public float amplitudFlotacion = 0.08f;
    public float elevacionAlEnfocar = 0.25f;
    public float velocidadTransicion = 8.0f;

    private Vector3 posicionObjetivoFisica; 
    private float desplazamientoVisualY = 0f; 
    private float desplazamientoObjetivoY = 0f; 
    private bool mouseEncima = false;
    private float tiempoInicioFlotacion;

    public void Initialize(int val, Sprite front, Sprite back)
    {
        value = val;
        frontSprite = front;
        backSprite = back;
        ConfigurarInicial(frontSprite);
    }

    public void Initialize(int rango, Palo palo, Sprite front, Sprite back)
    {
        rangoCarta = rango;
        paloCarta = palo;
        frontSprite = front;
        backSprite = back;
        ConfigurarInicial(backSprite);
    }

    private void ConfigurarInicial(Sprite spriteInicial)
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        col2D = GetComponent<BoxCollider2D>();
        spriteRenderer.sprite = spriteInicial; 
        
        posicionObjetivoFisica = transform.position;
        tiempoInicioFlotacion = UnityEngine.Random.Range(0f, 100f); 
    }

    public void Flip(bool faceUp)
    {
        spriteRenderer.sprite = faceUp ? frontSprite : backSprite;
    }

    public IEnumerator FlipAnimated(bool faceUp, float duration)
    {
        Vector3 escalaOriginal = transform.localScale;
        Vector3 escalaPlana = new Vector3(0f, escalaOriginal.y, escalaOriginal.z);
        float mitadDelTiempo = duration / 2f;
        float tiempoTranscurrido = 0f;

        while (tiempoTranscurrido < mitadDelTiempo)
        {
            transform.localScale = Vector3.Lerp(escalaOriginal, escalaPlana, tiempoTranscurrido / mitadDelTiempo);
            tiempoTranscurrido += Time.deltaTime;
            yield return null;
        }

        Flip(faceUp);
        transform.localScale = escalaPlana;

        tiempoTranscurrido = 0f;
        while (tiempoTranscurrido < mitadDelTiempo)
        {
            transform.localScale = Vector3.Lerp(escalaPlana, escalaOriginal, tiempoTranscurrido / mitadDelTiempo);
            tiempoTranscurrido += Time.deltaTime;
            yield return null;
        }
        transform.localScale = escalaOriginal;
    }

    public void ToggleDiscardSelection()
    {
        isSelectedForDiscard = !isSelectedForDiscard;
        spriteRenderer.color = isSelectedForDiscard ? Color.gray : Color.white;
    }

    private void Update()
    {
        if (Pointer.current != null && isSelectable)
        {
            Vector2 pointerPos = Pointer.current.position.ReadValue();
            Vector2 worldPos = Camera.main.ScreenToWorldPoint(pointerPos);
            mouseEncima = col2D.OverlapPoint(worldPos);
        }
        else
        {
            mouseEncima = false;
        }

        if (isSelectable)
        {
            if (mouseEncima || isSelectedForDiscard)
            {
                desplazamientoObjetivoY = elevacionAlEnfocar;
            }
            else
            {
                float ondaSeno = Mathf.Sin((Time.time + tiempoInicioFlotacion) * velocidadFlotacion);
                desplazamientoObjetivoY = ondaSeno * amplitudFlotacion;
            }
            desplazamientoVisualY = Mathf.Lerp(desplazamientoVisualY, desplazamientoObjetivoY, Time.deltaTime * velocidadTransicion);
        }
        else
        {
            desplazamientoVisualY = Mathf.Lerp(desplazamientoVisualY, 0f, Time.deltaTime * velocidadTransicion);
        }

        transform.position = posicionObjetivoFisica + new Vector3(0, desplazamientoVisualY, 0);
    }

    public void Select()
    {
        if (isSelectable && OnCardClicked != null)
        {
            mouseEncima = false;
            OnCardClicked(this);
        }
    }

    public IEnumerator MoveToPosition(Vector3 targetPosition, float duration)
    {
        Vector3 startPosition = posicionObjetivoFisica;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration;
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            posicionObjetivoFisica = Vector3.Lerp(startPosition, targetPosition, smoothT);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        posicionObjetivoFisica = targetPosition;
    }
}