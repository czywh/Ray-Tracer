using System;
using System.Collections.Generic;
using System.Globalization; 
using System.IO;
using Vec2 = System.Numerics.Vector2;

namespace RayTracer
{
	/// <summary>
	/// Add-on option C. You should implement your solution in this class template.
	/// OBJ model loader and renderer with BVH for complex models.
	/// </summary>
	public class ObjModel : SceneEntity
	{
		private string objFilePath;
		private Transform transform;
		private Material material;
		private List<Triangle> triangles;

		private readonly List<Vector3> worldVertexs = new();
		private readonly List<Vector3> worldNormal = new();
		private readonly List<Vec2> worldUVs = new();
		private BVH bvh;

		private const float EPS = 1e-8f;


		/// <summary>
		/// Construct a new OBJ model.
		/// </summary>
		/// <param name="objFilePath">File path of .obj</param>
		/// <param name="transform">Transform to apply to each vertex</param>
		/// <param name="material">Material applied to the model</param>
		public ObjModel(string objFilePath, Transform transform, Material material)
		{
			this.objFilePath = objFilePath;
			this.transform = transform;
			this.material = material;
			this.triangles = new List<Triangle>();

			ParseObj(objFilePath);
		}

		/// <summary>
		/// Given a ray, determine whether the ray hits the object
		/// Using AABB and BVH for efficient intersect detect
		/// and if so, return relevant hit data (otherwise null).
		/// </summary>
		/// <param name="ray">Ray data</param>
		/// <returns>Ray hit data, or null if no hit</returns>
		public RayHit Intersect(Ray ray)
		{
			if (bvh == null) return null;
			return bvh.Intersect(ray);
		}

		/// <summary>
		/// The material attached to this object.
		/// </summary>
		public Material Material { get { return this.material; } }

		private void ParseObj(string objFilePath)
		{
			// Here's some code to get you started reading the file...
			string[] lines = File.ReadAllLines(objFilePath);

			List<Vector3> localVertices = [];
			List<Vector3> localNormals = [];
			List<Face> faces = [];
			List<Vec2> localUVs = [];

			var inv = CultureInfo.InvariantCulture;
			foreach (string raw in lines)
			{
				string line = raw.Trim();

				// skip comments  and empty line 
				if (line.Length == 0 || line.StartsWith('#')) continue;

				// split line into words 
				string[] parts = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
				if (parts.Length == 0) continue;

				switch (parts[0])
				{
					case "v":
						if (parts.Length >= 4) VertexCase(parts, localVertices, inv);
						break;

					case "vn":
						if (parts.Length >= 4) NormalCase(parts, localNormals, inv);
						break;
					
					case "vt":
						if (parts.Length >= 3) VertexTextureCase(parts, localUVs, inv);
						break;

					case "f":

						if (parts.Length >= 4)
						{
							Face face = ParseFace(parts);
							faces.Add(face);
						}
						break;
				}
			}

			LocalToWorldTransform(localVertices, localNormals, localUVs, transform, worldVertexs, worldNormal, worldUVs);
			this.triangles = BuildTrianglesFromFace(faces, worldVertexs, material);
			
			// Build BVH for efficient ray intersection
			this.bvh = new BVH(this.triangles);

		}

		/// <summary>
		/// function to parese vertex case 
		/// </summary>
		/// <param name="parts">each lines input</param>
		/// <param name="localVertices">list to store local vertices</param>
		/// <param name="inv"></param>
		private static void VertexCase(string[] parts, List<Vector3> localVertices, IFormatProvider inv)
		{
			double x = double.Parse(parts[1], inv);
			double y = double.Parse(parts[2], inv);
			double z = double.Parse(parts[3], inv);
			localVertices.Add(new(x, y, z));
		}

		/// <summary>
		/// function to parese normal index
		/// </summary>
		/// <param name="parts">each lines input</param>
		/// <param name="localVertices">list to store local normal index</param>
		/// <param name="inv"></param>
		private static void NormalCase(string[] parts, List<Vector3> localNormals, IFormatProvider inv)
		{
			double x = double.Parse(parts[1], inv);
			double y = double.Parse(parts[2], inv);
			double z = double.Parse(parts[3], inv);
			localNormals.Add(new Vector3(x, y, z).Normalized());
		}

		/// <summary>
		/// function to parese vertex texture index
		/// </summary>
		/// <param name="parts">each lines input</param>
		/// <param name="localVertices">list to store local normal index</param>
		/// <param name="inv"></param>
		private static void VertexTextureCase(string[] parts, List<Vec2> localUVs, IFormatProvider inv)
		{
			float u = float.Parse(parts[1], inv);
			float v = float.Parse(parts[2], inv);
			localUVs.Add(new Vec2(u, v));
		}

