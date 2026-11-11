using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class RuletaPremios : MonoBehaviour
{
    [Header("Configuración Visual")]
    [Tooltip("El Transform del objeto que contiene el sprite de la rueda giratoria")]
    public Transform ruedaTransform; 
    
    [Tooltip("Lista de premios. Empieza por el que está arriba y sigue el orden en el sentido de las manecillas del reloj")]
    public string[] premios; 

    [Header("Ajustes de Giro")]
    public float tiempoGiro = 4f;
    [Tooltip("Cuántas vueltas completas dará como mínimo antes de detenerse")]
    public int vueltasMinimas = 5;
    
    [Tooltip("Ajusta este valor (ej. 0 a 360) si el premio seleccionado no coincide con el puntero visual")]
    public float ajusteAngulo = 0f; 

    [Header("UI")]
    public TextMeshProUGUI textoResultado;
    public Button botonGirar;

    private bool estaGirando = false;

    void Start()
    {
        botonGirar.onClick.AddListener(IniciarGiro);
        textoResultado.text = "¡Gira la ruleta para probar tu suerte!";
    }

    public void IniciarGiro()
    {
        if (!estaGirando && premios.Length > 0)
        {
            StartCoroutine(GirarRuleta());
        }
    }

    private IEnumerator GirarRuleta()
    {
        estaGirando = true;
        botonGirar.interactable = false;
        textoResultado.text = "Girando...";

        float anguloInicial = ruedaTransform.eulerAngles.z;
        float rotacionAleatoria = Random.Range(0f, 360f);
        
        float rotacionTotal = -(360f * vueltasMinimas + rotacionAleatoria);
        float anguloObjetivo = anguloInicial + rotacionTotal;

        float tiempoTranscurrido = 0f;

        while (tiempoTranscurrido < tiempoGiro)
        {
            tiempoTranscurrido += Time.deltaTime;
            
            float t = tiempoTranscurrido / tiempoGiro;
            float tSuavizado = 1f - Mathf.Pow(1f - t, 3f);

            float anguloActual = Mathf.Lerp(anguloInicial, anguloObjetivo, tSuavizado);
            ruedaTransform.eulerAngles = new Vector3(0, 0, anguloActual);

            yield return null;
        }

        ruedaTransform.eulerAngles = new Vector3(0, 0, anguloObjetivo);

        DeterminarPremio();

        estaGirando = false;
        botonGirar.interactable = true;
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
        textoResultado.text = $"Ganaste: <color=#FFD700>{premioGanado}</color>";
    }
}