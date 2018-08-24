using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Procedural.Road {
	[ExecuteInEditMode]
	public class Noise : MonoBehaviour {
		public float scale = 1f;
		public float x_offset = 0f;
		public float y_offset = 0f;

		public GameObject debug;
		public float GetNoise(float x, float y, int mapsize) {
			return Mathf.PerlinNoise((x -0.5f + x_offset) / scale / mapsize, (y -0.5f + y_offset) / scale / mapsize);
		}

		void Start() {
			if (debug) {
				Texture2D t2d = new Texture2D(1024, 1024, TextureFormat.ARGB32, false);
				for (int i = 0; i < 1024; i++) {
					for (int j = 0; j < 1024; j++) {
						float tmp = GetNoise(i, j, 1024);
						t2d.SetPixel(i,j,new Color(tmp,tmp,tmp));
					}
				}
				t2d.Apply(false);

				debug.GetComponent<MeshRenderer>().sharedMaterial.mainTexture = t2d;
			}
		}
	}
}