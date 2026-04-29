using System.Collections.Generic;
using UnityEngine;

namespace Altar.Data
{
    [CreateAssetMenu(fileName = "NewAltarSettings", menuName = "Altar/AltarSettings")]
    public class AltarSettings : ScriptableObject
    {
        [Tooltip("List of possible sacrifices. An Altar will randomly pick one of these upon creation.")]
        public List<AltarSacrificeData> PossibleSacrifices = new List<AltarSacrificeData>();
    }
}
