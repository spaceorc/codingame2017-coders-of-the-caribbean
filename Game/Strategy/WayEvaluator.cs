using Game.Geometry;

namespace Game.Strategy
{
	// pack: 3
	public static class WayEvaluator
	{
		private static readonly int[] costs = { 5, 5, 3, 1 };

		public static int CalcCost(int startCoord, int endCoord, int enemyCoord)
		{
			if (FastCoord.Distance(startCoord, endCoord) < FastCoord.Distance(enemyCoord, endCoord))
				return 0;

			var startX = FastCoord.GetX(startCoord);
			var startY = FastCoord.GetY(startCoord);

			var endX = FastCoord.GetX(endCoord);
			var endY = FastCoord.GetY(endCoord);

			var deltaX = endX - startX;
			var deltaY = endY - startY;

			var enemyDist = int.MaxValue;
			for (int i = 1; i < 10; i++)
			{
				var x = startX + deltaX * i / 10;
				var y = startY + deltaY * i / 10;

				var dist = FastCoord.Distance(enemyCoord, FastCoord.Create(x, y));
				if (dist < enemyDist)
					enemyDist = dist;
			}

			if (enemyDist >= costs.Length)
				return 0;
			return costs[enemyDist];
		}
	}
}