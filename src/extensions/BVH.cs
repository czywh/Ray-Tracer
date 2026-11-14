using System;
using System.Collections.Generic;

namespace RayTracer
{
	/// <summary>
	/// Bounding Volume Hierarchy for ray intersection detect
	/// </summary>
	public class BVH
	{
		/// <summary>
		/// BVH node for spatial partitioning.
		/// </summary>
		internal class BVHNode
		{
			public List<Triangle> Triangles { get; set; }
			public BoundingBox BoundingBox { get; set; }
			public BVHNode Left { get; set; }
			public BVHNode Right { get; set; }

			public BVHNode()
			{
				Triangles = new List<Triangle>();
				BoundingBox = new BoundingBox();
				Left = null;
				Right = null;
			}
		}

		/// <summary>
		/// AABB bounding box 
		/// </summary>
		internal class BoundingBox
		{
			public Vector3 Min { get; set; }
			public Vector3 Max { get; set; }

			public BoundingBox()
			{
				Min = new Vector3(double.MaxValue, double.MaxValue, double.MaxValue);
				Max = new Vector3(double.MinValue, double.MinValue, double.MinValue);
			}


			/// <summary>
			/// Expand the bounding box to include a point.
			/// </summary>
			/// <param name="point">Point to include</param>
			public void Expand(Vector3 point)
			{
				Min = new Vector3(
					Math.Min(Min.X, point.X),
					Math.Min(Min.Y, point.Y),
					Math.Min(Min.Z, point.Z)
				);
				Max = new Vector3(
					Math.Max(Max.X, point.X),
					Math.Max(Max.Y, point.Y),
					Math.Max(Max.Z, point.Z)
				);
			}

			/// <summary>
			/// Get the size of the bounding box.
			/// </summary>
			public Vector3 Size => Max - Min;

		}

		private BVHNode root;
		private const float EPS = 1e-6f;

		private const int LeafMax = 4;
		private const int MaxDepth = 20;

		/// <summary>
		/// Construct a new BVH from a list of triangles.
		/// </summary>
		/// <param name="triangles">List of triangles to build BVH for</param>
		public BVH(List<Triangle> triangles)
		{
			if (triangles == null || triangles.Count == 0)
			{
				root = null;
				return;
			}

			root = BuildBVHNode(triangles, 0);
		}

		/// <summary>
		/// Intersect a ray with the BVH and return the closest hit.
		/// </summary>
		/// <param name="ray">Ray to intersect</param>
		/// <returns>Closest hit or null if no intersection</returns>
		public RayHit Intersect(Ray ray)
		{
			if (root == null) return null;

			RayHit closestHit = null;
			double closestT = double.PositiveInfinity;

			IntersectBVH(ray, root, ref closestHit, ref closestT, EPS, double.PositiveInfinity);

			return closestHit;
		}

		/// <summary>
		/// Recursively build BVH node.
		/// Calculate the AABB of the current node, and use its longest axis as the splitting axis.
		/// </summary>
		private BVHNode BuildBVHNode(List<Triangle> triangles, int depth)
		{
			BVHNode node = new BVHNode();
			node.Triangles = triangles;
			node.BoundingBox = CalculateBoundingBox(triangles);

			if (triangles.Count <= LeafMax || depth > MaxDepth)
			{
				node.Left = null;
				node.Right = null;
				return node;
			}

			// find the longest axis of current node's bounding box as the splitting axis
			int axis = GetLongestAxis(node.BoundingBox);

			// sort triangles by center along the longest axis
			triangles.Sort((a, b) =>
			{
				Vector3 centerA = GetTriangleCenter(a);
				Vector3 centerB = GetTriangleCenter(b);
				return centerA.GetAxis(axis).CompareTo(centerB.GetAxis(axis));
			});

			// split triangles into left and right 
			int mid = triangles.Count / 2;
			List<Triangle> leftTriangles = triangles.GetRange(0, mid);
			List<Triangle> rightTriangles = triangles.GetRange(mid, triangles.Count - mid);

			node.Left = BuildBVHNode(leftTriangles, depth + 1);
			node.Right = BuildBVHNode(rightTriangles, depth + 1);

			return node;
		}

		/// <summary>
		/// Calculate AABB box for a list of triangles.
		/// </summary>
		private BoundingBox CalculateBoundingBox(List<Triangle> triangles)
		{
			if (triangles.Count == 0) return new BoundingBox();

			BoundingBox box = new();

			foreach (Triangle triangle in triangles)
			{
				Vector3[] vertices = triangle.GetVertices();
				foreach (Vector3 vertex in vertices)
				{
					box.Expand(vertex);
				}
			}

			return box;
		}

