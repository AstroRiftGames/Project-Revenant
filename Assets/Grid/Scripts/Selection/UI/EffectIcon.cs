using UnityEngine;
using UnityEngine.UI;

namespace Selection.UI
{
    public class EffectIcon : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image dimmer;
        private float duration = 1f;
        private float startTime = 0;

        public void Initialize(Sprite sprite, Color color, float timeInSeconds)
        {
            SetIcon(sprite);
            SetColor(color);
            duration = timeInSeconds;
            startTime = Time.time;
        }

        private void Update()
        {
            UpdateDimmer();
        }

        public void SetIcon(Sprite sprite)
        {
            if (iconImage != null)
            {
                iconImage.sprite = sprite;
                iconImage.enabled = sprite != null;
            }
        }

        public void SetColor(Color color)
        {
            if (backgroundImage != null)
            {
                backgroundImage.color = color;
            }
        }
        private void UpdateDimmer()
        {
            if (dimmer == null)
                return;
            if (duration > 0f)
            {
                dimmer.fillAmount = (Time.time - startTime) /duration;
            }
        }
    }
}
