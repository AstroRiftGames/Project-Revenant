using UnityEngine;

[CreateAssetMenu(fileName = "NecromancerData", menuName = "Player/Necromancer Data")]
public class NecromancerData : ScriptableObject
{
    [SerializeField] private string _necromancerId = "Necromancer";
    [SerializeField] private string _displayName = "Necromancer";
    [SerializeField] private Sprite _sprite;
    [SerializeField] private float _moveSpeed = 5f;

    public string NecromancerId => _necromancerId;
    public string DisplayName => string.IsNullOrWhiteSpace(_displayName) ? name : _displayName;
    public Sprite Sprite => _sprite;
    public float MoveSpeed => Mathf.Max(0f, _moveSpeed);
}
