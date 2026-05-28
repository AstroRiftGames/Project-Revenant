using System.Collections.Generic;
using UnityEngine;

namespace Dialogue.Data
{
    [CreateAssetMenu(fileName = "NewConversation", menuName = "Dialogue/Conversation", order = 1)]
    public class DialogueConversation : ScriptableObject
    {
        [Tooltip("The sequential list of dialogue lines for this conversation.")]
        [SerializeField] private List<DialogueLine> _lines = new List<DialogueLine>();

        [Tooltip("Optional event key to trigger when this conversation completes.")]
        [SerializeField] private string _completionEventId;

        public IReadOnlyList<DialogueLine> Lines => _lines;
        public string CompletionEventId => _completionEventId;
    }
}
