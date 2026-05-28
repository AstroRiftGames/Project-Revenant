using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Dialogue.Data;
using Dialogue.Core;

namespace Dialogue.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class DialogueUIController : MonoBehaviour
    {
        [Header("General UI Elements")]
        [SerializeField] private CanvasGroup _dialogueCanvasGroup;
        [SerializeField] private TMP_Text _speakerNameText;
        [SerializeField] private TMP_Text _dialogueText;
        [SerializeField] private Button _backgroundClickButton;

        [Header("Portraits Left")]
        [SerializeField] private RectTransform _leftPortraitContainer;
        [SerializeField] private Image _leftPortraitImage;

        [Header("Portraits Right")]
        [SerializeField] private RectTransform _rightPortraitContainer;
        [SerializeField] private Image _rightPortraitImage;

        [Header("Aesthetic Settings")]
        [SerializeField] private float _typewriterSpeed = 0.02f;
        [SerializeField] private float _fadeDuration = 0.25f;
        [SerializeField] private float _portraitSlideOffset = 40f;
        [SerializeField] private float _portraitAnimSpeed = 8f;
        [SerializeField] private Color _inactivePortraitColor = new Color(0.4f, 0.4f, 0.4f, 0.6f);
        [SerializeField] private Vector3 _activePortraitScale = new Vector3(1.05f, 1.05f, 1.05f);
        [SerializeField] private Vector3 _inactivePortraitScale = new Vector3(0.95f, 0.95f, 0.95f);

        [Header("Audio")]
        [SerializeField] private AudioSource _audioSource;

        private Coroutine _typeCoroutine;
        private Coroutine _fadeCoroutine;

        private string _currentLineText = string.Empty;
        private bool _isTyping = false;

        // Portrait animation state variables
        private Vector3 _leftStartLocalPos;
        private Vector3 _rightStartLocalPos;

        private Vector3 _leftTargetPos;
        private Vector3 _rightTargetPos;
        private Vector3 _leftTargetScale;
        private Vector3 _rightTargetScale;
        private Color _leftTargetColor;
        private Color _rightTargetColor;

        private void Awake()
        {
            if (_dialogueCanvasGroup == null)
                _dialogueCanvasGroup = GetComponent<CanvasGroup>();

            // Hide UI initially
            _dialogueCanvasGroup.alpha = 0f;
            _dialogueCanvasGroup.blocksRaycasts = false;
            _dialogueCanvasGroup.interactable = false;

            // Save portrait baseline positions for sliding math
            if (_leftPortraitContainer != null)
            {
                _leftStartLocalPos = _leftPortraitContainer.localPosition;
                _leftTargetPos = _leftStartLocalPos;
                _leftTargetScale = _inactivePortraitScale;
                _leftTargetColor = Color.clear;
            }

            if (_rightPortraitContainer != null)
            {
                _rightStartLocalPos = _rightPortraitContainer.localPosition;
                _rightTargetPos = _rightStartLocalPos;
                _rightTargetScale = _inactivePortraitScale;
                _rightTargetColor = Color.clear;
            }
        }

        private void Start()
        {
            // Subscribe to DialogueManager events
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.OnDialogueStarted += HandleDialogueStarted;
                DialogueManager.Instance.OnLineAdvanced += HandleLineAdvanced;
                DialogueManager.Instance.OnDialogueEnded += HandleDialogueEnded;
            }

            if (_backgroundClickButton != null)
            {
                _backgroundClickButton.onClick.AddListener(OnDialoguePanelPressed);
            }
        }

        private void OnDestroy()
        {
            // Unsubscribe to prevent memory leaks
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.OnDialogueStarted -= HandleDialogueStarted;
                DialogueManager.Instance.OnLineAdvanced -= HandleLineAdvanced;
                DialogueManager.Instance.OnDialogueEnded -= HandleDialogueEnded;
            }
        }

        private void Update()
        {
            // Smoothly animate portrait positions, scales, and colors for professional high-fidelity feedback
            if (_leftPortraitContainer != null && _leftPortraitImage != null)
            {
                _leftPortraitContainer.localPosition = Vector3.Lerp(_leftPortraitContainer.localPosition, _leftTargetPos, Time.deltaTime * _portraitAnimSpeed);
                _leftPortraitContainer.localScale = Vector3.Lerp(_leftPortraitContainer.localScale, _leftTargetScale, Time.deltaTime * _portraitAnimSpeed);
                _leftPortraitImage.color = Color.Lerp(_leftPortraitImage.color, _leftTargetColor, Time.deltaTime * _portraitAnimSpeed);
            }

            if (_rightPortraitContainer != null && _rightPortraitImage != null)
            {
                _rightPortraitContainer.localPosition = Vector3.Lerp(_rightPortraitContainer.localPosition, _rightTargetPos, Time.deltaTime * _portraitAnimSpeed);
                _rightPortraitContainer.localScale = Vector3.Lerp(_rightPortraitContainer.localScale, _rightTargetScale, Time.deltaTime * _portraitAnimSpeed);
                _rightPortraitImage.color = Color.Lerp(_rightPortraitImage.color, _rightTargetColor, Time.deltaTime * _portraitAnimSpeed);
            }
        }

        /// <summary>
        /// Triggered when the player clicks the dialogue panel to skip typing or advance.
        /// </summary>
        public void OnDialoguePanelPressed()
        {
            if (_isTyping)
            {
                // Skip the typewriter and show full text instantly
                if (_typeCoroutine != null) StopCoroutine(_typeCoroutine);
                _dialogueText.text = _currentLineText;
                _isTyping = false;
            }
            else
            {
                // Proceed to next line
                DialogueManager.Instance.AdvanceDialogue();
            }
        }

        private void HandleDialogueStarted(DialogueConversation conversation)
        {
            // Reset portraits
            ResetPortraitsAnimationState();

            // Fade in Dialogue screen smoothly
            StartFade(0f, 1f, true);
        }

        private void HandleLineAdvanced(DialogueLine line)
        {
            _currentLineText = line.Text;
            _speakerNameText.text = line.SpeakerName;

            // Handle portrait visual adjustments (sliding & dimming based on speaker focus)
            UpdateSpeakerPortraits(line);

            // Start typing typewriter text
            if (_typeCoroutine != null) StopCoroutine(_typeCoroutine);
            _typeCoroutine = StartCoroutine(TypeTextCoroutine(_currentLineText, line.VoiceClickSound));
        }

        private void HandleDialogueEnded()
        {
            if (_typeCoroutine != null) StopCoroutine(_typeCoroutine);
            _isTyping = false;

            // Fade out Dialogue screen smoothly
            StartFade(_dialogueCanvasGroup.alpha, 0f, false);
        }

        private void UpdateSpeakerPortraits(DialogueLine line)
        {
            Sprite portrait = line.SpeakerPortrait;
            DialoguePosition position = line.PortraitPosition;

            if (portrait == null)
            {
                // No portrait specified, dim or hide existing portraits
                _leftTargetColor = Color.clear;
                _rightTargetColor = Color.clear;
                return;
            }

            if (position == DialoguePosition.Left)
            {
                // Left active focus
                if (_leftPortraitImage != null)
                {
                    _leftPortraitImage.sprite = portrait;
                }

                _leftTargetPos = _leftStartLocalPos + new Vector3(_portraitSlideOffset, 0, 0);
                _leftTargetScale = _activePortraitScale;
                _leftTargetColor = Color.white;

                // Dim right portrait if it is currently visible
                if (_rightPortraitImage != null && _rightPortraitImage.sprite != null)
                {
                    _rightTargetPos = _rightStartLocalPos;
                    _rightTargetScale = _inactivePortraitScale;
                    _rightTargetColor = _inactivePortraitColor;
                }
                else
                {
                    _rightTargetColor = Color.clear;
                }
            }
            else if (position == DialoguePosition.Right)
            {
                // Right active focus
                if (_rightPortraitImage != null)
                {
                    _rightPortraitImage.sprite = portrait;
                }

                _rightTargetPos = _rightStartLocalPos - new Vector3(_portraitSlideOffset, 0, 0);
                _rightTargetScale = _activePortraitScale;
                _rightTargetColor = Color.white;

                // Dim left portrait if it is currently visible
                if (_leftPortraitImage != null && _leftPortraitImage.sprite != null)
                {
                    _leftTargetPos = _leftStartLocalPos;
                    _leftTargetScale = _inactivePortraitScale;
                    _leftTargetColor = _inactivePortraitColor;
                }
                else
                {
                    _leftTargetColor = Color.clear;
                }
            }
            else
            {
                // Position is None: Dim both if they exist, or just clear focus
                _leftTargetPos = _leftStartLocalPos;
                _rightTargetPos = _rightStartLocalPos;
                _leftTargetScale = _inactivePortraitScale;
                _rightTargetScale = _inactivePortraitScale;
                _leftTargetColor = _leftPortraitImage.sprite != null ? _inactivePortraitColor : Color.clear;
                _rightTargetColor = _rightPortraitImage.sprite != null ? _inactivePortraitColor : Color.clear;
            }
        }

        private IEnumerator TypeTextCoroutine(string fullText, AudioClip voiceSound)
        {
            _dialogueText.text = "";
            _isTyping = true;
            int charIndex = 0;

            while (charIndex < fullText.Length)
            {
                char c = fullText[charIndex];
                if (c == '<')
                {
                    // Catch and process TMPro rich text tags instantly without typing them out
                    int closeTagIndex = fullText.IndexOf('>', charIndex);
                    if (closeTagIndex != -1)
                    {
                        _dialogueText.text += fullText.Substring(charIndex, closeTagIndex - charIndex + 1);
                        charIndex = closeTagIndex + 1;
                        continue;
                    }
                }

                _dialogueText.text += c;
                charIndex++;

                // Optional modulated typewriter audio beep
                if (_audioSource != null && voiceSound != null)
                {
                    _audioSource.pitch = UnityEngine.Random.Range(0.85f, 1.15f);
                    _audioSource.PlayOneShot(voiceSound, 0.2f);
                }

                yield return new WaitForSeconds(_typewriterSpeed);
            }

            _dialogueText.text = fullText;
            _isTyping = false;
        }

        private void StartFade(float startAlpha, float endAlpha, bool enableInteractions)
        {
            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(FadeCanvasCoroutine(startAlpha, endAlpha, enableInteractions));
        }

        private IEnumerator FadeCanvasCoroutine(float startAlpha, float endAlpha, bool enableInteractions)
        {
            float elapsed = 0f;
            _dialogueCanvasGroup.alpha = startAlpha;

            if (enableInteractions)
            {
                _dialogueCanvasGroup.blocksRaycasts = true;
                _dialogueCanvasGroup.interactable = true;
            }
            else
            {
                _dialogueCanvasGroup.blocksRaycasts = false;
                _dialogueCanvasGroup.interactable = false;
            }

            while (elapsed < _fadeDuration)
            {
                elapsed += Time.deltaTime;
                _dialogueCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / _fadeDuration);
                yield return null;
            }

            _dialogueCanvasGroup.alpha = endAlpha;
        }

        private void ResetPortraitsAnimationState()
        {
            if (_leftPortraitContainer != null)
            {
                _leftPortraitContainer.localPosition = _leftStartLocalPos;
                _leftPortraitContainer.localScale = _inactivePortraitScale;
            }

            if (_rightPortraitContainer != null)
            {
                _rightPortraitContainer.localPosition = _rightStartLocalPos;
                _rightPortraitContainer.localScale = _inactivePortraitScale;
            }

            if (_leftPortraitImage != null)
            {
                _leftPortraitImage.sprite = null;
                _leftPortraitImage.color = Color.clear;
            }

            if (_rightPortraitImage != null)
            {
                _rightPortraitImage.sprite = null;
                _rightPortraitImage.color = Color.clear;
            }

            _leftTargetColor = Color.clear;
            _rightTargetColor = Color.clear;
        }
    }
}
