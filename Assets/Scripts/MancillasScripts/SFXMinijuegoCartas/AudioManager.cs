using UnityEngine;
using System.Collections;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Fuentes de Audio (Audio Sources)")]
    public AudioSource musicSource;
    public AudioSource sfxSource;
    [SerializeField] private AudioClip backgroundMusic;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CrearAudioManagerSiHaceFalta()
    {
        if (Instance != null) return;

        GameObject prefab = Resources.Load<GameObject>("AudioManager");
        if (prefab != null)
            Instantiate(prefab);
        else
            Debug.LogError("AudioManager prefab is missing from a Resources folder.");
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject); 
            return;
        }

        CargarVolumenes();
        PlayMusic(backgroundMusic);
    }

    public void PlayMusic(AudioClip clip)
    {
        if (clip == null) return;
        if (musicSource.clip == clip && musicSource.isPlaying) return;

        musicSource.clip = clip;
        musicSource.Play();
    }

    public void StopMusic()
    {
        musicSource.Stop();
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;
        
        // Reseteamos el tono a 1 por si la función RandomPitch lo modificó antes
        sfxSource.pitch = 1f; 
        sfxSource.PlayOneShot(clip);
    }

    // Nueva función para simular la variación que tenías en las cartas
    public void PlaySFXRandomPitch(AudioClip clip, float minPitch = 0.9f, float maxPitch = 1.1f)
    {
        if (clip == null) return;
        
        sfxSource.pitch = Random.Range(minPitch, maxPitch);
        sfxSource.PlayOneShot(clip);
        
        // Devolvemos el tono a 1 después de reproducirlo usando una pequeña corrutina
        StartCoroutine(ResetPitch(clip.length));
    }

    private IEnumerator ResetPitch(float delay)
    {
        yield return new WaitForSeconds(delay);
        sfxSource.pitch = 1f;
    }

    public void SetMusicVolume(float volume)
    {
        musicSource.volume = volume;
        PlayerPrefs.SetFloat("MusicVolume", volume); 
    }

    public void SetSFXVolume(float volume)
    {
        sfxSource.volume = volume;
        PlayerPrefs.SetFloat("SFXVolume", volume); 
    }

    private void CargarVolumenes()
    {
        musicSource.volume = PlayerPrefs.GetFloat("MusicVolume", 1.0f);
        sfxSource.volume = PlayerPrefs.GetFloat("SFXVolume", 1.0f);
    }
}
