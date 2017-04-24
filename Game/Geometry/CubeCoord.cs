using System;

namespace Game.Geometry
{
	public class CubeCoord : IEquatable<CubeCoord>
	{
		public CubeCoord(int x, int y, int z)
		{
			this.x = x;
			this.y = y;
			this.z = z;
		}

		public readonly int x;
		public readonly int y;
		public readonly int z;

		public int DistanceTo(CubeCoord dst)
		{
			return (Math.Abs(x - dst.x) + Math.Abs(y - dst.y) + Math.Abs(z - dst.z)) / 2;
		}
		
		public bool Equals(CubeCoord other)
		{
			if (ReferenceEquals(null, other))
				return false;
			if (ReferenceEquals(this, other))
				return true;
			return x == other.x && y == other.y && z == other.z;
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj))
				return false;
			if (ReferenceEquals(this, obj))
				return true;
			if (obj.GetType() != this.GetType())
				return false;
			return Equals((CubeCoord)obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				var hashCode = x;
				hashCode = (hashCode * 397) ^ y;
				hashCode = (hashCode * 397) ^ z;
				return hashCode;
			}
		}

		public static bool operator ==(CubeCoord left, CubeCoord right)
		{
			return Equals(left, right);
		}

		public static bool operator !=(CubeCoord left, CubeCoord right)
		{
			return !Equals(left, right);
		}

		public override string ToString()
		{
			return $"{x}, {y}, {z}";
		}
	}
}