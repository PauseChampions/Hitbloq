using System.Linq;
using UnityEngine;

namespace Hitbloq.Other
{
    internal class MaterialGrabber
    {
        private Material? noGlowRoundEdge;
        public Material NoGlowRoundEdge => noGlowRoundEdge ??= Resources.FindObjectsOfTypeAll<Material>().First(m => m.name == "UINoGlowRoundEdge");
    }   
}
