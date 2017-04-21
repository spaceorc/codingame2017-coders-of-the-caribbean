using Game.Geometry;

namespace Game
{
	// pack: 1
	public static class Debug
	{
		public const bool USE_DEBUG = false;

		public static int[] startPositions =
		{
			FastShipPosition.Create(14, 12, 2, 0),
			FastShipPosition.Create(11, 13, 1, 0)
		};

		public static ShipMoveCommand[][] preCommands = {
			new [] { ShipMoveCommand.Faster, ShipMoveCommand.Faster }
		};

		public static ShipMoveCommand[][] postCommands = {
			new [] { ShipMoveCommand.Faster, ShipMoveCommand.Faster },
			new [] { ShipMoveCommand.Wait, ShipMoveCommand.Wait },
		};
	}
}