using System;

namespace Game.Geometry
{
	public struct CubeCoord
	{
		private static readonly int[][] directions =
		{
			new[] {1, -1, 0},
			new[] {+1, 0, -1},
			new[] {0, +1, -1},
			new[] {-1, +1, 0},
			new[] {-1, 0, +1},
			new[] {0, -1, +1}
		};

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

		public CubeCoord Neighbor(int orientation)
		{
			var nx = x + directions[orientation][0];
			var ny = y + directions[orientation][1];
			var nz = z + directions[orientation][2];

			return new CubeCoord(nx, ny, nz);
		}

		public Coord ToCoord()
		{
			var newX = x + (z - (z & 1)) / 2;
			var newY = z;
			return new Coord(newX, newY);
		}
	}
}