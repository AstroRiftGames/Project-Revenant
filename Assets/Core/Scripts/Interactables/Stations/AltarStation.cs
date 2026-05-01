using UnityEngine;

public class AltarStation : BaseStation
{
    // Aquí se implementará el sistema específico del Altar más adelante.

    protected override void OnInteract()
    {
        base.OnInteract();
        // Puedes agregar lógica que ocurre al instante de interactuar,
        // además de abrir la UI.
        Debug.Log("Interactuando con el Altar...");
    }
}
