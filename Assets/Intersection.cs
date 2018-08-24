using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Procedural.Util {

	public class Intersection {
		public static bool Get(Vector2 s1, Vector2 e1, Vector2 s2, Vector2 e2, out Vector2 intersection) {
			float x, y;
			bool retval = Get(s1.x, s1.y, e1.x, e1.y, s2.x, s2.y, e2.x, e2.y, out x, out y);
			intersection = new Vector2(x, y);
			return retval;
		}

		static bool Get(float p0_x, float p0_y, float p1_x, float p1_y, float p2_x, float p2_y, float p3_x, float p3_y, out float i_x, out float i_y) {
			i_x = 0;
			i_y = 0;

			float s02_x, s02_y, s10_x, s10_y, s32_x, s32_y, s_numer, t_numer, denom, t;
			s10_x = p1_x - p0_x;
			s10_y = p1_y - p0_y;
			s32_x = p3_x - p2_x;
			s32_y = p3_y - p2_y;

			denom = s10_x * s32_y - s32_x * s10_y;
			if (denom == 0)//平行或共线
				return false; // Collinear
			bool denomPositive = denom > 0;

			s02_x = p0_x - p2_x;
			s02_y = p0_y - p2_y;
			s_numer = s10_x * s02_y - s10_y * s02_x;
			if ((s_numer < 0) == denomPositive)//参数是大于等于0且小于等于1的，分子分母必须同号且分子小于等于分母
				return false; // No collision

			t_numer = s32_x * s02_y - s32_y * s02_x;
			if ((t_numer < 0) == denomPositive)
				return false; // No collision

			if (((s_numer > denom) == denomPositive) || ((t_numer > denom) == denomPositive))
				return false; // No collision
							  // Collision detected
			t = t_numer / denom;

			i_x = p0_x + (t * s10_x);
			i_y = p0_y + (t * s10_y);

			return true;
		}
	}
}
