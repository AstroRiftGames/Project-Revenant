using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;

namespace UI.KillLog
{
    [RequireComponent(typeof(CanvasGroup))]
    public class KillLogEntry : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Image _subjectIcon;
        [SerializeField] private Image _mediumIcon;
        [SerializeField] private Image _targetIcon;
        
        [Header("Fading")]
        [SerializeField] private CanvasGroup _canvasGroup;

        [Header("Animation")]
        [SerializeField] private float _slideInDuration = 0.25f;
        
        private RectTransform _rectTransform;
        private LayoutElement _layoutElement;
        private float _targetHeight;
        private float _timeBeforeFade;
        private float _fadeDuration;
        private Coroutine _fadeCoroutine;

        public event Action<KillLogEntry> OnFadeComplete;

        private void Awake()
        {
            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
            }

            _rectTransform = GetComponent<RectTransform>();
            _layoutElement = GetComponent<LayoutElement>();
            
            _targetHeight = _rectTransform.rect.height;
        }

        public void Initialize(Sprite subject, Sprite medium, Sprite target, float delay, float fadeDur)
        {
            if (_subjectIcon != null)
            {
                _subjectIcon.sprite = subject;
                _subjectIcon.enabled = subject != null;
            }

            if (_mediumIcon != null)
            {
                _mediumIcon.sprite = medium;
                _mediumIcon.enabled = medium != null;
            }

            if (_targetIcon != null)
            {
                _targetIcon.sprite = target;
                _targetIcon.enabled = target != null;
            }

            _timeBeforeFade = delay;
            _fadeDuration = fadeDur;
            
            StartCoroutine(SlideInRoutine());
        }

        private IEnumerator SlideInRoutine()
        {
            _layoutElement.minHeight = 0f;
            _layoutElement.preferredHeight = 0f;
            _canvasGroup.alpha = 0f;
            
            Vector2 initialSize = _rectTransform.sizeDelta;

            float timePassed = 0f;
            while (timePassed < _slideInDuration)
            {
                timePassed += Time.deltaTime;
                float progress = timePassed / _slideInDuration;
                
                float easeOutProgress = 1f - Mathf.Pow(1f - progress, 3f);
                float currentHeight = Mathf.Lerp(0f, _targetHeight, easeOutProgress);
                
                _layoutElement.minHeight = currentHeight;
                _layoutElement.preferredHeight = currentHeight;
                _rectTransform.sizeDelta = new Vector2(initialSize.x, currentHeight);
                
                _canvasGroup.alpha = Mathf.Lerp(0f, 1f, progress);
                
                yield return null;
            }

            _layoutElement.minHeight = _targetHeight;
            _layoutElement.preferredHeight = _targetHeight;
            _rectTransform.sizeDelta = new Vector2(initialSize.x, _targetHeight);
            _canvasGroup.alpha = 1f;

            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
            }
            _fadeCoroutine = StartCoroutine(FadeOutRoutine());
        }

        public void ForceFadeImmediate()
        {
            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
            }
            _canvasGroup.alpha = 0f;
            OnFadeComplete?.Invoke(this);
        }

        private IEnumerator FadeOutRoutine()
        {
            yield return new WaitForSeconds(_timeBeforeFade);

            float timePassed = 0f;
            while (timePassed < _fadeDuration)
            {
                timePassed += Time.deltaTime;
                _canvasGroup.alpha = Mathf.Lerp(1f, 0f, timePassed / _fadeDuration);
                yield return null;
            }

            _canvasGroup.alpha = 0f;
            OnFadeComplete?.Invoke(this);
        }
    }
}
