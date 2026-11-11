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

        [Header("Portrait")]
        [SerializeField] private bool portraitStartsOnRight;
        [Min(0.1f)] [SerializeField] private float portraitExitDuration = 0.8f;
        [Min(0f)] [SerializeField] private float portraitCenterBounce = 32f;

        private Coroutine typewriterCoroutine;
        private Coroutine portraitExitCoroutine;
        private bool isTyping;
        private ParticleSystem activeParticle;
        private bool portraitOnRight;
        private Vector3 portraitRestPosition;

        private void OnEnable()
        {
            portraitOnRight = portraitStartsOnRight;
            ApplyPortraitSide();

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
            StopPortraitExit(restorePosition: false);
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
            StopPortraitExit(restorePosition: true);
            if (dialoguePanel != null) dialoguePanel.SetActive(true);
        }

        /// <summary>Moves the portrait to the opposite half of the screen without changing its orientation.</summary>
        public void SwapPortraitSide()
        {
            SetPortraitSide(!portraitOnRight);
        }

        /// <summary>Places the portrait on the requested side without reflecting its sprite.</summary>
        public void SetPortraitSide(bool onRight)
        {
            if (portraitOnRight == onRight)
                return;

            StopPortraitExit(restorePosition: true);
            portraitOnRight = onRight;
            ApplyPortraitSide();
        }

        private void ApplyPortraitSide()
        {
            if (characterPortrait == null)
                return;

            var rect = characterPortrait.rectTransform;
            Vector2 anchorMin = rect.anchorMin;
            Vector2 anchorMax = rect.anchorMax;
            Vector2 offsetMin = rect.offsetMin;
            Vector2 offsetMax = rect.offsetMax;

            anchorMin.x = portraitOnRight ? 0.5f : 0f;
            anchorMax.x = portraitOnRight ? 1f : 0.5f;
            offsetMin.x = portraitOnRight ? 0f : 24f;
            offsetMax.x = portraitOnRight ? -24f : 0f;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            portraitRestPosition = rect.localPosition;
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
            StopPortraitExit(restorePosition: true);
            if (characterPortrait != null && characterPortrait.enabled && characterPortrait.sprite != null)
                portraitExitCoroutine = StartCoroutine(AnimatePortraitExit());
        }

        private IEnumerator AnimatePortraitExit()
        {
            RectTransform rect = characterPortrait.rectTransform;
            float duration = Mathf.Max(0.1f, portraitExitDuration);
            float sideDirection = portraitOnRight ? 1f : -1f;
            RectTransform canvasRect = characterPortrait.canvas != null
                ? characterPortrait.canvas.GetComponent<RectTransform>()
                : null;
            float width = canvasRect != null ? canvasRect.rect.width : Screen.width;
            float exitX = -(width + rect.rect.width);
            float inwardX = -sideDirection * portraitCenterBounce;
            float recoilX = sideDirection * portraitCenterBounce * 0.2f;
            Vector3 start = portraitRestPosition;

            yield return MovePortrait(rect, start, start + Vector3.right * inwardX, duration * 0.24f);
            yield return MovePortrait(rect, start + Vector3.right * inwardX, start + Vector3.right * recoilX, duration * 0.18f);
            yield return MovePortrait(rect, start + Vector3.right * recoilX, start + Vector3.right * exitX, duration * 0.58f);

            characterPortrait.enabled = false;
            rect.localPosition = portraitRestPosition;
            portraitExitCoroutine = null;
        }

        private static IEnumerator MovePortrait(RectTransform rect, Vector3 from, Vector3 to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                t = t * t * (3f - 2f * t);
                rect.localPosition = Vector3.LerpUnclamped(from, to, t);
                yield return null;
            }
            rect.localPosition = to;
        }

        private void StopPortraitExit(bool restorePosition)
        {
            if (portraitExitCoroutine != null)
            {
                StopCoroutine(portraitExitCoroutine);
                portraitExitCoroutine = null;
            }

            if (restorePosition && characterPortrait != null)
                characterPortrait.rectTransform.localPosition = portraitRestPosition;
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
