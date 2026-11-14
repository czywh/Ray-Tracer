using System;
using System.Collections.Generic;



namespace RayTracer
{
	/// <summary>
	/// Class to represent a ray traced scene, including the objects,
	/// light sources, and associated rendering logic.
	/// </summary>
	public class Scene
	{
		private SceneOptions options;
		private Camera camera;
		private Color ambientLightColor;
		private ISet<SceneEntity> entities;
		private ISet<SceneEntity> emissiveEntities;
		private ISet<PointLight> lights;

		private ISet<Animation> animations;

		private readonly Random _rng = new Random(2025);
		private const float fieldOfView = 60f;
		private const int REFLECTION_MAX_DEPTH = 5;
		private const int REFRACTION_MAX_DEPTH = 5;

		private float _imagePlaneHeight;
		private float _imagePlaneWidth;

		/// <summary>
		/// Construct a new scene with provided options.
		/// </summary>
		/// <param name="options">Options data</param>
		public Scene(SceneOptions options = new SceneOptions())
		{
			this.options = options;
			this.camera = new Camera(Transform.Identity);
			this.ambientLightColor = new Color(1, 1, 1);
			this.entities = new HashSet<SceneEntity>();
			this.emissiveEntities = new HashSet<SceneEntity>();
			this.lights = new HashSet<PointLight>();
			this.animations = new HashSet<Animation>();
		}

		/// <summary>
		/// Set the camera for the scene.
		/// </summary>
		/// <param name="camera">Camera object</param>
		public void SetCamera(Camera camera)
		{
			this.camera = camera;
		}

		/// <summary>
		/// Rotate the local coordinate basis vectors of the camera to the world space
		/// </summary>
		/// <param name="cameraPosition">the local position of camera</param>
		/// <param name="right">horizaontal basis</param>
		/// <param name="up">vertical basis</param>
		public void GetCameraBasis(out Vector3 cameraPosition, out Vector3 right, out Vector3 up)
		{
			cameraPosition = this.camera.Transform.Position;
			Quaternion q = this.camera.Transform.Rotation;

			right = q.Rotate(new Vector3(1, 0, 0)).Normalized();
			up = q.Rotate(new Vector3(0, 1, 0)).Normalized();
		}

		/// <summary>
		/// Set the ambient light color for the scene.
		/// </summary>
		/// <param name="color">Color object</param>
		public void SetAmbientLightColor(Color color)
		{
			this.ambientLightColor = color;
		}

		/// <summary>
		/// Add an entity to the scene that should be rendered.
		/// </summary>
		/// <param name="entity">Entity object</param>
		public void AddEntity(SceneEntity entity)
		{
			this.entities.Add(entity);
			if (entity.IsEmissive()) emissiveEntities.Add(entity);
		}

		/// <summary>
		/// Add a point light to the scene that should be computed.
		/// </summary>
		/// <param name="light">Light structure</param>
		public void AddPointLight(PointLight light)
		{
			this.lights.Add(light);
		}

		/// <summary>
		/// Add an animation to the scene.
		/// </summary>
		/// <param name="animation">Animation object</param>
		public void AddAnimation(Animation animation)
		{
			this.animations.Add(animation);
		}

		/// <summary>
		///  Randomly pick an sample on the disk
		/// <param name="rng"> random seed</param>
		/// <returns>a random postion on the disk</returns>
		private static (double x, double y) RandomDiskSampling(Random rng)
		{

			double Sample_degree = 2.0 * Math.PI * rng.NextDouble();
			double Sample_radius = Math.Sqrt(rng.NextDouble()) ;
			double Sample_X = Sample_radius * Math.Cos(Sample_degree);
			double Sample_Y = Sample_radius * Math.Sin(Sample_degree);

			return (Sample_X, Sample_Y);
		}

		/// <summary>
		/// Apply tone mapping to prevent overexposure and improve visual quality.
		/// </summary>
		/// <param name="color">Input color in linear space</param>
		/// <returns>Tone mapped color</returns>
		private Color ToneMap(Color color)
		{
			// Simple Reinhard tone mapping
			double r = color.R / (1.0 + color.R);
			double g = color.G / (1.0 + color.G);
			double b = color.B / (1.0 + color.B);
			
			// Clamp to [0,1] range
			r = Math.Max(0.0, Math.Min(1.0, r));
			g = Math.Max(0.0, Math.Min(1.0, g));
			b = Math.Max(0.0, Math.Min(1.0, b));
			
			return new Color(r, g, b);
		}

