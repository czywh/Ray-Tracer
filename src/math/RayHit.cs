using System;

namespace RayTracer
{
    /// <summary>
    /// Class to represent ray hit data, including the position and
    /// normal of a hit (and optionally other computed vectors).
    /// </summary>
    public class RayHit
    {
        private Vector3 position;
        private Vector3 normal;
        private Vector3 incident;
        private Material material;

		public float? U { get; set; }   
        public float? V { get; set; }
		public bool HasUV => U.HasValue && V.HasValue;

		private double t;

		public RayHit(Vector3 position, Vector3 normal, Vector3 incident, Material material, double t)
		{
			this.position = position;
			this.normal = normal;
			this.incident = incident;
			this.material = material;
			this.t = t;

		}


        public Vector3 Position { get { return this.position; } }

        public Vector3 Normal { get { return this.normal; } }

        public Vector3 Incident { get { return this.incident; } }

        public Material Material { get { return this.material; } }

		public double T { get { return this.t; }}
		
		

		/// <summary>
		///  Offset position for the rays to avoid self-intersection.
		/// </summary>
		/// <param name="direction"> Direction of the ray</param>
		/// <param name="epsilon"> Offset distance</param>
		/// <returns> Offset position </returns>
		public Vector3 GetOffsetPosition(Vector3 outDir, double epsilon = 1e-4)
		{
			Vector3 direction = outDir.Normalized();

			Vector3 n = this.normal.Normalized();
			 if (Vector3.Dot(n, direction) < 0) n = -n;
			
			return this.position + direction * epsilon;
		}
		
		/// <summary>
		/// function to spawn offset ray to aviod self intersect
		/// </summary>
		/// <param name="outDir"></param>
		/// <param name="epsilon"></param>
		/// <returns></returns>
		public Ray SpawnOffsetRay(Vector3 outDir, double epsilon = 1e-4)
		{
			Vector3 o = GetOffsetPosition(outDir, epsilon);
			return new Ray(o, outDir.Normalized());
		}

        /// <summary>
		/// Compute the reflection direction 
		/// </summary>
		/// <returns> R: Reflected direction vector </returns>
		public Vector3 GetReflectionDirection()
		{
			Vector3 D = this.incident.Normalized();
			Vector3 N = this.normal.Normalized();

			//Reflected direction R = D - 2 * (Dot(D, N)) * N 
			return (D - 2.0 * Vector3.Dot(D, N) * N).Normalized();
		}

        /// <summary>
        /// Compute the refraction direction using Snell's law.
        /// </summary>
        /// <returns> T: Refracted direction vector, or null if total internal reflection occurs </returns>
        public Vector3? GetRefractionDirection()
        {
			Vector3 D = this.incident.Normalized();
            Vector3 N = this.normal.Normalized();
			double etaI, etaT;

			// Determine if ray is entering or exiting the material
			if (Vector3.Dot(D, N) > 0)
			{
				// Ray is exiting: material -> air, flip normal to point towards incident ray
				N = -N; 
				etaI = material.RefractiveIndex; 
				etaT = 1.0;
			}
			else
			{
				// Ray is entering: air -> material
				etaI = 1.0; 
				etaT = material.RefractiveIndex; 
			}

			double cosI = -Vector3.Dot(D, N);
			double eta = etaI / etaT;            
            double sinT2 = eta * eta * (1.0 - cosI * cosI);
            
            // Total internal reflection
            if (sinT2 > 1.0) return null;
          
            double cosT = Math.Sqrt(1.0 - sinT2);
            return (eta * D + (eta * cosI - cosT) * N).Normalized();
        }

    }
}
