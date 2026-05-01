using UnityEngine;
using UnityEngine.UI;

public abstract class StationUIManager : MonoBehaviour
{
    [Header("Base Station UI")]
    [Tooltip("El tipo de UI de este panel (ej. Altar, Shop). Requerido para estaciones que se instancian en tiempo de ejecución.")]
    [SerializeField] protected UIType _myUIType;

    [Tooltip("Controlador de la estación. Puede estar vacío si la estación se genera dinámicamente.")]
    [SerializeField] protected StationController _stationController;
    
    [Tooltip("Panel principal de la estación.")]
    [SerializeField] protected GameObject _mainPanel;

    [Tooltip("Botón para cerrar el panel principal.")]
    [SerializeField] protected Button _closeMainButton;

    protected BaseStation _currentActiveStation;
    protected StationController _currentActiveController;

    protected virtual void Awake()
    {
        if (_mainPanel != null)
        {
            _mainPanel.SetActive(false);
        }
    }

    protected virtual void OnEnable()
    {
        BaseStation.OnStationUIRequestedGlobal += HandleGlobalUIRequest;

        if (_stationController != null)
        {
            _stationController.OnUIRequested += OpenMainPanel;
        }

        if (_closeMainButton != null)
        {
            _closeMainButton.onClick.AddListener(CloseAllPanels);
        }
    }

    protected virtual void OnDisable()
    {
        BaseStation.OnStationUIRequestedGlobal -= HandleGlobalUIRequest;

        if (_stationController != null)
        {
            _stationController.OnUIRequested -= OpenMainPanel;
        }

        if (_closeMainButton != null)
        {
            _closeMainButton.onClick.RemoveListener(CloseAllPanels);
        }
    }

    protected virtual void HandleGlobalUIRequest(BaseStation station, UIType uiType)
    {
        if (uiType == _myUIType)
        {
            _currentActiveStation = station;
            
            // Intentamos obtener el controlador de esta estación específica
            _currentActiveController = station.GetComponent<StationController>();
            
            OpenMainPanel();
        }
    }

    /// <summary>
    /// Abre el panel principal. Puede ser sobreescrito para lógica adicional.
    /// </summary>
    protected virtual void OpenMainPanel()
    {
        if (_mainPanel != null)
        {
            _mainPanel.SetActive(true);
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.HideGameplayUI();
        }
    }

    /// <summary>
    /// Cierra el panel principal y oculta el elemento en el UIManager.
    /// </summary>
    protected virtual void CloseAllPanels()
    {
        if (_mainPanel != null)
        {
            _mainPanel.SetActive(false);
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.HideElement(_myUIType);
            UIManager.Instance.ShowGameplayUI();
        }
    }
}
