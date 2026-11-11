using System.Collections.Generic;
using UnityEngine;

namespace DialogueSystem
{
    [CreateAssetMenu(fileName = "Speaker", menuName = "Dialogue/Speaker")]
    public sealed class SpeakerData : ScriptableObject
    {
        [SerializeField] private string speakerName;
        [SerializeField] private List<Sprite> expressions = new List<Sprite>();

        public string SpeakerName => speakerName;
        public IReadOnlyList<Sprite> Expressions => expressions;
    }
}
