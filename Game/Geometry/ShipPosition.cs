using System;

namespace Game.Geometry
{
	public class ShipPosition : IEquatable<ShipPosition>
	{
		public readonly Coord bow;
		public readonly Coord stern;
		public readonly Coord coord;
		public readonly int orientation;
		public readonly int speed;

		public ShipPosition(Coord coord, int orientation, int speed)
		{
			this.coord = coord;
			this.orientation = orientation;
			this.speed = speed;
			bow = coord.Neighbor(orientation);
			stern = coord.Neighbor((orientation + 3)%6);
		}

		public bool Collides(Coord other)
		{
			return coord.Equals(other) || bow.Equals(other) || stern.Equals(other);
		}

		public bool CollidesShip(ShipPosition other)
		{
			return Collides(other.coord) || Collides(other.bow) || Collides(other.stern);
		}

		public int DistanceTo(Coord target)
		{
			var dist = coord.DistanceTo(target);
			if (dist == 0)
				return 0;
			var bowDist = bow.DistanceTo(target);
			if (bowDist == 0)
				return 0;
			if (bowDist < dist)
				return bowDist;
			var sternDist = stern.DistanceTo(target);
			if (sternDist == 0)
				return 0;
			return dist;
		}

		public bool Equals(ShipPosition other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return coord.Equals(other.coord) && orientation == other.orientation && speed == other.speed;
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != this.GetType()) return false;
			return Equals((ShipPosition) obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				var hashCode = coord.GetHashCode();
				hashCode = (hashCode * 397) ^ orientation;
				hashCode = (hashCode * 397) ^ speed;
				return hashCode;
			}
		}

		public static bool operator ==(ShipPosition left, ShipPosition right)
		{
			return Equals(left, right);
		}

		public static bool operator !=(ShipPosition left, ShipPosition right)
		{
			return !Equals(left, right);
		}
	}
}