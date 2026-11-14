using UnityEngine;
using UnityEngine.InputSystem; 
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class RuletaPremios : MonoBehaviour
{
    [Header("UI Instrucciones")]
    public GameObject panelInstrucciones;
    public GameObject botonIniciar;

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

    void Start()
    {
        botonGirar.onClick.AddListener(IniciarGiro);
        
        // Estado inicial de espera
        panelInstrucciones.SetActive(true);
        if (botonIniciar != null) botonIniciar.SetActive(false);
        botonGirar.gameObject.SetActive(false);
        textoResultado.text = "";
    }

    void Update()
    {
        if (panelInstrucciones.activeSelf)
        {
            bool clickMouse = Pointer.current != null && Pointer.current.press.wasPressedThisFrame;
            bool teclaE = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;

            if (clickMouse || teclaE)
            {
                EmpezarJuego();
            }
        }
    }

    public void EmpezarJuego()
    {
        if (panelInstrucciones.activeSelf)
        {
            panelInstrucciones.SetActive(false);
            botonGirar.gameObject.SetActive(true);
            textoResultado.text = "¡Presiona Girar para probar tu suerte!";
        }
    }

    private void ReproducirSonido(AudioClip clip, bool variarTono)
    {
        if (clip != null)
        {
            if (variarTono) 
            {
                AudioManager.Instance.PlaySFXRandomPitch(clip);
            }
            else 
            {
                AudioManager.Instance.PlaySFX(clip);
            }
        }
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
        float gradosPorPremio = 360f / premios.Length;
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
        textoResultado.text = $"¡Felicidades! Ganaste: <color=#FFD700>{premioGanado}</color>";
    }
}