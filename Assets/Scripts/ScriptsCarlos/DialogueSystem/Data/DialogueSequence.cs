using System.Collections.Generic;
using UnityEngine;

namespace DialogueSystem
{
    [CreateAssetMenu(fileName = "DialogueSequence", menuName = "Dialogue/Sequence")]
    public sealed class DialogueSequence : ScriptableObject
    {
        [SerializeField] private List<DialogueLine> lines = new List<DialogueLine>();
        public IReadOnlyList<DialogueLine> Lines => lines;
    }
}
