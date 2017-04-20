using System;
using Game.Geometry;
using Game.State;

namespace Game.Strategy
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
			return Decision.Goto(ftargets[currentTarget]);
		}

		public string Dump(string gameStateRef)
		{
			return $"new {nameof(WalkAroundStrategy)}({shipId}, {gameStateRef}) {{ {nameof(currentTarget)} = {currentTarget}, {nameof(started)} = {started.ToString().ToLower()} }}";
		}
	}
}