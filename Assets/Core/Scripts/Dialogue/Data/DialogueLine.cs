using System;
using UnityEngine;

namespace Dialogue.Data
{
    public enum DialoguePosition
    {
        Left,
        Right,
        None
    }

    [Serializable]
    public class DialogueLine
    {
        [Tooltip("Name of the speaker shown in the UI.")]
        [SerializeField] private string _speakerName;

        [Tooltip("The portrait sprite for the speaker.")]
        [SerializeField] private Sprite _speakerPortrait;

        [Tooltip("Where to show the speaker portrait on screen.")]
        [SerializeField] private DialoguePosition _portraitPosition = DialoguePosition.Left;

        [Tooltip("The text to type out.")]
        [TextArea(3, 10)]
        [SerializeField] private string _text;

        [Tooltip("Optional: Sound played during typewriter typing for this speaker.")]
        [SerializeField] private AudioClip _voiceClickSound;

        public string SpeakerName => _speakerName;
        public Sprite SpeakerPortrait => _speakerPortrait;
        public DialoguePosition PortraitPosition => _portraitPosition;
        public string Text => _text;
        public AudioClip VoiceClickSound => _voiceClickSound;
    }
}
