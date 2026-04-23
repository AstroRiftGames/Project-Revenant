using System;
using UnityEngine;
using PrefabDungeonGeneration;

[Serializable]
public struct RoomTypeSpriteMapping
{
    public PDRoomType roomType;
    public Sprite doorSprite;
}

[RequireComponent(typeof(SpriteRenderer))]
public class DoorVisuals : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private SpriteRenderer _spriteRenderer;

    [Header("Settings")]
    [SerializeField] private RoomTypeSpriteMapping[] _doorSpriteMappings;

    private void Awake()
    {
        if (_spriteRenderer == null)
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }

    /// <summary>
    /// Cambia el sprite de la puerta basado en el tipo de sala con la que conecta.
    /// </summary>
    /// <param name="targetRoomType">El tipo de sala de destino.</param>
    public void UpdateDoorSprite(PDRoomType targetRoomType)
    {
        if (_spriteRenderer == null)
        {
            Debug.LogError($"SpriteRenderer no asignado en {gameObject.name}.");
            return;
        }

        foreach (var mapping in _doorSpriteMappings)
        {
            if (mapping.roomType == targetRoomType)
            {
                _spriteRenderer.sprite = mapping.doorSprite;
                return;
            }
        }

        Debug.LogWarning($"No se encontró un sprite para el tipo de sala {targetRoomType} en {gameObject.name}.");
    }
}
