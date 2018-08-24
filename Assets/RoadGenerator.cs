using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

using Procedural.Util;

namespace Procedural.Road {

	public enum RoadType {
		MainRoad,
		SubRoad,
	}

	public class RoadSegment : IComparable<RoadSegment> {
		public Vector2 start;
		public Vector2 end;
		public RoadType roadType;
		public int roadLength;
		public float rotation;

		public float priority;
		public float width = 1f;

		public int CompareTo(RoadSegment other) {
			return priority - other.priority > 0 ? 1 : 0;
		}

		public RoadSegment prev;

		public static Vector2 RotateVector(float angle, float length) {
			float x = Mathf.Cos(Mathf.Deg2Rad * angle) * length;
			float y = Mathf.Sin(Mathf.Deg2Rad * angle) * length;
			return new Vector2(x, y);
		}

		internal Vector2 getMiddle() {
			return start + end / 2;
		}
	}

	[RequireComponent(typeof(Noise))]
	public class RoadGenerator : MonoBehaviour {
		System.Random random;
		Noise noise;

		public int seed = 0;

		public int MaxSegments = 100;

		public float closeMiddle = 5;

		public int MainRoadStepLength = 10;
		public int SubRoadStepLength = 5;

		[Range(0, 100)] public int mainRoadBranchChance = 50; // 50%

		public int maxMainRoadLength = 10;
		public int maxSecondaryRoadLength = 5;

		public int MapSize = 100; //100x100 map

		[Range(0, 1)] public float mainRoadAdvantage = 0.1f;
		[Range(1, 1000)] public float mainRoadDetrimentRange = 500;
		[Range(0, 1)] public float mainRoadDetrimentImpact = 0.01f;

		[Range(0, 360)] public int MainChangeIntensity = 45; //degree
		[Range(0, 360)] public int SubChangeIntensity = 45;

		private Dictionary<int, Dictionary<int, HashSet<RoadSegment>>> SegmentMap;
		List<RoadSegment> determinedSegments = new List<RoadSegment>();
		List<RoadSegment> MainSegments = new List<RoadSegment>();

		private void Awake() {
			noise = GetComponent<Noise>();
		}

		void RefSegmentsAdd(RoadSegment current) {
			SegmentMap[(int) current.start.x / maxMainRoadLength][(int) current.start.y / maxMainRoadLength].Add(current);
			SegmentMap[(int) current.end.x / maxMainRoadLength][(int) current.end.y / maxMainRoadLength].Add(current);
		}

		void RefSegmentsRemove(RoadSegment current) {
			int x1 = (int) current.end.x / maxMainRoadLength;
			int y1 = (int) current.end.y / maxMainRoadLength;

			int x2 = (int) current.start.x / maxMainRoadLength;
			int y2 = (int) current.start.y / maxMainRoadLength;

			if (SegmentMap[x1][y1].Contains(current))
				SegmentMap[x1][y1].Remove(current);
			if (SegmentMap[x2][y2].Contains(current))
				SegmentMap[x2][y2].Remove(current);
		}

		void GetRefPoint(Vector2 point, HashSet<RoadSegment> hashSet) {
			int index_x = (int) point.x / maxMainRoadLength;
			int index_y = (int) point.y / maxMainRoadLength;

			int minx = index_x - 1;
			int miny = index_y - 1;

			int maxx = index_x + 1;
			int maxy = index_y + 1;

			for (int i = minx; i < maxx; i++) {
				for (int j = miny; j < maxy; j++) {
					foreach (var s in SegmentMap[i][j]) {
						hashSet.Add(s);
					}
				}
			}
		}

		HashSet<RoadSegment> GetRefSegments(RoadSegment current) {
			HashSet<RoadSegment> retval = new HashSet<RoadSegment>();
			GetRefPoint(current.start, retval);
			GetRefPoint(current.start, retval);
			return retval;
		}

		private void Start() {
			random = new System.Random(seed);

			SegmentMap = new Dictionary<int, Dictionary<int, HashSet<RoadSegment>>>();

			int smap = MapSize / maxMainRoadLength;

			for (int i = -1; i < smap + 1; i++) {
				SegmentMap.Add(i, new Dictionary<int, HashSet<RoadSegment>>());
				for (int j = -1; j < smap + 1; j++) {
					SegmentMap[i].Add(j, new HashSet<RoadSegment>());
				}
			}

			determinedSegments.Clear();

			InitRoadSegments();
		}

