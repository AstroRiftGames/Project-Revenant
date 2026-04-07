using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LifeUpdateText : MonoBehaviour
{

    public void Initialize(int update)
    {
        TryGetComponent(out TextMeshProUGUI tmpro);
        tmpro.text = update.ToString();
        tmpro.color = update < 0 ? Color.red : Color.green;
        SetRandomRotation();
    }

    private void SetRandomRotation()
    {
        float randomZRotation = Random.Range(-15f, 15f);
        transform.rotation = Quaternion.Euler(0, 0, randomZRotation);
    }

    private void End()
    {
        Destroy(gameObject);
    }
}