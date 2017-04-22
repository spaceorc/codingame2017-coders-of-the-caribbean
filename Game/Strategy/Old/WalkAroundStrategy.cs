using System;
using Game.Geometry;
using Game.State;

namespace Game.Strategy.Old
{
	public class WalkAroundStrategy : IStrategy
	{
		private static readonly int[] ftargets =
		{
			new Coord(5, 5).ToFastCoord(),
			new Coord(5, Constants.MAP_HEIGHT - 5).ToFastCoord(),
			new Coord(Constants.MAP_WIDTH - 5, Constants.MAP_HEIGHT - 5).ToFastCoord(),
			new Coord(Constants.MAP_WIDTH - 5, 5).ToFastCoord()
		};

		public readonly GameState gameState;
		public readonly int shipId;

		public int currentTarget;
		public bool started;

		public WalkAroundStrategy(int shipId, GameState gameState)
		{
			this.shipId = shipId;
			this.gameState = gameState;
		}

		public int? Decide(TurnState turnState)
		{
			var ship = turnState.FindMyShip(shipId);
			if (FastShipPosition.DistanceTo(ship.fposition, ftargets[currentTarget]) < Settings.FREE_WALK_TARGET_REACH_DIST)
			{
				currentTarget = (currentTarget + 1) % ftargets.Length;
				Console.Error.WriteLine($"New target for {ship.id}: {FastCoord.ToCoord(ftargets[currentTarget])}");
			}
			if (!started)
			{
				started = true;
				Console.Error.WriteLine($"New target for {ship.id}: {FastCoord.ToCoord(ftargets[currentTarget])}");
			}
			return ftargets[currentTarget];
		}

		public string Dump(string gameStateRef)
		{
			return $"new {nameof(WalkAroundStrategy)}({shipId}, {gameStateRef}) {{ {nameof(currentTarget)} = {currentTarget}, {nameof(started)} = {started.ToString().ToLower()} }}";
		}
	}
}