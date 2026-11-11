using System;
using UnityEngine;

namespace DialogueSystem
{
    /// <summary>Owns dialogue progression and publishes changes for presentation layers.</summary>
    public sealed class DialogueManager : MonoBehaviour
    {
        [SerializeField] private DialogueSequence initialSequence;

        private DialogueSequence currentSequence;
        private int currentLineIndex = -1;

        public event Action<DialogueSequence> DialogueStarted;
        public event Action<DialogueLine> LineStarted;
        public event Action DialogueEnded;

        public bool IsDialogueActive => currentSequence != null;
        public int CurrentLineIndex => currentLineIndex;

        private void Start()
        {
            if (initialSequence != null)
                StartDialogue(initialSequence);
        }

        public void StartDialogue(DialogueSequence sequence)
        {
            if (sequence == null)
            {
                Debug.LogWarning("Cannot start a null dialogue sequence.", this);
                return;
            }

            currentSequence = sequence;
            currentLineIndex = -1;
            DialogueStarted?.Invoke(currentSequence);
            AdvanceDialogue();
        }

        public void AdvanceDialogue()
        {
            if (currentSequence == null)
                return;

            var lines = currentSequence.Lines;
            while (lines != null && ++currentLineIndex < lines.Count && lines[currentLineIndex] == null)
                Debug.LogWarning($"Dialogue '{currentSequence.name}' contains a null line at index {currentLineIndex}; skipping it.", currentSequence);

            if (lines == null || currentLineIndex >= lines.Count)
            {
                EndDialogue();
                return;
            }

            LineStarted?.Invoke(lines[currentLineIndex]);
        }

        public void EndDialogue()
        {
            if (currentSequence == null)
                return;

            currentSequence = null;
            currentLineIndex = -1;
            DialogueEnded?.Invoke();
        }
    }
}
