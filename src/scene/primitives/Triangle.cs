using System;
using System.Numerics;
using Vec2 = System.Numerics.Vector2;

namespace RayTracer
{
	/// <summary>
	/// Class to represent a triangle in a scene represented by three vertices.
	/// </summary>
	public class Triangle : SceneEntity
	{
		private Vector3 v0, v1, v2;
		private Vec2 uv0, uv1, uv2;
		private Material material;

		private bool hasUV;

		/// <summary>
		/// Construct a triangle object given three vertices.
		/// </summary>
		/// <param name="v0">First vertex position</param>
		/// <param name="v1">Second vertex position</param>
		/// <param name="v2">Third vertex position</param>
		/// <param name="material">Material assigned to the triangle</param>
		public Triangle(Vector3 v0, Vector3 v1, Vector3 v2, Material material)
		{
			this.v0 = v0;
			this.v1 = v1;
			this.v2 = v2;
			this.material = material;
			this.hasUV = false; 
		}


		public Triangle(Vector3 v0, Vector3 v1, Vector3 v2, Material material,
					Vec2 uv0, Vec2 uv1, Vec2 uv2)
		{
			this.v0 = v0;
			this.v1 = v1;
			this.v2 = v2;
			this.material = material;
			this.uv0 = uv0;
			this.uv1 = uv1;
			this.uv2 = uv2;
			this.hasUV = true; 
		}



		/// <summary>
		/// Determine if a ray intersects with the triangle, and if so, return hit data.
		/// Using Möller–Trumbore Algorithmn to detect interset 
		/// </summary>
		/// <param name="ray">Ray to check</param>
		/// <returns>Hit data (or null if no intersection)</returns>
		public RayHit Intersect(Ray ray)
		{
			Vector3 Origin = ray.Origin;
			Vector3 Direction = ray.Direction;
			bool backface_cull = false;

			Vector3 edge1 = this.v1 - this.v0;
    		Vector3 edge2 = this.v2 - this.v0;

			// pvec = D x e2
			Vector3 pvec = Vector3.Cross(Direction, edge2);
			double det = Vector3.Dot(edge1, pvec);

			if (backface_cull)
			{
				if (det <= Vector3.EPS) return null;
			}
			else
			{
				if (Math.Abs(det) <= Vector3.EPS) return null;
			}

			double invDet = 1.0 / det;

			// u = (tvec · pvec) * invDet (b1)
			Vector3 tvec = Origin - this.v0;
			double u = Vector3.Dot(tvec, pvec) * invDet;

			if (u < 0.0 - Vector3.EPS || u > 1.0 + Vector3.EPS) return null;

			// qvec = tvec x e1
			Vector3 qvec = Vector3.Cross(tvec, edge1);

			// v = (D · qvec) * invDet  (b2)
			double v = Vector3.Dot(Direction, qvec) * invDet;
			if (v < 0.0 - Vector3.EPS || (u + v) > 1.0 + Vector3.EPS) return null;

			// t = (e2 · qvec) * invDet
			double t = Vector3.Dot(edge2, qvec) * invDet;
			if (t <= Vector3.EPS) return null;

			Vector3 P = Origin + t * Direction;
			Vector3 N = Vector3.Cross(edge1, edge2).Normalized();

			if (Vector3.Dot(Direction, N) > 0) N = -N;

			RayHit hit = new(P, N, Direction, this.material, t);

			if (hasUV)
			{
				double b0 = u;
				double b1 = v;
				double b2 = 1 - b0 - b1;

				Vec2 uv = uv0 * (float)b0 + uv1 * (float)b1 + uv2 * (float)b2;
				hit.U = uv.X;
				hit.V = uv.Y;
			}
			return hit;

		}


		/// <summary>
		/// The material of the triangle.
		/// </summary>
		public Material Material { get { return this.material; } }

		/// <summary>
		/// Get the three vertices of the triangle.
		/// </summary>
		/// <returns>Array of three vertices</returns>
		public Vector3[] GetVertices()
		{
			 return [v0, v1, v2];
		}

	
	}
}
