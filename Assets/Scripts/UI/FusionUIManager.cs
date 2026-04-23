using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FusionUIManager : StationUIManager
{
    // Acceso tipado al controlador específico de fusión
    private FusionController FusionController => (_currentActiveController as FusionController) ?? (_stationController as FusionController);

    [Header("Panels Adicionales")]
    [SerializeField] private GameObject _selectionPanel;
    [SerializeField] private GameObject _resultPanel;

    [Header("Main Panel Elements")]
    [SerializeField] private Button _fuseButton;
    [SerializeField] private Button _slotAButton;
    [SerializeField] private Button _slotBButton;
    [SerializeField] private Image _slotAImage;
    [SerializeField] private Image _slotBImage;

    [Header("Selection Panel Elements")]
    [SerializeField] private Button _closeSelectionButton;
    [SerializeField] private Transform _gridContainer;
    [SerializeField] private CreatureFusionCard _cardPrefab;

    [Header("Result Panel Elements")]
    [SerializeField] private Button _closeResultButton;
    [SerializeField] private TextMeshProUGUI _resultTitleText;
    [SerializeField] private TextMeshProUGUI _resultDescText;
    [SerializeField] private CreatureFusionCard _resultCard;

    private PartyMemberData _memberA;
    private PartyMemberData _memberB;
    private int _selectingSlot = 0;

    private List<CreatureFusionCard> _spawnedCards = new List<CreatureFusionCard>();

    protected override void Awake()
    {
        base.Awake(); // Oculta el _mainPanel
        _selectionPanel?.SetActive(false);
        _resultPanel?.SetActive(false);
    }

    protected override void OnEnable()
    {
        base.OnEnable(); // Suscribe _closeMainButton y OpenMainPanel

        if (FusionController != null)
        {
            FusionController.OnFusionCompleted += ShowResultPanel;
        }

        _fuseButton?.onClick.AddListener(TryFuse);
        _slotAButton?.onClick.AddListener(() => OpenSelectionPanel(1));
        _slotBButton?.onClick.AddListener(() => OpenSelectionPanel(2));
        _closeSelectionButton?.onClick.AddListener(ReturnToMainPanel);
        _closeResultButton?.onClick.AddListener(ReturnToMainPanel);
    }

    protected override void OnDisable()
    {
        base.OnDisable(); // Desuscribe _closeMainButton y OpenMainPanel

        if (FusionController != null)
        {
            FusionController.OnFusionCompleted -= ShowResultPanel;
        }

        _fuseButton?.onClick.RemoveListener(TryFuse);
        _slotAButton?.onClick.RemoveAllListeners();
        _slotBButton?.onClick.RemoveAllListeners();
        _closeSelectionButton?.onClick.RemoveListener(ReturnToMainPanel);
        _closeResultButton?.onClick.RemoveListener(ReturnToMainPanel);
    }

    protected override void OpenMainPanel()
    {
        _memberA = null;
        _memberB = null;
        RefreshMainPanel();
        
        _selectionPanel.SetActive(false);
        _resultPanel.SetActive(false);

        base.OpenMainPanel(); // Activa el _mainPanel
    }

    protected override void CloseAllPanels()
    {
        base.CloseAllPanels(); // Oculta el _mainPanel
        _selectionPanel.SetActive(false);
        _resultPanel.SetActive(false);
    }

    private void ReturnToMainPanel()
    {
        _selectionPanel.SetActive(false);
        _resultPanel.SetActive(false);
        RefreshMainPanel();
        
        if (_mainPanel != null)
            _mainPanel.SetActive(true);
    }

    private void RefreshMainPanel()
    {
        if (_slotAImage != null)
        {
            _slotAImage.gameObject.SetActive(_memberA != null);
            if (_memberA != null) _slotAImage.sprite = _memberA.CharacterSprite;
        }

        if (_slotBImage != null)
        {
            _slotBImage.gameObject.SetActive(_memberB != null);
            if (_memberB != null) _slotBImage.sprite = _memberB.CharacterSprite;
        }

        if (_fuseButton != null)
        {
            _fuseButton.interactable = (_memberA != null && _memberB != null);
        }
    }

    private void OpenSelectionPanel(int slot)
    {
        _selectingSlot = slot;
        _mainPanel.SetActive(false);
        _selectionPanel.SetActive(true);
        PopulateGrid();
    }

    private void PopulateGrid()
    {
        foreach (var card in _spawnedCards)
        {
            if (card != null) Destroy(card.gameObject);
        }
        _spawnedCards.Clear();

        if (NecromancerParty.Instance == null) return;

        foreach (var member in NecromancerParty.Instance.Members)
        {
            if (member == null || !member.IsAlive) continue;
            if (member.UnitDefinition != null && member.UnitDefinition.isFusion) continue;

            bool isLocked = false;
            if (_selectingSlot == 1 && member == _memberB) isLocked = true;
            if (_selectingSlot == 2 && member == _memberA) isLocked = true;

            CreatureFusionCard newCard = Instantiate(_cardPrefab, _gridContainer);
            newCard.Setup(member, OnCardSelected, isLocked);
            _spawnedCards.Add(newCard);
        }
    }

    private void OnCardSelected(PartyMemberData member)
    {
        if (_selectingSlot == 1) _memberA = member;
        else if (_selectingSlot == 2) _memberB = member;

        ReturnToMainPanel();
    }

    private void TryFuse()
    {
        if (_memberA == null || _memberB == null) return;
        FusionController.ExecuteFusion(_memberA, _memberB);
    }

    private void ShowResultPanel(FusionResult result)
    {
        _mainPanel.SetActive(false);
        _selectionPanel.SetActive(false);
        _resultPanel.SetActive(true);

        if (result.IsSuccess)
        {
            if (_resultTitleText != null) _resultTitleText.text = "FUSION SUCCESFUL";
            if (_resultDescText != null) 
            {
                _resultDescText.text = string.Empty;
                _resultDescText.gameObject.SetActive(false);
            }
            if (_resultCard != null)
            {
                _resultCard.gameObject.SetActive(true);
                _resultCard.SetupVisualOnly(result.ResultCreature.Visual, $"Mutant {result.ResultCreature.UnitFaction}");
            }
        }
        else
        {
            if (_resultTitleText != null) _resultTitleText.text = "FUSION FAILED";
            if (_resultDescText != null)
            {
                _resultDescText.text = $"{result.RemainsAmount} Fusion Remains obtained.";
                _resultDescText.gameObject.SetActive(true);
            }
            if (_resultCard != null) _resultCard.gameObject.SetActive(false);
        }

        _memberA = null;
        _memberB = null;
    }
}
