using Game.Geometry;

namespace Game
{
	// pack: 1
	public static class Debug
	{
		public const bool USE_DEBUG = false;

		public static int[] startPositions =
		{
			FastShipPosition.Create(7, 5, 1, 0),
			FastShipPosition.Create(9, 6, 2, 0)
		};

		public static ShipMoveCommand[][] debugCommands = {
			new [] { ShipMoveCommand.Faster, ShipMoveCommand.Wait },
			new [] { ShipMoveCommand.Faster, ShipMoveCommand.Faster },
		};
	}
}