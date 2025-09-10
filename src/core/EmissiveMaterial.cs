using System;

namespace RayTracer
{
    public class EmissiveMaterial : Material
    {
        private Color emissiveColor;
        private double intensity;

        /// <summary>
        /// Construct a new emissive material.
        /// </summary>
        /// <param name="emissiveColor">The color of emitted light</param>
        /// <param name="intensity">The intensity of emitted light</param>
        public EmissiveMaterial(Color emissiveColor, double intensity = 1.0)
            : base(new Color(0, 0, 0), new Color(0, 0, 0), new Color(0, 0, 0), 0, 0, 0, 1, true, emissiveColor * intensity)
        {
            this.emissiveColor = emissiveColor;
            this.intensity = intensity;
        }
		

        /// <summary>
		/// Get the emissive color of the material.
		/// </summary>
		public Color EmissiveColor { get { return this.emissiveColor; } }

        /// <summary>
        /// Get the intensity of the material.
        /// </summary>
        public double Intensity { get { return this.intensity; } }


    }
}