using UnityEngine;

namespace DialogueSystem
{
    /// <summary>Example trigger: call Interact from the NPC interaction system.</summary>
    public sealed class DialogueTrigger : MonoBehaviour
    {
        [SerializeField] private DialogueManager dialogueManager;
        [SerializeField] private DialogueSequence dialogueSequence;

        public void Interact()
        {
            if (dialogueManager == null || dialogueSequence == null)
            {
                Debug.LogWarning("DialogueTrigger needs a manager and sequence assigned.", this);
                return;
            }

            dialogueManager.StartDialogue(dialogueSequence);
        }
    }
}
