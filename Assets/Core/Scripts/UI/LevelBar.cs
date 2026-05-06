using TMPro;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class LevelBar : MonoBehaviour
{
    [SerializeField] private Slider _bar;
    [SerializeField] private TextMeshProUGUI _level;
    [SerializeField] private Animator _animator;
    [SerializeField, Min(0f)] private float _visibleDurationSeconds = 2.5f;
    [SerializeField] private NecromancerProgressionContext _progressionContext;

    private NecromancerProgressionBank _progressionBank;
    private Coroutine _hideRoutine;
    private float _nextRebindAttemptTime;

    private static readonly int OnActivationHash = Animator.StringToHash("OnActivation");
    private static readonly int OnDeactivationHash = Animator.StringToHash("OnDeactivation");

    private void Awake()
    {
        ResolveDependencies();
    }

    private void OnEnable()
    {
        ResolveDependencies();
        BindToProgression();

        Debug.Log("Refresh from Enable");
        RefreshUI();
    }

    private void OnDisable()
    {
        UnbindFromProgression();
        StopHideRoutine();
    }

    private void Start()
    {
        TryRebindIfNeeded();
    }

    private void Update()
    {
        TryRebindIfNeeded();
    }

    public void UpdateLevel(int currentLVL, int currentXP, int XPToNextLevel = 100)
    {
        _level.text = currentLVL.ToString();
        _bar.value = currentXP;
        _bar.maxValue = XPToNextLevel;
    }

    private void HandleProgressionChanged(NecromancerProgressionSnapshot snapshot, int _)
    {
        int xpToNextLevel = snapshot.HasReachedMaxLevel ? 1 : snapshot.ExperienceRequiredForNextLevel;
        UpdateLevel(snapshot.CurrentLevel, snapshot.ExperienceIntoCurrentLevel, xpToNextLevel);
    }

    private void HandleXPGained(int _)
    {
        if (_animator == null)
        {
            return;
        }

        _animator.SetTrigger(OnActivationHash);
        RestartHideRoutine();
    }

    public void RefreshUI()
    {
        Debug.Log("Refresh");
        if (_progressionBank == null)
            return;

        NecromancerProgressionProfile profile = _progressionContext != null ? _progressionContext.Profile : null;
        NecromancerProgressionSnapshot snapshot = _progressionBank.GetSnapshot(profile);
        HandleProgressionChanged(snapshot, 0);
    }

    private void ResolveDependencies()
    {
        _animator ??= GetComponent<Animator>();
        _progressionContext ??= GetComponentInParent<NecromancerProgressionContext>();
        _progressionContext ??= NecromancerProgressionContext.Current;
    }

    private void BindToProgression()
    {
        NecromancerProgressionBank nextBank = _progressionContext != null ? _progressionContext.ProgressionBank : null;
        if (ReferenceEquals(_progressionBank, nextBank))
            return;

        UnbindFromProgression();
        _progressionBank = nextBank;

        if (_progressionBank == null)
        {
            return;
        }

        _progressionBank.OnProgressionChanged += HandleProgressionChanged;
        _progressionBank.OnXPGained += HandleXPGained;
    }

    private void TryRebindIfNeeded()
    {
        if (_progressionBank != null || Time.unscaledTime < _nextRebindAttemptTime)
            return;

        _nextRebindAttemptTime = Time.unscaledTime + 1f;
        ResolveDependencies();
        BindToProgression();

        Debug.Log("Refresh from Rebind");
        RefreshUI();
    }

    private void UnbindFromProgression()
    {
        if (_progressionBank == null)
            return;

        _progressionBank.OnProgressionChanged -= HandleProgressionChanged;
        _progressionBank.OnXPGained -= HandleXPGained;
        _progressionBank = null;
    }

    private void RestartHideRoutine()
    {
        StopHideRoutine();
        _hideRoutine = StartCoroutine(DeactivateAfterDelay());
    }

    private void StopHideRoutine()
    {
        if (_hideRoutine == null)
            return;

        StopCoroutine(_hideRoutine);
        _hideRoutine = null;
    }

    private IEnumerator DeactivateAfterDelay()
    {
        if (_visibleDurationSeconds > 0f)
            yield return new WaitForSeconds(_visibleDurationSeconds);

        if (_animator != null)
        {
            _animator.SetTrigger(OnDeactivationHash);
        }

        _hideRoutine = null;
    }
}
