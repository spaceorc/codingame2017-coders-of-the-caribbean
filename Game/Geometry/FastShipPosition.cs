using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Game.Geometry
{
	public static class FastShipPosition
	{
		private const int coordBits = 10;
		private const int coordMask = (1 << coordBits) - 1;
		private const int speedShift = coordBits;
		private const int speedBits = 2;
		private const int speedMask = (1 << (speedBits + speedShift)) - 1 - coordMask;
		private const int orientationShift = speedBits + speedShift;
		private const int orientationBits = 3;
		private const int orientationMask = (1 << (orientationBits + orientationShift)) - 1 - coordMask - speedMask;
		private const int count = (orientationMask | coordMask | speedMask) + 1;

		private const int positionBits = orientationShift + orientationBits;
		private const int moveCommandBits = 3;
		private const int movesCount = 1 << (positionBits + moveCommandBits);
		private static bool initialized;
		private static readonly uint[] moves = new uint[movesCount];

		public static void Init()
		{
			if (initialized)
				return;
			initialized = true;
			FastCoord.Init();

			for (int speed = 0; speed <= Constants.MAX_SHIP_SPEED; speed++)
				for (int orientation = 0; orientation < 6; orientation++)
					for (int x = 0; x < Constants.MAP_WIDTH; x++)
						for (int y = 0; y < Constants.MAP_HEIGHT; y++)
						{
							foreach (var moveCommand in ShipMoveCommands.all)
							{
								var shipPosition = new ShipPosition(new Coord(x, y), orientation, speed);
								var nextPositions = shipPosition.Apply(moveCommand);
								if (nextPositions.Count != 2)
									throw new InvalidOperationException("nextPositions.Count != 2");
								var fastShipPosition = Create(shipPosition);
								var moved = 0u;
								for (int phase = 0; phase < nextPositions.Count; phase++)
									moved = unchecked((moved << 16) | (uint)Create(nextPositions[phase]));
								moves[fastShipPosition | ((int)moveCommand << positionBits)] = moved;
							}
						}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int Create(int x, int y, int orientation, int speed)
		{
			return Create(FastCoord.Create(x, y), orientation, speed);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int Create(int coord, int orientation, int speed)
		{
			return (coord & coordMask) | (speed << speedShift) | (orientation << orientationShift);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int Create(ShipPosition position)
		{
			return Create(position.coord.x, position.coord.y, position.orientation, position.speed);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int Coord(int fastShipPosition)
		{
			return (fastShipPosition & coordMask) == coordMask ? -1 : (fastShipPosition & coordMask);
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
			return (fastShipPosition & speedMask) >> speedShift;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int SetSpeed(int fastShipPosition, int speed)
		{
			return (fastShipPosition & ~speedMask) | (speed << speedShift);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int Orientation(int fastShipPosition)
		{
			return fastShipPosition >> orientationShift;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsInsideMap(int fastShipPosition)
		{
			return FastCoord.IsInsideMap(Coord(fastShipPosition));
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
		public static bool CenterCollides(int fastShipPosition, int otherFastCoord)
		{
			return Coord(fastShipPosition) == otherFastCoord;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool BowCollides(int fastShipPosition, int otherFastCoord)
		{
			return Bow(fastShipPosition) == otherFastCoord;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool SternCollides(int fastShipPosition, int otherFastCoord)
		{
			return Stern(fastShipPosition) == otherFastCoord;
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
			var bowDist = FastCoord.Distance(Bow(fastShipPosition), targetFastCoord);
			if (bowDist == 0)
				return 0;
			var dist = FastCoord.Distance(Coord(fastShipPosition), targetFastCoord);
			if (dist == 0)
				return 0;
			var sternDist = FastCoord.Distance(Stern(fastShipPosition), targetFastCoord);
			if (sternDist == 0)
				return 0;
			return bowDist;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int Mine(int fastShipPosition)
		{
			var backOrientation = (Orientation(fastShipPosition) + 3) % 6;
			return FastCoord.Neighbor(FastCoord.Neighbor(Coord(fastShipPosition), backOrientation), backOrientation);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static uint Move(int fastShipPosition, ShipMoveCommand moveCommand)
		{
			return moves[fastShipPosition | ((int)moveCommand << positionBits)];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static uint Move(int moved, int final)
		{
			return unchecked(((uint)moved << 16) | (uint)final);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int GetMovedPosition(uint moved)
		{
			return unchecked((int)(moved >> 16));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int GetFinalPosition(uint moved)
		{
			return unchecked((int)(moved & ((1 << 16) - 1)));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int GetPositionAtPhase(uint moved, int phase)
		{
			return phase == 0 ? GetMovedPosition(moved) : GetFinalPosition(moved);
		}
	}
}