		public void InitRoadSegments() {
			RoadSegment startRoad = new RoadSegment {
				start = new Vector2(MapSize / 2, MapSize / 2),
				roadType = RoadType.MainRoad,
				roadLength = 1,
			};

			startRoad.prev = startRoad;

			float bestval = float.MinValue;
			for (int i = 0; i < 360; i++) {
				Vector2 end = startRoad.start + RoadSegment.RotateVector(i, MainRoadStepLength);

				float tmp = noise.GetNoise(end.x, end.y, MapSize);
				if (bestval < tmp) {
					bestval = tmp;
					startRoad.rotation = i;
					startRoad.end = end;
				}
			}

			PriorityQueue<RoadSegment> queue = new PriorityQueue<RoadSegment>(4096);

			queue.Push(startRoad);

			while (queue.Count > 0 && determinedSegments.Count < MaxSegments) {
				var current = queue.Pop();
				if (localConstraints(current)) {
					determinedSegments.Add(current);
					RefSegmentsAdd(current);
					if (current.roadType == RoadType.MainRoad) {
						MainSegments.Add(current);
					}

					addExtensions(queue, current);
				}
			}

			Debug.Log("Done");
		}

		private void Update() {
			foreach (var s in determinedSegments) {
				if (s.roadType == RoadType.MainRoad)
					Debug.DrawLine(s.start, s.end, Color.black);
				else
					Debug.DrawLine(s.start, s.end, Color.yellow);
			}

			Debug.DrawLine(new Vector2(0, 0), new Vector2(0, MapSize), Color.red);
			Debug.DrawLine(new Vector2(0, 0), new Vector2(MapSize, 0), Color.red);
			Debug.DrawLine(new Vector2(MapSize, MapSize), new Vector2(MapSize, 0), Color.red);
			Debug.DrawLine(new Vector2(MapSize, MapSize), new Vector2(0,MapSize), Color.red);
		}

		void addExtensions(PriorityQueue<RoadSegment> queue, RoadSegment current) {
			if (current.roadType == RoadType.MainRoad) {
				if (current.roadLength < maxMainRoadLength)
					addRoadForward(queue, current);

				if (random.Next(0, 100) < mainRoadBranchChance)
					addRoadSide(queue, current, true, RoadType.MainRoad);
				else
					addRoadSide(queue, current, true, RoadType.SubRoad);

				if (random.Next(0, 100) < mainRoadBranchChance)
					addRoadSide(queue, current, false, RoadType.MainRoad);
				else
					addRoadSide(queue, current, false, RoadType.SubRoad);
			}
			else if (current.roadType == RoadType.SubRoad) {
				if (current.roadLength < maxSecondaryRoadLength) {
					addRoadForward(queue, current);

					addRoadSide(queue, current, true, RoadType.SubRoad);
					addRoadSide(queue, current, false, RoadType.SubRoad);
				}
			}
		}


		private void addRoadSide(PriorityQueue<RoadSegment> queue, RoadSegment prev, bool left, RoadType roadType) {
			RoadSegment newRoad = new RoadSegment();

			newRoad.prev = prev;
			newRoad.roadType = roadType;

			float newRotation = left ? 90 : -90;
			newRoad.rotation = prev.rotation + newRotation;

			newRoad.start = prev.start;
			newRoad.end = newRoad.start; //for GetRefSegments

			float stepLength = prev.roadType == RoadType.MainRoad ? MainRoadStepLength : SubRoadStepLength;

			newRoad.rotation = getBestRotation(SubChangeIntensity,
				newRoad.rotation, newRoad.start, stepLength, MainSegments, mainRoadDetrimentRange, mainRoadDetrimentImpact);

			newRoad.end = newRoad.start + RoadSegment.RotateVector(newRoad.rotation, stepLength);

			float val = getValueOfRotation(newRoad.end, MainSegments, mainRoadDetrimentRange, mainRoadDetrimentImpact);
			newRoad.priority = -val + ((newRoad.roadType == RoadType.MainRoad) ? mainRoadAdvantage : 0) +
			               Mathf.Abs(0.1f * prev.priority); // + baseLibraryStream.FRand() * 0.1;

			newRoad.roadLength = (prev.roadType == RoadType.MainRoad && roadType != RoadType.MainRoad) ? 1 : prev.roadLength + 1;

			if (checkVaild(newRoad)) {
				queue.Push(newRoad);
				Debug.Log($"addRoadSide {newRoad.start} -> {newRoad.end}");
			}
		}

