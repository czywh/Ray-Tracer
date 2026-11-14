
using System;

namespace RayTracer
{
    /// <summary>
    /// Class to represent a texture material that is associated with an entity
    /// that is to be rendered by the ray tracer. This material uses an image
    /// as a texture map for the color and optionally a normal map for surface
    /// details. It extends the base Material class to include texture properties.
    /// </summary>
    public class TextureMaterial : Material
    {
        private Image colorMap;
        private Image normalMap;

        /// <summary>
        /// Construct a new material object.
        /// </summary>
        /// <param name="colorMap">The color map (texture) of the material</param>
        /// <param name="normalMap">The normal map of the material (optional)</param>
        public TextureMaterial(Image colorMap, Image normalMap = null)
            : base(new Color(0, 0, 0), new Color(0, 0, 0), new Color(0, 0, 0), 0, 0, 0, 1)
        {
            this.colorMap = colorMap;
            this.normalMap = normalMap;
        }
		
		

        /// <summary>
		/// Get the diffuse color from the texture map at the given texture coordinate.
		/// </summary>
		/// <param name="coord">Texture coordinate</param>
		/// <returns>Color from texture map</returns>
		public override Color GetDiffuseColor(TextureCoord coord)
		{
			if (colorMap == null) return base.DiffuseColor;

			// Clamp UV coordinates to [0,1] range
			double u = Math.Max(0.0, Math.Min(1.0, coord.U));
			double v = Math.Max(0.0, Math.Min(1.0, coord.V));

			// Flip V coordinate (texture coordinates have V=0 at bottom, image has Y=0 at top)
			v = 1.0 - v;

			// Convert UV coordinates to pixel coordinates with proper scaling
			double x = u * (colorMap.Width - 1);
			double y = v * (colorMap.Height - 1);

			// Use proper rounding instead of truncation
			int pixelX = (int)Math.Round(x);
			int pixelY = (int)Math.Round(y);

			// Clamp coordinates to valid range
			pixelX = Math.Max(0, Math.Min(pixelX, colorMap.Width - 1));
			pixelY = Math.Max(0, Math.Min(pixelY, colorMap.Height - 1));

			// Get sRGB color from texture and convert to linear space
			Color srgbColor = colorMap.GetPixel(pixelX, pixelY);
			return srgbColor.ToLinear();
		}

        /// <summary>
        /// Get the normal vector from the normal map at the given texture coordinate.
        /// </summary>
        /// <param name="coord">Texture coordinate</param>
        /// <param name="geometricNormal">Geometric normal from the surface</param>
        /// <returns>Normal vector from normal map or geometric normal</returns>
        public override Vector3 GetNormal(TextureCoord coord, Vector3 geometricNormal)
        {
            if (normalMap == null)
                return geometricNormal.Normalized();

            // Clamp UV coordinates to [0,1] range
            double u = Math.Max(0.0, Math.Min(1.0, coord.U));
            double v = Math.Max(0.0, Math.Min(1.0, coord.V));

            // Flip V coordinate (texture coordinates have V=0 at bottom, image has Y=0 at top)
            v = 1.0 - v;

            // Convert UV coordinates to pixel coordinates
            int x = (int)(u * (normalMap.Width - 1));
            int y = (int)(v * (normalMap.Height - 1));

            // Clamp coordinates to valid range
            x = Math.Max(0, Math.Min(x, normalMap.Width - 1));
            y = Math.Max(0, Math.Min(y, normalMap.Height - 1));

            // Get normal from normal map (assuming RGB represents XYZ)
            Color normalColor = normalMap.GetPixel(x, y);
            
            // Convert from [0,1] range to [-1,1] range
            double nx = normalColor.R * 2.0 - 1.0;
            double ny = normalColor.G * 2.0 - 1.0;
            double nz = normalColor.B * 2.0 - 1.0;

            Vector3 normalMapNormal = new Vector3(nx, ny, nz).Normalized();

            // For now, return the normal map normal directly
            // In a full implementation, you would transform this to world space
            // using tangent and bitangent vectors
            return normalMapNormal;
        }
    }
}
