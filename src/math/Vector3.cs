using System;


namespace RayTracer
{
	/// <summary>
	/// Immutable structure to represent a three-dimensional vector.
	/// </summary>
	public readonly struct Vector3
	{
		private readonly double x, y, z;

		public static readonly Vector3 Zero = new Vector3(0, 0, 0);
		public static readonly Vector3 One = new Vector3(1, 1, 1);

		public const double EPS = 1e-12;

		/// <summary>
		/// Construct a three-dimensional vector.
		/// </summary>
		/// <param name="x">X component</param>
		/// <param name="y">Y component</param>
		/// <param name="z">Z component</param>
		public Vector3(double x, double y, double z)
		{
			this.x = x;
			this.y = y;
			this.z = z;
		}

		/// <summary>
		/// Convert vector to a readable string.
		/// </summary>
		/// <returns>Vector as string in form (x, y, z)</returns>
		public override string ToString()
		{
			return "(" + this.x + "," + this.y + "," + this.z + ")";
		}

		/// <summary>
		/// Compute the length of the vector squared.
		/// This should be used if there is a way to perform a vector
		/// computation without needing the actual length, since
		/// a square root operation is expensive.
		/// </summary>
		/// <returns>Length of the vector squared</returns>
		public double LengthSq()
		{
			// Write your code here...
			return this.x * this.x + this.y * this.y + this.z * this.z;
		}

		/// <summary>
		/// Compute the length of the vector.
		/// </summary>
		/// <returns>Length of the vector</returns>
		public double Length()
		{
			// Write your code here...
			return Math.Sqrt(LengthSq());
		}

		/// <summary>
		/// Compute a length 1 vector in the same direction.
		/// </summary>
		/// <returns>Normalized vector</returns>
		public Vector3 Normalized()
		{
			// Write your code here...
			double lenSq = LengthSq();
			if (lenSq < EPS) return Zero;

			double inverseLength = 1.0 / Length();

			double new_x = this.x * inverseLength;
			double new_y = this.y * inverseLength;
			double new_z = this.z * inverseLength;
			return new Vector3(new_x, new_y, new_z);
		}

		/// <summary>
		/// Compute the dot product with another vector.
		/// </summary>
		/// <param name="u">Vector to dot product </param>
		/// <param name="v">Vector to dot product with</param>
		/// <returns>Dot product result</returns>
		public static double Dot(Vector3 u, Vector3 v)
		{
			// Write your code here...
			return u.x * v.x + u.y * v.y + u.z * v.z;;
		}

		/// <summary>
		/// Compute the cross product with another vector.
		/// </summary>
		/// /// <param name="u">Vector to cross product </param>
		/// <param name="v">Vector to cross product with</param>
		/// <returns>Cross product result</returns>
		public static Vector3 Cross(Vector3 u, Vector3 v)
		{
			double new_x = u.y * v.z - u.z * v.y;
			double new_y = u.z * v.x - u.x * v.z;
			double new_z = u.x * v.y - u.y * v.x;
			return new Vector3(new_x, new_y, new_z);
		}

		/// <summary>
		/// Sum two vectors together (using + operator).
		/// </summary>
		/// <param name="a">First vector</param>
		/// <param name="b">Second vector</param>
		/// <returns>Summed vector</returns>
		public static Vector3 operator +(Vector3 a, Vector3 b)
		{
			// Write your code here...
			return new Vector3(a.x + b.x, a.y + b.y, a.z + b.z);
		}

		/// <summary>
		/// Negate a vector (using - operator)
		/// </summary>
		/// <param name="a">Vector to negate</param>
		/// <returns>Negated vector</returns>
		public static Vector3 operator -(Vector3 a)
		{
			// Write your code here...
			return new Vector3(-a.x, -a.y, -a.z);
		}

		/// <summary>
		/// Subtract one vector from another.
		/// </summary>
		/// <param name="a">Original vector</param>
		/// <param name="b">Vector to subtract</param>
		/// <returns>Subtracted vector</returns>
		public static Vector3 operator -(Vector3 a, Vector3 b)
		{
			// Write your code here...

			return new Vector3(a.x - b.x, a.y - b.y, a.z - b.z);
		}

		/// <summary>
		/// Multiply a vector by a scalar value.
		/// </summary>
		/// <param name="a">Original vector</param>
		/// <param name="b">Scalar multiplier</param>
		/// <returns>Multiplied vector</returns>
		public static Vector3 operator *(Vector3 a, double b)
		{
			// Write your code here...
			return new Vector3(a.x * b, a.y * b, a.z * b);
		}

		/// <summary>
		/// Multiply a vector by a scalar value (opposite operands).
		/// </summary>
		/// <param name="b">Scalar multiplier</param>
		/// <param name="a">Original vector</param>
		/// <returns>Multiplied vector</returns>
		public static Vector3 operator *(double b, Vector3 a)
		{
			// Write your code here...
			return new Vector3(a.x * b, a.y * b, a.z * b);
		}

		/// <summary>
		/// Divide a vector by a scalar value.
		/// </summary>
		/// <param name="a">Original vector</param>
		/// <param name="b">Scalar divisor</param>
		/// <returns>Divided vector</returns>
		public static Vector3 operator /(Vector3 a, double b)
		{
			// Write your code here...
			if (Math.Abs(b) < EPS) throw new DivideByZeroException("Division by zero./");
			
			double new_x = a.x / b;
			double new_y = a.y / b;
			double new_z = a.z / b;
			return new Vector3(new_x, new_y, new_z);
		}

		/// <summary>
		/// X component of the vector.
		/// </summary>
		public double X { get { return this.x; } }

		/// <summary>
		/// Y component of the vector.
		/// </summary>
		public double Y { get { return this.y; } }

        /// <summary>
        /// Z component of the vector.
        /// </summary>
        public double Z { get { return this.z; } }

        /// <summary>
        /// Get component by axis index (0=X, 1=Y, 2=Z).
        /// </summary>
        /// <param name="axis">Axis index (0=X, 1=Y, 2=Z)</param>
        /// <returns>Component value</returns>
        public double GetAxis(int axis)
        {
            return axis switch
            {
                0 => this.x,
                1 => this.y,
                2 => this.z,
                _ => throw new ArgumentOutOfRangeException(nameof(axis))
            };
        }
    }
}
