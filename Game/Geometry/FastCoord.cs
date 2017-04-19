using System.Runtime.CompilerServices;

namespace Game.Geometry
{
	public static class FastCoord
	{
		private static bool initialized;
		private const int bits = 10;
		private const int count = 1 << bits;
		private const int shiftY = bits/2;
		private const int maskX = (1 << shiftY) - 1;
		private const int orientationBits = 3;
		private const int neighborsCount = 1 << (bits + orientationBits);
		private const int distancesCount = (1 << bits) * (1 << bits);
		private static readonly int[] distances = new int[distancesCount];
		private static readonly int[] neighbors = new int[neighborsCount];

		public static void Init()
		{
			if (initialized)
				return;
			initialized = true;
			var coords = new Coord[count];
			for (int x = -1; x < Constants.MAP_WIDTH + 1; x++)
				for (int y = -1; y < Constants.MAP_HEIGHT + 1; y++)
				{
					var coord = new Coord(x, y);
					var fastCoord = Create(coord);
					coords[fastCoord] = coord;
				}
			for (int c = 0; c < coords.Length; c++)
			{
				for (int o = 0; o < 6; o++)
				{
					var neighbor = coords[c].Neighbor(o);
					neighbors[c << orientationBits | o] = Create(neighbor);
				}
			}
			for (int a = 0; a < coords.Length; a++)
			for (int b = 0; b < coords.Length; b++)
			{
				distances[a << bits | b] = coords[a].DistanceTo(coords[b]);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int Create(Coord coord)
		{
			return Create(coord.x, coord.y);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int Create(int x, int y)
		{
			if (x < -1 || y < -1 || x > Constants.MAP_WIDTH || y > Constants.MAP_HEIGHT)
				return -1;
			return ((y + 1) << shiftY) | (x + 1);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Coord ToCoord(int fastCoord)
		{
			return new Coord(GetX(fastCoord), GetY(fastCoord));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsInsideMap(int fastCoord)
		{
			if (fastCoord < 0)
				return false;
			var rawX = fastCoord & maskX;
			if (rawX <= 0 || rawX >= Constants.MAP_WIDTH + 1)
				return false;
			var rawY = fastCoord >> shiftY;
			if (rawY <= 0 || rawY >= Constants.MAP_HEIGHT + 1)
				return false;
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int GetX(int fastCoord)
		{
			return fastCoord < 0 ? -1000 : (fastCoord & maskX) - 1;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int GetY(int fastCoord)
		{
			return fastCoord < 0 ? -1000 : (fastCoord >> shiftY) - 1;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int Neighbor(int fastCoord, int orientation)
		{
			return neighbors[fastCoord << orientationBits | orientation];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int Distance(int fastCoord1, int fastCoord2)
		{
			return fastCoord1 == fastCoord2 ? 0 : distances[fastCoord1 << bits | fastCoord2];
		}
	}
}