using System.Linq;
using UnityEngine;

namespace Hitbloq.Other
{
	internal class MaterialGrabber
	{
		private Material? _noGlowRoundEdge;
		public Material NoGlowRoundEdge => _noGlowRoundEdge ??= Resources.FindObjectsOfTypeAll<Material>().First(m => m.name == "UINoGlowRoundEdge");
	}
}