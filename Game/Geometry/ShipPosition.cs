using System;
using System.Collections.Generic;

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

		public bool IsInsideMap()
		{
			return coord.IsInsideMap();
		}

		public int DistanceTo(Coord target)
		{
			var bowDist = bow.DistanceTo(target);
			if (bowDist == 0)
				return 0;
			var dist = coord.DistanceTo(target);
			if (dist == 0)
				return 0;
			var sternDist = stern.DistanceTo(target);
			if (sternDist == 0)
				return 0;
			return bowDist;
		}

		public List<ShipPosition> Apply(ShipMoveCommand moveCommand)
		{
			var result = new List<ShipPosition>();
			var newSpeed = speed;
			switch (moveCommand)
			{
				case ShipMoveCommand.Faster:
					newSpeed++;
					break;
				case ShipMoveCommand.Slower:
					newSpeed--;
					break;
			}
			if (newSpeed > Constants.MAX_SHIP_SPEED)
				newSpeed = Constants.MAX_SHIP_SPEED;
			if (newSpeed < 0)
				newSpeed = 0;
			var movedShip = this;
			for (var sp = 1; sp <= newSpeed; sp++)
			{
				var newShip = new ShipPosition(movedShip.coord.Neighbor(orientation), orientation, sp);
				if (!newShip.IsInsideMap())
					break;
				movedShip = newShip;
			}
			if (movedShip.speed != newSpeed)
				movedShip = new ShipPosition(movedShip.coord, orientation, 0);
			result.Add(movedShip);
			switch (moveCommand)
			{
				case ShipMoveCommand.Port:
					movedShip = new ShipPosition(movedShip.coord, (orientation + 1) % 6, movedShip.speed);
					break;
				case ShipMoveCommand.Starboard:
					movedShip = new ShipPosition(movedShip.coord, (orientation + 5) % 6, movedShip.speed);
					break;
			}
			result.Add(movedShip);
			return result;
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

		public override string ToString()
		{
			return $"{nameof(coord)}: {coord}, {nameof(orientation)}: {orientation}, {nameof(speed)}: {speed}";
		}
	}
}