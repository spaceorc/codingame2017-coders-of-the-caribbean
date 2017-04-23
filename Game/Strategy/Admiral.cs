using System.Collections.Generic;
using System.Linq;
using Game.Geometry;
using Game.Navigation;
using Game.State;

namespace Game.Strategy
{
	public class Admiral
	{
		public readonly GameState gameState;

		public Admiral(GameState gameState)
		{
			this.gameState = gameState;
		}

		public void Iteration(TurnState turnState)
		{
			var decisions = gameState.strateg.MakeDecisions(turnState);
			var moves = FindBestMoveCommands(turnState, decisions);
			for (var i = 0; i < turnState.myShips.Count; i++)
			{
				if (moves[i] == ShipMoveCommand.Wait)
				{
					gameState.GetCannoneer(turnState.myShips[i]).PrepareToFire(turnState, decisions[i].fireToCoord);
					gameState.GetMiner(turnState.myShips[i]).PrepareToMine(turnState);
				}
			}
			for (var i = 0; i < turnState.myShips.Count; i++)
			{
				if (moves[i] == ShipMoveCommand.Wait)
				{
					if (gameState.GetCannoneer(turnState.myShips[i]).Fire(turnState))
						continue;
					if (gameState.GetMiner(turnState.myShips[i]).Mine(turnState))
						continue;
				}
				turnState.myShips[i].Move(moves[i]);
			}
		}

		private List<ShipMoveCommand> FindBestMoveCommands(TurnState turnState, List<StrategicDecision> decisions)
		{
			var moves = FindBestMoveCommands2(turnState, decisions);
			var isDouble = Settings.USE_DOUBLE_PATHFINDING && turnState.stopwatch.ElapsedMilliseconds < Settings.DOUBLE_PATHFINDING_TIMELIMIT;
			if (isDouble)
				moves = FindBestMoveCommands2(turnState, decisions);
			return moves;
		}

		private List<ShipMoveCommand> FindBestMoveCommands2(TurnState turnState, List<StrategicDecision> decisions)
		{
			var moves = new List<ShipMoveCommand>();
			foreach (var ship in turnState.myShips)
			{
				var decision = decisions[ship.index];
				if (decision.role == StrategicRole.Explicit)
				{
					moves.Add(decision.explicitCommand.Value);
				}
				else
				{
					var navigator = gameState.GetNavigator(ship);
					if (decision.targetCoord.HasValue)
					{
						var path = navigator.FindPath(turnState, decision.targetCoord.Value, GetNavigationMethod(decision));
						moves.Add(path.FirstOrDefault());
						gameState.forecaster.ApplyPath(ship, path);
					}
					else
						moves.Add(ShipMoveCommand.Wait);
				}
			}
			return moves;
		}

		private static NavigationMethod GetNavigationMethod(StrategicDecision decision)
		{
			switch (decision.role)
			{
				case StrategicRole.Approach:
					return NavigationMethod.Approach;
				case StrategicRole.Collector:
					return NavigationMethod.Collect;
				default:
					return NavigationMethod.Default;
			}
		}
	}
}