		/// <summary>
		/// Get the longest axis of a bounding box.
		/// </summary>
		private int GetLongestAxis(BoundingBox box)
		{
			Vector3 size = box.Size;
			if (size.X >= size.Y && size.X >= size.Z) return 0;
			if (size.Y >= size.Z) return 1;
			return 2;
		}

		/// <summary>
		/// Get triangle center point.
		/// </summary>
		private static Vector3 GetTriangleCenter(Triangle triangle)
		{
			Vector3[] vertices = triangle.GetVertices();
			return (vertices[0] + vertices[1] + vertices[2]) / 3.0;
		}

		/// <summary>
		/// Intersect ray with BVH.
		/// </summary>
		private void IntersectBVH(Ray ray, BVHNode node, ref RayHit closestHit, ref double closestT, double tMin, double tMax)
		{
			if (node == null) return;

			// Check if ray intersects bounding box
			double tNear = tMin, tFar = closestT < tMax ? closestT : tMax;
			if (!IntersectBoundingBox(ray, node.BoundingBox, ref tNear, ref tFar)) return;

			// If leaf node, test all triangles
			if (node.Left == null && node.Right == null)
			{
				foreach (Triangle triangle in node.Triangles)
				{
					RayHit hit = triangle.Intersect(ray);
					if (hit != null && hit.T < closestT)
					{
						closestHit = hit;
						closestT = hit.T;
					}
				}
			}

			IntersectBVH(ray, node.Left, ref closestHit, ref closestT, tNear, tFar);
			IntersectBVH(ray, node.Right, ref closestHit, ref closestT, tNear, tFar);
			
		}

		/// <summary>
		/// Using invDir slab method to check the intersection between ray and bounding box.
		/// </summary>
		private static bool IntersectBoundingBox(Ray ray, BoundingBox box, ref double tMin, ref double tMax)
		{
			// Handle zero direction components to avoid division by zero
			double invX = Math.Abs(ray.Direction.X) < EPS ? 1e30 : 1.0 / ray.Direction.X;
			double invY = Math.Abs(ray.Direction.Y) < EPS ? 1e30 : 1.0 / ray.Direction.Y;
			double invZ = Math.Abs(ray.Direction.Z) < EPS ? 1e30 : 1.0 / ray.Direction.Z;

			// Update the effective range along the X-axis
			double t0 = (box.Min.X - ray.Origin.X) * invX;
			double t1 = (box.Max.X - ray.Origin.X) * invX;
			if (t0 > t1) (t0, t1) = (t1, t0);
			tMin = Math.Max(tMin, t0);
			tMax = Math.Min(tMax, t1);
			if (tMax <= tMin) return false;

			// Update the effective range along the Y-axis
			t0 = (box.Min.Y - ray.Origin.Y) * invY;
			t1 = (box.Max.Y - ray.Origin.Y) * invY;
			if (t0 > t1) (t0, t1) = (t1, t0);
			tMin = Math.Max(tMin, t0);
			tMax = Math.Min(tMax, t1);
			if (tMax <= tMin) return false;

			// Update the effective range along the Z-axis
			t0 = (box.Min.Z - ray.Origin.Z) * invZ;
			t1 = (box.Max.Z - ray.Origin.Z) * invZ;
			if (t0 > t1) (t0, t1) = (t1, t0);
			tMin = Math.Max(tMin, t0);
			tMax = Math.Min(tMax, t1);

			return tMax >= tMin;
		}

		/// <summary>
		/// Check if the ray hit any box in BVH
		/// </summary>
		/// <param name="ray"></param>
		/// <param name="tMax"></param>
		/// <returns></returns>
		public bool AnyHit(Ray ray, double tMax)
		{
			if (root == null) return false;
			return AnyHitBVH(ray, root, EPS, tMax);
		}

		private bool AnyHitBVH(Ray ray, BVHNode node, double tMin, double tMax)
		{
			if (node == null) return false;

			// Check if ray intersects bounding box
			double tNear = tMin, tFar = tMax;
			if (!IntersectBoundingBox(ray, node.BoundingBox, ref tNear, ref tFar)) return false;

			// If leaf node, test all triangles
			if (node.Left == null && node.Right == null)
			{
				foreach (Triangle triangle in node.Triangles)
				{
					RayHit hit = triangle.Intersect(ray);
					// ray interset one of traingles in BVH, return true 
					if (hit != null && hit.T > tMin && hit.T < tMax) return true;
				}
				return false;
			}
			else
			{
				// Recursively test children
				if (AnyHitBVH(ray, node.Left, tMin, tMax)) return true;
				if (AnyHitBVH(ray, node.Right, tMin, tMax)) return true;
				return false;
			}

		}
    }
}