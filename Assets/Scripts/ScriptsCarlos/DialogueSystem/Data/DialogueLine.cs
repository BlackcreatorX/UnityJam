using System;
using UnityEngine;

namespace DialogueSystem
{
    [Serializable]
    public sealed class DialogueLine
    {
        [TextArea(2, 5)] [SerializeField] private string text;
        [SerializeField] private SpeakerData speaker;
        [Min(0)] [SerializeField] private int spriteIndex;
        [SerializeField] private ParticleSystem particlePrefab;

        public string Text => text;
        public SpeakerData Speaker => speaker;
        public int SpriteIndex => spriteIndex;
        public ParticleSystem ParticlePrefab => particlePrefab;
    }
}
