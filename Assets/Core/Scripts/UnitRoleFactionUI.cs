using UnityEngine;
using UnityEngine.UI;

public class UnitRoleFactionUI : MonoBehaviour
{
    [SerializeField] private Image _backgroundImage;
    [SerializeField] private Image _roleImage;
    [SerializeField] private Data.GameIconDatabase _iconDatabase;

    public void Initialize(UnitData unitData)
    {
        if (unitData == null) return;

        if (_iconDatabase == null)
        {
            _iconDatabase = Data.GameIconDatabase.LoadFromResources();
            if (_iconDatabase == null)
            {
                Debug.LogWarning($"[{nameof(UnitRoleFactionUI)}] GameIconDatabase is not assigned and could not be loaded from Resources.", this);
                return;
            }
        }

        if (_backgroundImage != null)
        {
            var (_, factionColor) = _iconDatabase.GetFactionIcon(unitData.faction);
            _backgroundImage.color = factionColor;
        }

        if (_roleImage != null)
        {
            var (roleSprite, _) = _iconDatabase.GetRoleIcon(unitData.role);
            _roleImage.sprite = roleSprite;
        }
    }
}
