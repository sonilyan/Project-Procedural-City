using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Procedural.Road {

	[CustomEditor(typeof(Noise))]
	public class NoiseIns : Editor {
		public override void OnInspectorGUI() {
			base.OnInspectorGUI();
		}
	}
}