using UnityEngine;
using UnityEngine.UI;
using PrefabDungeonGeneration;

namespace ProjectRevenant.UI
{
    public class MinimapRoomNodeUI : MonoBehaviour
    {
        public Image BackgroundImage;
        public Image HighlightImage; 
        
        public int RoomID { get; private set; }

        public void Initialize(PDRoomNode roomNode)
        {
            RoomID = roomNode.ID;
            
            if (BackgroundImage != null)
            {
                BackgroundImage.color = GetColorForRoomType(roomNode.RoomType);
            }

            SetIsCurrentRoom(false);
        }

        public void SetIsCurrentRoom(bool isCurrent)
        {
            if (HighlightImage != null)
            {
                HighlightImage.enabled = isCurrent;
            }
            else
            {
                if (BackgroundImage != null)
                {
                    var color = BackgroundImage.color;
                    color.a = isCurrent ? 1f : 0.5f;
                    BackgroundImage.color = color;
                    
                    // We also make it a bit larger if it's current, just in case there's no highlight image
                    transform.localScale = isCurrent ? Vector3.one * 1.5f : Vector3.one;
                }
            }
        }

        private Color GetColorForRoomType(PDRoomType type)
        {
            switch (type)
            {
                case PDRoomType.Start: return Color.green;
                case PDRoomType.Combat: return Color.red;
                case PDRoomType.Loot: return Color.yellow;
                case PDRoomType.Shop: return Color.blue;
                case PDRoomType.Altar: return Color.cyan;
                case PDRoomType.MiniBoss: return new Color(1f, 0.5f, 0f); // Naranja
                case PDRoomType.Boss: return new Color(0.5f, 0f, 0.5f); // Morado
                default: return Color.white;
            }
        }
    }
}
