namespace Game.Geometry
{
	public struct Coord
	{
		private static readonly int[][] DIRECTIONS_EVEN =
			{new[] {1, 0}, new[] {0, -1}, new[] {-1, -1}, new[] {-1, 0}, new[] {-1, 1}, new[] {0, 1}};

		private static readonly int[][] DIRECTIONS_ODD =
			{new[] {1, 0}, new[] {1, -1}, new[] {0, -1}, new[] {-1, 0}, new[] {0, 1}, new[] {1, 1}};

		public Coord(int x, int y)
		{
			this.x = x;
			this.y = y;
		}

		public readonly int x;
		public readonly int y;

		public int DistanceTo(Coord dst)
		{
			return ToCubeCoord().DistanceTo(dst.ToCubeCoord());
		}

		public Coord Neighbor(int orientation)
		{
			int newY, newX;
			if (y % 2 == 1 || y % 2 == -1)
			{
				newY = y + DIRECTIONS_ODD[orientation][1];
				newX = x + DIRECTIONS_ODD[orientation][0];
			}
			else
			{
				newY = y + DIRECTIONS_EVEN[orientation][1];
				newX = x + DIRECTIONS_EVEN[orientation][0];
			}

			return new Coord(newX, newY);
		}

		public override string ToString()
		{
			return $"{x}, {y}";
		}

		public CubeCoord ToCubeCoord()
		{
			var xp = x - (y - (y & 1)) / 2;
			var zp = y;
			var yp = -(xp + zp);
			return new CubeCoord(xp, yp, zp);
		}

		public bool IsInsideMap()
		{
			return x >= 0 && x < Constants.MAP_WIDTH && y >= 0 && y < Constants.MAP_HEIGHT;
		}
	}
}