		/// <summary>
		/// Transofrm from local coordinates to world cooridinates
		/// Follow by order S -> R -> T
		/// </summary>
		/// <param name="localVertices">list to store local vertices</param>
		/// <param name="localNormals">list to store local normal index</param>
		/// <param name="localUVs">list to store local u and v for texture mapping</param>
		/// <param name="transform">transform class</param>
		/// <param name="worldVertices">list to store world vertices</param>
		/// <param name="worldNormals">list to store world normal index</param>
		/// <param name="worldUVs">list to store world u and v for texture mapping</param>
		private static void LocalToWorldTransform(IReadOnlyList<Vector3> localVertices,
			IReadOnlyList<Vector3> localNormals,
			IReadOnlyList<Vec2> localUVs,
			Transform transform,
			List<Vector3> worldVertices,
			List<Vector3> worldNormals,
			List<Vec2> worldUVs)
		{
			double scale = transform.Scale;
			var rotation = transform.Rotation;
			var t = transform.Position;

			worldVertices?.Clear();
			worldNormals?.Clear();
			worldUVs?.Clear();

			// transform for vertex from local to world 
			foreach (var vertex in localVertices)
			{
				// S -> R -> T
				var vertex_scaled = vertex * scale;
				var vertex_rotated = rotation.Rotate(vertex_scaled);
				var vertex_world = vertex_rotated + t;
				worldVertices.Add(vertex_world);
			}

			// transform for normals from local to world 
			foreach (var normal in localNormals)
			{
				var rotated_normal = rotation.Rotate(normal);
				worldNormals.Add(rotated_normal.Normalized());
			}
			// u and v remain the same 
			worldUVs.AddRange(localUVs);
		}

		internal class Face
		{
			public int V0, V1, V2;
			public int N0, N1, N2;

			public int T0, T1, T2;

			public Face()
			{
				V0 = V1 = V2 = -1;
				N0 = N1 = N2 = -1;
				T0 = T1 = T2 = -1;
			}
		}

		/// <summary>
		/// function to parese face
		/// </summary>
		/// <param name="parts">input line </param>
		/// <returns>paresed face</returns>
		/// <exception cref="FormatException"></exception>
		private static Face ParseFace(string[] parts)
		{
			// 'v', 'v//vn', 'v/vt/vn'
			// function to read "v" and "vn"
			static (int vi, int ti, int ni) ParseVertex(string part)
			{
				var comps = part.Split('/');

				// we will also need convert index from 1-base into 0-base
				// If we dont have corresponding value; set index as -1 
				int vi = (comps.Length >= 1 && comps[0].Length > 0) ? int.Parse(comps[0]) - 1 : -1;
				int ti = (comps.Length >= 2 && comps[1].Length > 0) ? int.Parse(comps[1]) - 1 : -1;
				int ni = (comps.Length >= 3 && comps[2].Length > 0) ? int.Parse(comps[2]) - 1 : -1;

				if (vi < 0) throw new FormatException($"the index of vertex must be positve:\"{part}\"");
				return (vi, ti, ni);
			}

			// "f v0/t0/n0 v1/t1/n1 v2/t2/n2"
			var (v0, t0, n0) = ParseVertex(parts[1]);
			var (v1, t1, n1) = ParseVertex(parts[2]);
			var (v2, t2, n2) = ParseVertex(parts[3]);

			var face = new Face
			{
				V0 = v0,
				V1 = v1,
				V2 = v2,
				T0 = t0,
				T1 = t1,
				T2 = t2,
				N0 = n0,
				N1 = n1,
				N2 = n2
			};

			return face;
		}


		/// <summary>
		/// Building triganles from faces 
		/// </summary>
		/// <param name="faces">face list wheree stroe faces' information </param>
		/// <param name="worldVertices"> list where store world vertices coordinates</param>
		/// <param name="material"></param>
		/// <returns>list of triangles which are builted from faces </returns>
		private List<Triangle> BuildTrianglesFromFace(List<Face> faces, List<Vector3> worldVertices,  Material material)
		{
			var triangles = new List<Triangle>();
			bool flip = transform.Scale < 0;

			foreach (var face in faces)
			{
				Vector3 v0 = worldVertices[face.V0];
				Vector3 v1 = worldVertices[face.V1];
				Vector3 v2 = worldVertices[face.V2];

				bool hasUV = face.T0 >= 0 && face.T1 >= 0 && face.T2 >= 0 && face.T0 < worldUVs.Count && face.T1 < worldUVs.Count &&
						face.T2 < worldUVs.Count;

				// Create triangle with proper vertex order
				if (hasUV)
					{
						Vec2 uv0 = worldUVs[face.T0];
						Vec2 uv1 = worldUVs[face.T1];
						Vec2 uv2 = worldUVs[face.T2];

						if (!flip)
							triangles.Add(new Triangle(v0, v1, v2, material, uv0, uv1, uv2));
						else
							triangles.Add(new Triangle(v0, v2, v1, material, uv0, uv2, uv1));
					}
					else
					{
						// UV is not readed in 
						if (!flip)
							triangles.Add(new Triangle(v0, v1, v2, material));
						else
							triangles.Add(new Triangle(v0, v2, v1, material));
					}
			}
			return triangles;
		}


	}
}
