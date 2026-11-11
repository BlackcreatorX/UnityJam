using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DialogueSystem
{
    /// <summary>Renders dialogue and translates traditional input into progression requests.</summary>
    public sealed class DialogueUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private DialogueManager dialogueManager;
        [SerializeField] private GameObject dialoguePanel;
        [SerializeField] private TextMeshProUGUI speakerName;
        [SerializeField] private TextMeshProUGUI dialogueText;
        [SerializeField] private Image characterPortrait;
        [SerializeField] private Transform particleSpawnPoint;
        [SerializeField] private GameObject continueIndicator;

        [Header("Typewriter")]
        [SerializeField] private bool useTypewriter = true;
        [Min(0f)] [SerializeField] private float charactersPerSecond = 35f;

        [Header("Particles")]
        [SerializeField] private bool parentParticlesToSpawnPoint;

        private Coroutine typewriterCoroutine;
        private bool isTyping;
        private ParticleSystem activeParticle;

        private void OnEnable()
        {
            if (dialogueManager == null)
            {
                Debug.LogError("DialogueUI requires a DialogueManager reference.", this);
                enabled = false;
                return;
            }

            dialogueManager.DialogueStarted += HandleDialogueStarted;
            dialogueManager.LineStarted += HandleLineStarted;
            dialogueManager.DialogueEnded += HandleDialogueEnded;
            if (dialoguePanel != null) dialoguePanel.SetActive(false);
            SetContinueVisible(false);
        }

        private void OnDisable()
        {
            if (dialogueManager != null)
            {
                dialogueManager.DialogueStarted -= HandleDialogueStarted;
                dialogueManager.LineStarted -= HandleLineStarted;
                dialogueManager.DialogueEnded -= HandleDialogueEnded;
            }
            StopTypewriter();
            DestroyActiveParticle();
        }

        private void Update()
        {
            if (!dialogueManager.IsDialogueActive || !WasAdvancePressed())
                return;

            if (isTyping)
                CompleteCurrentText();
            else
                dialogueManager.AdvanceDialogue();
        }

        private static bool WasAdvancePressed()
        {
            return Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
        }

        private void HandleDialogueStarted(DialogueSequence sequence)
        {
            if (dialoguePanel != null) dialoguePanel.SetActive(true);
        }

        private void HandleLineStarted(DialogueLine line)
        {
            StopTypewriter();
            DestroyActiveParticle();
            UpdateSpeaker(line);
            PlayParticle(line.ParticlePrefab);

            if (dialogueText == null)
                return;

            dialogueText.text = line.Text ?? string.Empty;
            dialogueText.maxVisibleCharacters = int.MaxValue;
            SetContinueVisible(false);

            if (useTypewriter && charactersPerSecond > 0f && dialogueText.text.Length > 0)
            {
                dialogueText.maxVisibleCharacters = 0;
                isTyping = true;
                typewriterCoroutine = StartCoroutine(TypeText());
            }
            else
            {
                isTyping = false;
                SetContinueVisible(true);
            }
        }

        private IEnumerator TypeText()
        {
            dialogueText.ForceMeshUpdate();
            int characterCount = dialogueText.textInfo.characterCount;
            float elapsed = 0f;
            int visibleCount = 0;

            while (visibleCount < characterCount)
            {
                elapsed += Time.unscaledDeltaTime * charactersPerSecond;
                int targetCount = Mathf.Min(characterCount, Mathf.FloorToInt(elapsed));
                if (targetCount > visibleCount)
                {
                    visibleCount = targetCount;
                    dialogueText.maxVisibleCharacters = visibleCount;
                }
                yield return null;
            }

            dialogueText.maxVisibleCharacters = int.MaxValue;
            typewriterCoroutine = null;
            isTyping = false;
            SetContinueVisible(true);
        }

        private void CompleteCurrentText()
        {
            StopTypewriter();
            if (dialogueText != null) dialogueText.maxVisibleCharacters = int.MaxValue;
            SetContinueVisible(true);
        }

        private void StopTypewriter()
        {
            if (typewriterCoroutine != null)
            {
                StopCoroutine(typewriterCoroutine);
                typewriterCoroutine = null;
            }
            isTyping = false;
        }

        private void UpdateSpeaker(DialogueLine line)
        {
            SpeakerData speaker = line.Speaker;
            if (speaker == null)
            {
                if (speakerName != null) speakerName.text = string.Empty;
                if (characterPortrait != null) characterPortrait.enabled = false;
                return;
            }

            if (speakerName != null) speakerName.text = speaker.SpeakerName;
            if (characterPortrait == null) return;

            var expressions = speaker.Expressions;
            if (expressions != null && line.SpriteIndex >= 0 && line.SpriteIndex < expressions.Count && expressions[line.SpriteIndex] != null)
            {
                characterPortrait.sprite = expressions[line.SpriteIndex];
                characterPortrait.enabled = true;
            }
            else
            {
                Debug.LogWarning($"Expression index {line.SpriteIndex} is invalid for speaker '{speaker.SpeakerName}'. Portrait hidden.", this);
                characterPortrait.enabled = false;
            }
        }

        private void PlayParticle(ParticleSystem prefab)
        {
            if (prefab == null) return;

            if (particleSpawnPoint == null)
            {
                Debug.LogWarning("A particle prefab is assigned, but no Particle Spawn Point is configured.", this);
                return;
            }

            activeParticle = parentParticlesToSpawnPoint
                ? Instantiate(prefab, particleSpawnPoint)
                : Instantiate(prefab, particleSpawnPoint.position, particleSpawnPoint.rotation);
            activeParticle.Play();
        }

        private void HandleDialogueEnded()
        {
            StopTypewriter();
            DestroyActiveParticle();
            if (dialoguePanel != null) dialoguePanel.SetActive(false);
            SetContinueVisible(false);
        }

        private void DestroyActiveParticle()
        {
            if (activeParticle == null) return;
            activeParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            Destroy(activeParticle.gameObject);
            activeParticle = null;
        }

        private void SetContinueVisible(bool visible)
        {
            if (continueIndicator != null) continueIndicator.SetActive(visible);
        }
    }
}
