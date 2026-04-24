using System;
using UnityEngine;

public class FusionStation : BaseStation
{
    // La lógica de IInteractable (OnEnable, OnDisable, Interact) ahora está en BaseStation.
    // El FusionController ya escucha el evento OnInteraction, por lo que no hace falta más código aquí.
    // Podrías usar OnInteract() si necesitaras lógica extra antes de disparar el evento.
}

