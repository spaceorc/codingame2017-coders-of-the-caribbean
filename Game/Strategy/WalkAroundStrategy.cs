using System;
using Game.Geometry;
using Game.State;

namespace Game.Strategy
{
	public class WalkAroundStrategy : IStrategy
	{
		private static readonly Coord[] targets =
		{
			new Coord(5, 5),
			new Coord(5, Constants.MAP_HEIGHT - 5),
			new Coord(Constants.MAP_WIDTH - 5, Constants.MAP_HEIGHT - 5),
			new Coord(Constants.MAP_WIDTH - 5, 5)
		};

		public readonly GameState gameState;
		public readonly int shipId;

		private int currentTarget;
		private bool started;

		public WalkAroundStrategy(int shipId, GameState gameState)
		{
			this.shipId = shipId;
			this.gameState = gameState;
		}

		public Decision Decide(TurnState turnState)
		{
			var ship = turnState.myShipsById[shipId];
			if (ship.DistanceTo(targets[currentTarget]) < Settings.FREE_WALK_TARGET_REACH_DIST)
			{
				currentTarget = (currentTarget + 1) % targets.Length;
				Console.Error.WriteLine($"New target for {ship.id}: {targets[currentTarget]}");
			}
			if (!started)
			{
				started = true;
				Console.Error.WriteLine($"New target for {ship.id}: {targets[currentTarget]}");
			}
			return Decision.Goto(targets[currentTarget]);
		}

		public string Dump(string gameStateRef)
		{
			return $"new {nameof(WalkAroundStrategy)}({shipId}, {gameStateRef}) {{ {nameof(currentTarget)} = {currentTarget}, {nameof(started)} = {started.ToString().ToLower()} }}";
		}
	}
}