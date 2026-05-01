using System.Collections.Generic;
using UnityEngine;

namespace UI.KillLog
{
    public class KillLogManager : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private int _maxLogsLimit = 5;
        [SerializeField] private float _timeBeforeFade = 3f;
        [SerializeField] private float _fadeDuration = 1f;
        
        [Header("Default Assets")]
        [Tooltip("Used when a unit kills without a specific ability icon")]
        [SerializeField] private Sprite _defaultAttackIcon;

        [Header("References")]
        [SerializeField] private KillLogEntry _logEntryPrefab;
        [SerializeField] private RectTransform _logContainer;

        private readonly List<KillLogEntry> _activeLogs = new List<KillLogEntry>();

        private void OnEnable()
        {
            LifeController.OnUnitDied += HandleUnitDied;
        }

        private void OnDisable()
        {
            LifeController.OnUnitDied -= HandleUnitDied;
        }

        private void HandleUnitDied(Unit victim)
        {
            if (victim == null) return;

            Unit attacker = victim.GetLastAttacker();
            
            if (attacker == null) return; 

            Sprite subjectIcon = attacker.CharacterSprite;
            Sprite targetIcon = victim.CharacterSprite;
            
            Sprite mediumIcon = attacker.AbilityIcon != null ? attacker.AbilityIcon : _defaultAttackIcon;

            CreateKillLog(subjectIcon, mediumIcon, targetIcon);
        }

        private void CreateKillLog(Sprite subject, Sprite medium, Sprite target)
        {
            if (_logEntryPrefab == null || _logContainer == null) return;

            KillLogEntry newEntry = Instantiate(_logEntryPrefab, _logContainer);

            newEntry.transform.SetAsFirstSibling();
            
            newEntry.Initialize(subject, medium, target, _timeBeforeFade, _fadeDuration);
            newEntry.OnFadeComplete += HandleEntryFadeComplete;

            _activeLogs.Add(newEntry);

            EnforceLimit();
        }

        private void EnforceLimit()
        {
            while (_activeLogs.Count > _maxLogsLimit)
            {
                KillLogEntry oldest = _activeLogs[0];
                _activeLogs.RemoveAt(0);
                
                if (oldest != null)
                {
                    oldest.ForceFadeImmediate();
                }
            }
        }

        private void HandleEntryFadeComplete(KillLogEntry entry)
        {
            entry.OnFadeComplete -= HandleEntryFadeComplete;
            _activeLogs.Remove(entry);
            Destroy(entry.gameObject);
        }
    }
}
