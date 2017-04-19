using System.Runtime.CompilerServices;

namespace Game.Geometry
{
	public static class FastShipPosition
	{
		public const int coordMask = (1 << FastCoord.bits) - 1;
		public const int speedShift = FastCoord.bits;
		public const int speedBits = 2;
		public const int speedMask = (1 << (speedBits + speedShift)) - 1 - coordMask;
		public const int orientationShift = speedBits + speedShift;
		public const int orientationBits = 3;
		public const int orientationMask = (1 << (orientationBits + orientationShift)) - 1 - coordMask - speedMask;
		public const int count = (orientationMask | coordMask | speedMask) + 1;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int Create(int x, int y, int orientation, int speed)
		{
			return (FastCoord.Create(x, y) * 6 + orientation) * Constants.MAX_SHIP_SPEED + speed;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int Create(ShipPosition position)
		{
			return Create(position.coord.x, position.coord.y, position.orientation, position.speed);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int Coord(int fastShipPosition)
		{
			return fastShipPosition & coordMask;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int Bow(int fastShipPosition)
		{
			return FastCoord.Neighbor(Coord(fastShipPosition), Orientation(fastShipPosition));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int Stern(int fastShipPosition)
		{
			return FastCoord.Neighbor(Coord(fastShipPosition), (Orientation(fastShipPosition) + 3) % 6);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int Speed(int fastShipPosition)
		{
			return fastShipPosition & speedMask >> speedShift;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int Orientation(int fastShipPosition)
		{
			return fastShipPosition >> orientationShift;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ShipPosition ToShipPosition(int fastShipPosition)
		{
			return new ShipPosition(FastCoord.ToCoord(Coord(fastShipPosition)), Orientation(fastShipPosition), Speed(fastShipPosition));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool Collides(int fastShipPosition, int otherFastCoord)
		{
			return Coord(fastShipPosition) == otherFastCoord
			       || Bow(fastShipPosition) == otherFastCoord
			       || Stern(fastShipPosition) == otherFastCoord;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool CollidesShip(int fastShipPosition, int otherFastShipPosition)
		{
			return Collides(fastShipPosition, Coord(otherFastShipPosition))
			       || Collides(fastShipPosition, Bow(otherFastShipPosition))
			       || Collides(fastShipPosition, Stern(otherFastShipPosition));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int DistanceTo(int fastShipPosition, int targetFastCoord)
		{
			var dist = FastCoord.Distance(Coord(fastShipPosition), targetFastCoord);
			if (dist == 0)
				return 0;
			var bowDist = FastCoord.Distance(Bow(fastShipPosition), targetFastCoord);
			if (bowDist == 0)
				return 0;
			if (bowDist < dist)
				return bowDist;
			var sternDist = FastCoord.Distance(Stern(fastShipPosition), targetFastCoord);
			if (sternDist == 0)
				return 0;
			return dist;
		}
	}
}