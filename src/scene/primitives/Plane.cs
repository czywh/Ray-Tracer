using System;

namespace RayTracer
{
    /// <summary>
    /// Class to represent an (infinite) plane in a scene.
    /// </summary>
    public class Plane : SceneEntity
    {
        private Vector3 center;
        private Vector3 normal;
        private Material material;

        /// <summary>
        /// Construct an infinite plane object.
        /// </summary>
        /// <param name="center">Position of the center of the plane</param>
        /// <param name="normal">Direction that the plane faces</param>
        /// <param name="material">Material assigned to the plane</param>
        public Plane(Vector3 center, Vector3 normal, Material material)
        {
            this.center = center;
            this.normal = normal.Normalized();
            this.material = material;
        }

        /// <summary>
        /// Determine if a ray intersects with the plane, and if so, return hit data.
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

			// the direction of ray and plane's normal are the same, we need to inverse 
			if (dot_n_r > 0) { hit_normal = -hit_normal; }

			return new(hit_postion, hit_normal, ray.Direction, material, t);
        }

        /// <summary>
        /// The material of the plane.
        /// </summary>
        public Material Material { get { return this.material; } }
    }

}