		private bool checkVaild(RoadSegment road) {
			if (road.start.x < 0 || road.start.x > MapSize || road.end.x < 0 || road.end.x > MapSize)
				return false;
			if (road.start.y < 0 || road.start.y > MapSize || road.end.y < 0 || road.end.y > MapSize)
				return false;
			return true;
		}

		private void addRoadForward(PriorityQueue<RoadSegment> queue, RoadSegment prev) {
			RoadSegment newRoad = new RoadSegment();

			newRoad.prev = prev;
			newRoad.start = prev.end;
			newRoad.roadType = prev.roadType;
			newRoad.roadLength = prev.roadLength + 1;

			newRoad.end = newRoad.start; //for GetRefSegments

			float stepLength = prev.roadType == RoadType.MainRoad ? MainRoadStepLength : SubRoadStepLength;
			int changeIntensity = prev.roadType == RoadType.MainRoad ? MainChangeIntensity : SubChangeIntensity;


			newRoad.rotation = getBestRotation(changeIntensity,
				prev.rotation, newRoad.start, stepLength, MainSegments, mainRoadDetrimentRange, mainRoadDetrimentImpact);

			newRoad.end = newRoad.start + RoadSegment.RotateVector(newRoad.rotation, stepLength);

			float val = getValueOfRotation(newRoad.end, MainSegments, mainRoadDetrimentRange, mainRoadDetrimentImpact);
			newRoad.priority = -val + ((newRoad.roadType == RoadType.MainRoad) ? mainRoadAdvantage : 0) +
			               Mathf.Abs(0.1f * prev.priority); // + baseLibraryStream.FRand() * 0.1;

			if (checkVaild(newRoad)) {
				queue.Push(newRoad);
				Debug.Log($"addRoadForward {newRoad.start} -> {newRoad.end}");
			}
		}

		private float getBestRotation(int maxDiffAllowed, float original, Vector2 originalPoint, float step,
			List<RoadSegment> others, float maxDist, float detriment) {
			Vector2 testPoint = originalPoint + RoadSegment.RotateVector(original, step);
			float bestVal = float.MinValue;
			float bestRotator = original;
			for (int i = 0; i < 7; i++) {
				float curr = original + random.Next(-maxDiffAllowed, maxDiffAllowed);
				testPoint = originalPoint + RoadSegment.RotateVector(curr, step);
				float val = getValueOfRotation(testPoint, others, maxDist, detriment);
				if (val > bestVal) {
					bestRotator = curr;
					bestVal = noise.GetNoise(testPoint.x, testPoint.y, MapSize);
				}
			}

			return bestRotator;
		}

		private float getValueOfRotation(Vector2 testPoint, List<RoadSegment> others, float maxDist, float detriment) {
			float val = noise.GetNoise(testPoint.x, testPoint.y, MapSize); //noise(noiseScale, testPoint.X, testPoint.Y);
			foreach (var t in others) {
				if (Vector2.Distance(t.getMiddle(), testPoint) < maxDist) {
					//val -= Mathf.Max(0.0f, detriment * (maxDist - Vector2.Distance(t.getMiddle(), testPoint)) / maxDist);
					var mid_dist = Vector2.Distance(t.getMiddle(), testPoint);
					val -= Mathf.Max(0.0f, detriment * (1 - mid_dist / maxDist));
				}
			}

			return val;

		}

		bool localConstraints(RoadSegment current) {
			foreach (var seg in GetRefSegments(current)) {
				if (seg.prev == current)
					continue;

				if (Vector2.Distance(current.getMiddle(), seg.getMiddle()) < closeMiddle)
					return false;

				Vector2 intersection;
				if (Intersection.Get(current.start, current.end, seg.start, seg.end, out intersection)) {
					current.priority = float.MaxValue;
					collideInto(current, seg, intersection);
				}
			}

			return true;
		}

		void collideInto(RoadSegment current, RoadSegment seg, Vector2 intersection) {
			current.end = intersection;
		}

	}

}