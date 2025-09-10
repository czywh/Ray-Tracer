using System;

namespace RayTracer
{
    /// <summary>
    /// Class to represent an (infinite) plane in a scene.
    /// </summary>
    public class Sphere : SceneEntity
    {
        private Vector3 center;
        private double radius;
        private Material material;

        /// <summary>
        /// Construct a sphere given its center point and a radius.
        /// </summary>
        /// <param name="center">Center of the sphere</param>
        /// <param name="radius">Radius of the spher</param>
        /// <param name="material">Material assigned to the sphere</param>
        public Sphere(Vector3 center, double radius, Material material)
        {
            this.center = center;
            this.radius = radius;
            this.material = material;
        }

        /// <summary>
        /// Determine if a ray intersects with the sphere, and if so, return hit data.
        /// </summary>
        /// <param name="ray">Ray to check</param>
        /// <returns>Hit data (or null if no intersection)</returns>
        public RayHit Intersect(Ray ray)
        {

			// R(t) = Origin + t * Direction
			Vector3 Origin = ray.Origin;
			Vector3 Direction = ray.Direction;
			Vector3 PO_PC = Origin - center;

			
			double a = Vector3.Dot(Direction, Direction); 
			double b = 2 * Vector3.Dot(PO_PC, Direction);
			double c = Vector3.Dot(PO_PC, PO_PC) - radius * radius;
			double delta = b * b - 4 * a * c;

			// delta < 0; there is no intersection 
			if (delta < 0) return null;

			double sqrt_delta = Math.Sqrt(delta);
			double t1 = (-b - sqrt_delta) / (2 * a);
			double t2 = (-b + sqrt_delta) / (2 * a);

			// find the first intersection point between camera and Sphere
			double t = (t1 > Vector3.EPS) ? t1: (t2 > Vector3.EPS) ? t2 : double.PositiveInfinity;

			// Sphere is behind the camera
			if (double.IsInfinity(t)) return null;

			Vector3 hit_postion = Origin + t * Direction;
			Vector3 hit_normal = (hit_postion - center).Normalized();

			RayHit hit = new(hit_postion, hit_normal, ray.Direction, material, t);

			return hit;
        }

        /// <summary>
        /// The material of the sphere.
        /// </summary>
        public Material Material { get { return this.material; } }

		public Vector3 position { get; private set; }
	}

}
