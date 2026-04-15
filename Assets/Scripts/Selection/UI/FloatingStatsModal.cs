using UnityEngine;
using TMPro;

namespace Selection.UI
{
    public class FloatingStatsModal : MonoBehaviour
    {
        public static FloatingStatsModal Instance { get; private set; }

        [Header("UI Text References")]
        [SerializeField] private TextMeshProUGUI attackDamageText;
        [SerializeField] private TextMeshProUGUI attackCooldownText;
        [SerializeField] private TextMeshProUGUI attackRangeText;
        [SerializeField] private TextMeshProUGUI preferredDistanceText;
        [SerializeField] private TextMeshProUGUI accuracyText;
        [SerializeField] private TextMeshProUGUI evasionText;
        [SerializeField] private TextMeshProUGUI moveSpeedText;
        [SerializeField] private TextMeshProUGUI visionRangeText;

        [Header("Settings")]
        [SerializeField] private Vector2 offset = new Vector2(15f, -15f);

        private RectTransform _rectTransform;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                _rectTransform = GetComponent<RectTransform>();
                Hide(); // Ocultar al inicio
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            if (gameObject.activeInHierarchy && _rectTransform != null)
            {
                UpdatePositionToMouse();
            }
        }

        public void Show(UnitStatsData stats)
        {
            if (stats == null) return;

            if (attackDamageText != null) attackDamageText.text = stats.attackDamage.ToString();
            if (attackCooldownText != null) attackCooldownText.text = $"{stats.attackCooldown:F2}s";
            if (attackRangeText != null) attackRangeText.text = stats.attackRangeInCells.ToString();
            if (preferredDistanceText != null) preferredDistanceText.text = stats.preferredDistanceInCells.ToString();
            if (accuracyText != null) accuracyText.text = $"{(stats.accuracy * 100f):F0}%";
            if (evasionText != null) evasionText.text = $"{(stats.evasion * 100f):F0}%";
            if (moveSpeedText != null) moveSpeedText.text = stats.moveSpeed.ToString("F1");
            if (visionRangeText != null) visionRangeText.text = stats.visionRange.ToString("F0");

            gameObject.SetActive(true);
            UpdatePositionToMouse();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void UpdatePositionToMouse()
        {
            Vector2 mousePos = Input.mousePosition;

            // Determina de qué lado de la pantalla está el mouse para invertir el pivot
            float pivotX = mousePos.x / Screen.width > 0.5f ? 1f : 0f;
            float pivotY = mousePos.y / Screen.height > 0.5f ? 1f : 0f;
            _rectTransform.pivot = new Vector2(pivotX, pivotY);

            // Invertimos el offset para alejarlo siempre del puntero en la dirección correcta
            Vector2 directionalOffset = new Vector2(
                pivotX == 1f ? -Mathf.Abs(offset.x) : Mathf.Abs(offset.x),
                pivotY == 1f ? -Mathf.Abs(offset.y) : Mathf.Abs(offset.y)
            );

            var parentCanvas = GetComponentInParent<Canvas>();
            if (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    (RectTransform)parentCanvas.transform,
                    mousePos,
                    parentCanvas.worldCamera,
                    out Vector2 localPoint
                );
                _rectTransform.localPosition = localPoint + directionalOffset;
            }
            else
            {
                // Screen Space Overlay
                _rectTransform.position = mousePos + (Vector2)_rectTransform.TransformVector(directionalOffset);
            }
        }
    }
}
