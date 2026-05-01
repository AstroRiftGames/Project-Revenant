using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelBar : MonoBehaviour
{
    [SerializeField] private Slider _bar;
    [SerializeField] private TextMeshProUGUI _level;

    void OnEnable()
    {
        //TODO: Suscribir UpdateLevel a un evento que se dispare cuando el nivel del jugador cambie
    }
    void OnDisable()
    {
        //TODO: Desuscribir UpdateLevel al evento
    }

    private void UpdateLevel(int currentLVL, int currentXP, int XPToNextLevel = 100)
    {
        _level.text = currentLVL.ToString();
        _bar.value = currentXP;
        _bar.maxValue = XPToNextLevel;
    }

    [ContextMenu("Update Level")]
    public void TestUpdate()
    {
        UpdateLevel(5, Random.Range(0, 100), 100);
    }
}
