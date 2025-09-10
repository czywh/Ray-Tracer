using System;

namespace RayTracer
{
    public class EmissiveDisk : SceneEntity
    {
        public Vector3 center { get; }
        public Vector3 normal { get; }
        public float radius { get; }
		private Material material;
		
		private readonly Vector3 _tangent;
        private readonly Vector3 _bitangent;


		public EmissiveDisk(Vector3 center, Vector3 normal, float radius, Material emissiveMat)
		{
			this.center = center;
			this.normal = normal.Normalized();
			this.radius = radius;
			this.material = emissiveMat;
			
			// Construct a local basis orthogonal to the normal.
			Vector3 basis_calculater = Math.Abs(this.normal.X) > 0.5f ? new Vector3(0, 1, 0) : new Vector3(1, 0, 0);
            _tangent   = Vector3.Cross(basis_calculater, this.normal).Normalized();
            _bitangent = Vector3.Cross(this.normal, _tangent).Normalized();
		}

		/// <summary>
        /// Determine if a ray intersects with the EmissiveDisk, and if so, return hit data.
        /// </summary>
        /// <param name="ray">Ray to check</param>
        /// <returns>Hit data (or null if no intersection)</returns>
		public RayHit Intersect(Ray ray)
		{
			Vector3 Origin = ray.Origin;
			Vector3 Direction = ray.Direction;

			// Dot(n, r) close to 0; the ray is (nearly) parallel to the plane 
			double dot_n_r = Vector3.Dot(this.normal, Direction);
			if (Math.Abs(dot_n_r) <= Vector3.EPS) return null;


			// t = - dot(n, P_Origion - P_plane) / dot(n, r)
			double t = -Vector3.Dot(normal, Origin - center) / dot_n_r;
			// the ray is opposite direction to the plane  
			if (t <= Vector3.EPS) return null;

			Vector3 hit_postion = Origin + t * Direction;
			Vector3 hit_normal = this.normal;

			// check if it is in the radius range 
			double dist2 = (hit_postion - this.center).LengthSq();
			if (dist2 > this.radius * this.radius) return null;

			// the direction of ray and plane's normal are the same, we need to inverse 
			if (dot_n_r > 0) { hit_normal = -hit_normal; }

			return new(hit_postion, hit_normal, ray.Direction, material, t);
		}

		 public Material Material { get { return this.material; } }



		/// <summary>
		///  Randomly pick an sample on the disk 
		/// </summary>
		/// <param name="rng"> random seed</param>
		/// <returns></returns>
		public Vector3 RandomSamplePostion(Random rng)
		{
			double Sample_degree = 2.0 * Math.PI * rng.NextDouble();
			double Sample_radius = Math.Sqrt(rng.NextDouble()) * radius;
			double Sample_X = Sample_radius * Math.Cos(Sample_degree);
			double Sample_Y = Sample_radius * Math.Sin(Sample_degree);

			Vector3 Sample_Postion = this.center + Sample_X * _tangent + Sample_Y * _bitangent;
			return Sample_Postion;
		}

        public bool IsEmissive() => true;
        public Color EmissionColor() => Material.EmissionColor;
    }
}