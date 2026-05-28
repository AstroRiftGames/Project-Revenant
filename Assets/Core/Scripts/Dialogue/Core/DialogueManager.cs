using System;
using System.Collections.Generic;
using UnityEngine;
using Dialogue.Data;

namespace Dialogue.Core
{
    [DisallowMultipleComponent]
    public class DialogueManager : MonoBehaviour
    {
        public static DialogueManager Instance { get; private set; }

        public event Action<DialogueConversation> OnDialogueStarted;
        public event Action<DialogueLine> OnLineAdvanced;
        public event Action OnDialogueEnded;
        public event Action<string> OnDialogueEventFired;

        private DialogueConversation _currentConversation;
        private int _currentLineIndex;
        private Action _onDialogueCompleteCallback;
        private GameState _stateBeforeDialogue = GameState.SafeZone;

        public bool IsDialogueActive { get; private set; }
        public DialogueConversation CurrentConversation => _currentConversation;
        public int CurrentLineIndex => _currentLineIndex;

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
            }
        }

        /// <summary>
        /// Starts a new dialogue sequence.
        /// </summary>
        /// <param name="conversation">The ScriptableObject conversation to play.</param>
        /// <param name="onComplete">Optional callback that runs when the dialogue fully closes.</param>
        public void StartDialogue(DialogueConversation conversation, Action onComplete = null)
        {
            if (conversation == null)
            {
                Debug.LogWarning("[DialogueManager] Attempted to start dialogue with a null conversation.");
                return;
            }

            if (IsDialogueActive)
            {
                Debug.LogWarning("[DialogueManager] Dialogue is already active! Can't start new one.");
                return;
            }

            IsDialogueActive = true;
            _currentConversation = conversation;
            _currentLineIndex = 0;
            _onDialogueCompleteCallback = onComplete;

            // Save state to restore it afterwards
            if (GameManager.Instance != null && GameManager.Instance.StateManager != null)
            {
                _stateBeforeDialogue = GameManager.Instance.StateManager.CurrentState;
                GameManager.Instance.RequestStateChange(GameState.Dialogue);
            }

            OnDialogueStarted?.Invoke(_currentConversation);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.RequestShowUI(UIType.Dialogue);
            }

            DisplayCurrentLine();
        }

        /// <summary>
        /// Attempts to advance the current active dialogue to the next line.
        /// </summary>
        public void AdvanceDialogue()
        {
            if (!IsDialogueActive) return;

            // Increment line index
            _currentLineIndex++;
            DisplayCurrentLine();
        }

        /// <summary>
        /// Ends the current dialogue, triggering cleanups, events, and restoring the player state.
        /// </summary>
        public void EndDialogue()
        {
            if (!IsDialogueActive) return;

            // Fire generic conversation completion event
            if (_currentConversation != null && !string.IsNullOrEmpty(_currentConversation.CompletionEventId))
            {
                TriggerDialogueEvent(_currentConversation.CompletionEventId);
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.RequestHideUI(UIType.Dialogue);

                // Restore previous state cleanly
                if (_stateBeforeDialogue == GameState.Dialogue || _stateBeforeDialogue == GameState.MainMenu)
                {
                    GameManager.Instance.RequestStateChange(GameState.SafeZone);
                }
                else
                {
                    GameManager.Instance.RequestStateChange(_stateBeforeDialogue);
                }
            }

            IsDialogueActive = false;
            _currentConversation = null;
            _currentLineIndex = 0;

            var callback = _onDialogueCompleteCallback;
            _onDialogueCompleteCallback = null;
            callback?.Invoke();

            OnDialogueEnded?.Invoke();
        }

        private void DisplayCurrentLine()
        {
            if (_currentConversation == null) return;

            if (_currentLineIndex < _currentConversation.Lines.Count)
            {
                OnLineAdvanced?.Invoke(_currentConversation.Lines[_currentLineIndex]);
            }
            else
            {
                // Reached the end of lines, close dialogue directly since there are no branching choices
                EndDialogue();
            }
        }

        private void TriggerDialogueEvent(string eventId)
        {
            if (string.IsNullOrEmpty(eventId)) return;

            Debug.Log($"[DialogueManager] Firing dialogue event key: '{eventId}'");
            OnDialogueEventFired?.Invoke(eventId);
        }
    }
}
