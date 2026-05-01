using UnityEngine;

public class ShopStation : BaseStation
{
    // Aquí se implementará el sistema específico de la Tienda más adelante.

    protected override void OnInteract()
    {
        base.OnInteract();
        // Puedes agregar lógica que ocurre al instante de interactuar,
        // además de abrir la UI.
        Debug.Log("Interactuando con la Tienda...");
    }
}
