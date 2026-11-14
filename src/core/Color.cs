using System;

namespace RayTracer
{
    /// <summary>
    /// Immutable structure to represent a color as r/g/b with 0-1 ranges
    /// </summary>
    public readonly struct Color
    {
        private readonly double r, g, b;

        /// <summary>
        /// Construct a new color structure given red, green, blue
        /// components (0-1 ranges).
        /// </summary>
        /// <param name="r">Red component (0-1)</param>
        /// <param name="g">Blue component (0-1)</param>
        /// <param name="b">Green component (0-1)</param>
        public Color(double r, double g, double b)
        {
            this.r = r;
            this.g = g;
            this.b = b;
        }

        /// <summary>
        /// Convert color structure to string.
        /// </summary>
        /// <returns>Color as string in form (r, g, b)</returns>
        public override string ToString()
        {
            return "(" + this.r + "," + this.g + "," + this.b + ")";
        }

        /// <summary>
        /// The red component of the color (0-1).
        /// </summary>
        public double R { get { return this.r; } }

        /// <summary>
        /// The green component of the color (0-1).
        /// </summary>
        public double G { get { return this.g; } }

        /// <summary>
        /// The blue component of the color (0-1).
        /// </summary>
        public double B { get { return this.b; } }

        /// <summary>
        /// Convert sRGB color to linear space for proper lighting calculations.
        /// </summary>
        /// <param name="srgb">sRGB color value (0-1)</param>
        /// <returns>Linear color value (0-1)</returns>
        public static double SRGBToLinear(double srgb)
        {
            if (srgb <= 0.04045)
                return srgb / 12.92;
            else
                return Math.Pow((srgb + 0.055) / 1.055, 2.4);
        }

        /// <summary>
        /// Convert linear color to sRGB space for display.
        /// </summary>
        /// <param name="linear">Linear color value (0-1)</param>
        /// <returns>sRGB color value (0-1)</returns>
        public static double LinearToSRGB(double linear)
        {
            if (linear <= 0.0031308)
                return linear * 12.92;
            else
                return 1.055 * Math.Pow(linear, 1.0 / 2.4) - 0.055;
        }

        /// <summary>
        /// Convert this color from sRGB to linear space.
        /// </summary>
        /// <returns>Linear color</returns>
        public Color ToLinear()
        {
            return new Color(
                SRGBToLinear(this.r),
                SRGBToLinear(this.g),
                SRGBToLinear(this.b)
            );
        }

        /// <summary>
        /// Convert this color from linear to sRGB space.
        /// </summary>
        /// <returns>sRGB color</returns>
        public Color ToSRGB()
        {
            return new Color(
                LinearToSRGB(this.r),
                LinearToSRGB(this.g),
                LinearToSRGB(this.b)
            );
        }

        /// <summary>
        /// Multiply each color component by a single scalar value.
        /// </summary>
        /// <param name="a">Color structure</param>
        /// <param name="b">Scalar multiple</param>
        /// <returns>Multiplied color structure</returns>
        public static Color operator *(Color a, double b)
        {
            return new Color(a.r * b, a.g * b, a.b * b);
        }

        /// <summary>
        /// Multiply two colors together component-wise.
        /// </summary>
        /// <param name="a">First color structure</param>
        /// <param name="b">Second color structure</param>
        /// <returns>Multiplied color structure</returns>
        public static Color operator *(Color a, Color b)
        {
            return new Color(a.r * b.r, a.g * b.g, a.b * b.b);
        }

        /// <summary>
        /// Divide each color component by a single scalar value.
        /// </summary>
        /// <param name="a">Color structure</param>
        /// <param name="b">Scalar divisor</param>
        /// <returns>Divided color structure</returns>
        public static Color operator /(Color a, double b)
        {
            return new Color(a.r / b, a.g / b, a.b / b);
        }

        /// <summary>
        /// Divide two colors component-wise.
        /// </summary>
        /// <param name="a">Color structure</param>
        /// <param name="b">Divisor color structure</param>
        /// <returns>Divided color structure</returns>
        public static Color operator /(Color a, Color b)
        {
            return new Color(a.r / b.r, a.g / b.g, a.b / b.b);
        }

        /// <summary>
        /// Add two colors together component-wise.
        /// </summary>
        /// <param name="a">First color structure</param>
        /// <param name="b">Second color structure</param>
        /// <returns>Multiplied color structure</returns>
        public static Color operator +(Color a, Color b)
        {
            return new Color(a.r + b.r, a.g + b.g, a.b + b.b);
        }

		public static implicit operator double(Color v)
		{
			throw new NotImplementedException();
		}
	}
}