		/// <summary>
		/// Render the scene to an output image. This is where the bulk
		/// of your ray tracing logic should go... though you may wish to
		/// break it down into multiple functions as it gets more complex!
		/// </summary>
		/// <param name="outputImage">Image to store render output</param>
		/// <param name="time">Time since start in seconds</param>
		public void Render(Image outputImage, double time = 0)
		{
			// Figure out how the image is scaled in the world (the image "plane").
			ComputeWorldImageBounds(outputImage);

			Color background = new(0f, 0f, 0f);
			int N = Math.Max(1, this.options.AAMultiplier); // SSAA - samples per axis

			RenderAllPixles(outputImage, N, background);
		}

		/// <summary>
		/// Iterating and rendering all pixles' color
		/// default background color is black 
		/// using SSAA for single pixel render, Samples per pxiels = N * N 
		/// </summary>
		/// <param name="outputImage">Image to store render output</param>
		/// <param name="N">AA samples per axis</param>
		/// <param name="background"></param>
		private void RenderAllPixles(Image outputImage, int N, Color background)
		{
			float width = outputImage.Width;
			float height = outputImage.Height;


			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					Color pixelColor = SinglePixlesAA(x, y, width, height, N, background, _rng);
					outputImage.SetPixel(x, y, pixelColor);
				}
			}
		}


		/// <summary>
		/// Using Super-sampling anti-aliasing (SSAA) for each pixels, Samples per pxiels = N * N 
		/// when N = 1, using central sampling 
		/// for each pxiles, using DOF to calculate color 
		/// <param name="x">iteration index in width</param>
		/// <param name="y">siteration index in height</param>
		/// <param name="N">samples per axis</param>
		private Color SinglePixlesAA(int x, int y, float width, float height, int N, Color background, Random rng)
		{
			// Central Sampling 
			if (N <= 1)
			{
				// convert into NDC (0, 1)
				float u = (x + 0.5f) / width;
				float v = (y + 0.5f) / height;

				// return SampleWithDOF(u, v, background, rng); // this is for DOF
				Ray ray = BuildPrimaryRay(u, v);
				return RayTrace(ray, background);
			}

			Color AA_result = new(0f, 0f, 0f);
			int smaples_per_pxiels = N * N;
			
			// Super sampling anti alisaing, with N * N samples
			for (int i = 0; i < N; i++)
			{
				for (int j = 0; j < N; j++)
				{
					// randomly pick an number in [0, 1), 
					float random_pick_x = (float)rng.NextDouble();
					float random_pick_y = (float)rng.NextDouble();

					// randomly pick a sample located within the horizontal and vertical range of N*N grid;
					float u = (x + (i + random_pick_x) / N) / width;
					float v = (y + (j + random_pick_y) / N) / height;

					Ray ray = BuildPrimaryRay(u, v);
					AA_result += RayTrace(ray, background);

					// Color DOF_Sampling_Color = SampleWithDOF(u, v, background, rng);  // this is for DOF
					// AA_result += DOF_Sampling_Color;  // this is for DOF
				}
			}

			// calculate average color for 
			AA_result /= smaples_per_pxiels;
			return AA_result; 
		}

		/// <summary>
		///  Calculate Color based on depth of field 
		/// </summary>
		/// <param name="u"></param>
		/// <param name="v"></param>
		/// <param name="background"></param>
		/// <param name="rng"></param>
		/// <returns></returns>
		private Color SampleWithDOF(float u, float v, Color background, Random rng)
		{
			Ray primary = BuildPrimaryRay(u, v);
			double aperture = this.options.ApertureRadius;
			double focalLen = this.options.FocalLength;

			Vector3 focalPoint = primary.Origin + (float)focalLen * primary.Direction;

			// pinhole projection if aperature <= 0; 
			if (aperture <= 0.0)
				return RayTrace(primary, background);

			GetCameraBasis(out Vector3 cameraPosition, out Vector3 camRight, out Vector3 camUp);

			var (x, y) = RandomDiskSampling(rng);

			Vector3 apertureOffset = x * aperture * camRight + y * aperture * camUp;

			Vector3 Origin = cameraPosition + apertureOffset;
			Vector3 Direction = (focalPoint - Origin).Normalized();
			
			return RayTrace(new Ray(Origin, Direction), background);
		}

		/// <summary>
		/// Build a ray from camer indicating to corresponding NDC (u, v)
		/// </summary>
		/// <param name="u"> horizontal index of NDC </param>
		/// <param name="v">vertical index of NDC</param>
		/// <returns>Builted ray</returns>
		private Ray BuildPrimaryRay(float u, float v)
		{
			// conver NDC into camera plane 
			float x_Camera = _imagePlaneWidth * (u - 0.5f);
			float y_Camera = _imagePlaneHeight * (0.5f - v);
			Vector3 direction_Camera = new Vector3(x_Camera, y_Camera, 1.0f).Normalized();

			// Convert local space into world space 
			Quaternion q = this.camera.Transform.Rotation;
			Vector3 direction_world = q.Rotate(direction_Camera).Normalized();
			Vector3 origin_world = this.camera.Transform.Position;

			return new Ray(origin_world, direction_world);
		}

		/// <summary>
		/// Convert output image width and height to horizontal FOV
		/// </summary>
		/// <param name="outputImage">image we want to output</param>
		private void ComputeWorldImageBounds(Image outputImage)
		{
			ArgumentNullException.ThrowIfNull(outputImage);

			var aspectRatio = (float)outputImage.Width / outputImage.Height;
			var fovLength = (float)Math.Tan(Math.PI * fieldOfView / 360.0) * 2f;

			// here, we using horizontal FOV
			this._imagePlaneWidth = fovLength;
			this._imagePlaneHeight = _imagePlaneWidth / aspectRatio;

		}

		/// <summary>
		/// Ray Tracing, rendering color with the same as closetest entity material's color 
		/// After getting local color, we will also keep compute reflection and refraction from hit postion 
		/// </summary>
		/// <param name="ray">ray to check</param>
		/// <param name="background">backround color </param>
		/// <param name="depth">recursive depth for refraction and reflection</param>
		/// <returns></returns>
		private Color RayTrace(Ray ray, Color background, int depth = 0)
		{
			double closestT = float.PositiveInfinity;
			RayHit hitRay = null;
			Material closestMaterial = null;

			foreach (SceneEntity entity in this.entities)
			{
				RayHit hit = entity.Intersect(ray);
				if (hit != null && hit.T > Vector3.EPS && hit.T < closestT)
				{
					closestT = hit.T;
					hitRay = hit;
					closestMaterial = entity.Material;
				}
			}

			// this ray does not hit anything, we can just return background color 
			if (hitRay == null || closestMaterial == null) return background;

			// calculate local lighting
			Color localColor = ComputeLocalColor(hitRay, closestMaterial);
			Color result = localColor;

			// Calculate reflection and refraction 
			double kr = closestMaterial.Reflectivity;
			double kt = closestMaterial.Transmissivity;

			// Add Reflection
			if (kr > 0.0 && depth < REFLECTION_MAX_DEPTH)
			{
				// Reflection Color = Trace(Reflected, depth +1) * kr; kr ∈ [0, 1]
				Color reflection = ComputeReflectionTrace(hitRay, background, depth) * Math.Clamp(kr, 0.0, 1.0);
				result += reflection;
			}

			// Add Refraction
			if (kt > 0.0 && depth < REFRACTION_MAX_DEPTH)
			{
				// Refraction Color = Trace(Refracted, depth + 1) * kt;  kt ∈ [0, 1]
				Color refraction = ComputeRefractionTrace(hitRay, background, depth) * Math.Clamp(kt, 0.0, 1.0);
				result += refraction;
			}

			return result;
		}

		/// <summary>
		/// Compute Local Color, where Color = C_a + Hard Shadows Color With PointLight + Soft Shadow Color With Emissive Entities
		/// </summary>
		/// <param name="hitRay">hit ray to check</param>
		/// <param name="material">the material of hitted object</param>
		/// <returns>local color</returns>
		private Color ComputeLocalColor(RayHit hitRay, Material material)
		{
			if (material.IsEmissive)
				return material.EmissionColor;

			// Get texture coordinates if available
			TextureCoord texCoord = new TextureCoord(0, 0);
			if (hitRay.HasUV)
			{
				texCoord = new TextureCoord(hitRay.U.Value, hitRay.V.Value);
			}

			// Get diffuse color from texture if available
			Color diffuseColor = material.GetDiffuseColor(texCoord);
			Color C_a = diffuseColor * ambientLightColor;
			Color C_local = C_a;

			// Get normal from normal map if available
			Vector3 N = material.GetNormal(texCoord, hitRay.Normal);

			foreach (var light in this.lights)
			{
				C_local += HardShadowsWithPointLight(hitRay, material, light, diffuseColor, N);
			}

			int shadowSamples = Math.Max(25, this.options.AAMultiplier * this.options.AAMultiplier);
			C_local += SoftShadowWithEmissiveEntities(hitRay, material, shadowSamples, _rng, diffuseColor, N);

			return C_local;
		}


		/// <summary>
		/// Get Hard Shadows from  a point light 
		/// Calculate Sum of Diffuse reflection and specular reflection 
		/// </summary>
		/// <param name="hitRay"> the ray hitting object</param>
		/// <param name="material"> the material of hitted object </param>
		/// <param name="light">the point light we want to calculate</param>
		/// <returns>Color of Hard Shadows</returns>
		private Color HardShadowsWithPointLight(RayHit hitRay, Material material, PointLight light, Color diffuseColor, Vector3 N)
		{
			// if this light is blacked by other entity, there is no specular or diffuse reflection from this light
			if (!IsRayVisible(hitRay, light.Position)) return new Color(0, 0, 0);

			// direction from the hit point to the light
			Vector3 L = (light.Position - hitRay.Position).Normalized();
			// direction towards the camera
			Vector3 V = (-hitRay.Incident).Normalized();

			// Calculate distance attenuation
			double distance = (light.Position - hitRay.Position).Length();
			double attenuation = 1.0 / (1.0 + 0.1 * distance + 0.01 * distance * distance);

			// Diffuse reflection: C_d = C_md * C_l * max(0, N · L) * attenuation
			Color C_d = diffuseColor * light.Color * MathF.Max(0, (float)Vector3.Dot(N, L)) * attenuation;

			// R (mirror reflection direction of the light) = (- L) - 2.0f * Dot(-L, N) * N
			Vector3 R = ((-L) - 2f * Vector3.Dot(-L, N) * N).Normalized();

			// Specular reflection: C_s = C_ms * C_l * [max(0, R · V)]^(n_s) * attenuation
			double n_s = (material.Shininess > 0f) ? material.Shininess : 0f;
			double dot_RV = MathF.Max(0, (float)Vector3.Dot(R, V));
			Color C_s = material.SpecularColor * light.Color * MathF.Pow((float)dot_RV, (float)n_s);

			return C_d + C_s;
		}


		/// <summary>
		/// Get Soft Shadows from  an Emissive Entities
		///  Calculate Sum of Diffuse reflection and specular reflection and then multiply visibility ratio
		/// </summary>
		/// <param name="hitRay"> the ray hitting object</param>
		/// <param name="material"> the material of hitted object </param>
		/// <param name="samples_N">Numbeer of samples want to pick</param>
		/// <param name="rng">random seed </param>
		/// <returns>Color of Soft Shadows</returns>
		private Color SoftShadowWithEmissiveEntities(RayHit hitRay, Material material, int samples_N, Random rng, Color diffuseColor, Vector3 N)
		{
			if (samples_N <= 0 || emissiveEntities.Count == 0) return new Color(0,0,0);

			Color result = new(0,0,0);

			// direction towards the camera
   			Vector3 V = (-hitRay.Incident).Normalized();

			foreach (var emitter in emissiveEntities)
			{
				double visibility_ratio = VisibilityRatio(hitRay, emitter, samples_N, rng);

				Vector3 sample = emitter.RandomSamplePostion(rng);
				// direction from the hit point to the emitter 
				Vector3 L = (sample - hitRay.Position).Normalized();

				Color C_l = emitter.EmissionColor();

				// Diffuse reflection: C_d = C_md * C_l * max(0, N · L)
				Color C_d = diffuseColor * C_l * MathF.Max(0, (float)Vector3.Dot(N, L));

				// R (mirror reflection direction of the light) = (- L) - 2.0f * Dot(-L, N) * N
				Vector3 R = ((-L) - 2f * Vector3.Dot(-L, N) * N).Normalized();

				// Specular reflection: C_s = C_ms * C_l * [max(0, R · V)]^(n_s)
				double n_s = (material.Shininess > 0f) ? material.Shininess : 0f;
				double dot_RV = MathF.Max(0, (float)Vector3.Dot(R, V));
				Color C_s = material.SpecularColor * C_l * MathF.Pow((float)dot_RV, (float)n_s);

				result += (C_d + C_s) * visibility_ratio;

			}

			return result;
		}


		/// <summary>
		///  Calculate the visible ratio of an emitter, from the starting point of hitRay.
		/// </summary>
		/// <param name="hitRay">the ray hitting the object</param>
		/// <param name="light">the area light want to check </param>
		/// <param name="N">Numbeer of samples want to pick</param>
		/// <param name="rng">random seed </param>
		/// <returns></returns>
		private double VisibilityRatio(RayHit hitRay, SceneEntity emitter, int N, Random rng)
		{
			 if (N <= 1)
				{
					Vector3 sample = emitter.RandomSamplePostion(rng);
					return IsRayVisible(hitRay, sample, emitter) ? 1f : 0f;
				}

			int visibleSamples = 0;

			// Repet N times to randomly pick a sample, and check whether it is visible 
			for (int i = 0; i < N; i++)
			{
				Vector3 sample = emitter.RandomSamplePostion(rng);
				if (IsRayVisible(hitRay, sample, emitter))
					visibleSamples++;
			}

			double ratio = visibleSamples / (double)N;
			return ratio;
		}


		/// <summary>
		/// Check if this light ray is blocked by other objects (visiable)
		/// </summary>
		/// <param name="hitRay">the ray hitting the object </param>
		/// <param name="light_Position">the position of light</param>
		/// <returns></returns>
		private bool IsRayVisible(RayHit hitRay, Vector3 light_Position, SceneEntity emitter = null)
		{
			Vector3 Direction = (light_Position - hitRay.Position).Normalized();
			Ray shadowRay = hitRay.SpawnOffsetRay(Direction);

			double lightDistance = (light_Position - hitRay.Position).Length();

			// find if there is any entity block the light 
			foreach (SceneEntity entity in this.entities)
			{
				// skip the emitter itself
				if (entity == emitter) continue;
				
				RayHit hit = entity.Intersect(shadowRay);
				if (hit != null)
					{
						double hitDistance = (hit.Position - hitRay.Position).Length();

						// This entity is an obstruction located between the origin and the light
						if (hitDistance > Vector3.EPS && hitDistance < (lightDistance - Vector3.EPS)) { return false; }
					}
				
			}

			// no entity is block between the origin of shadow ray and the light. This pixel is visible 
			return true;
		}
		

		/// <summary>
		/// Recursively calculate reflection color
		/// </summary>
		/// <param name="depth">recursive depth</param>
		/// <returns> Trace, a recursive function that computes the reflection color at ta given ray</returns>
		private Color ComputeReflectionTrace(RayHit hitRay, Color background, int depth)
		{
			Vector3 R = hitRay.GetReflectionDirection();
			Ray reflRay = hitRay.SpawnOffsetRay(R);

			return RayTrace(reflRay, background, depth + 1);
		}

		/// <summary>
		/// Recursively calculate refraction color
		/// </summary>
		/// <param name="depth">recursive depth</param>
		/// <returns> Trace, a recursive function that computes the refraction color at ta given ray</returns>
		private Color ComputeRefractionTrace(RayHit hitRay, Color background, int depth)
		{			
			Vector3? T = hitRay.GetRefractionDirection();

			if (T != null)
			{
				Ray refrRay = hitRay.SpawnOffsetRay(T.Value);
				return RayTrace(refrRay,  background, depth + 1);
			}
			else
			{
				// Total internal reflection, no refraction contribute 
				return new Color(0, 0, 0); ;
			}
			
		}

    }
}


