using System.Collections.Generic;
using System.Linq;
using Game.Geometry;
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
					gameState.GetCannoneer(turnState.myShips[i]).PrepareToFire(turnState, decisions[i].preferredFireTargetCoord);
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
				var action = decisions[ship.index];
				var navigator = gameState.GetNavigator(ship);
				if (action.targetCoord.HasValue)
				{
					var path = navigator.FindPath(turnState, action.targetCoord.Value);
					moves.Add(path.FirstOrDefault());
					gameState.forecaster.ApplyPath(ship, path);
				}
				else
					moves.Add(ShipMoveCommand.Wait);
			}
			return moves;
		}
	}
}