using System;
using System.Collections.Generic;
using System.Linq;
using Game.Entities;
using Game.FireTeam;
using Game.Geometry;
using Game.State;
using Game.Statistics;

namespace Game.Strategy
{
	public class Admiral
	{
		public readonly GameState gameState;
		public readonly Dictionary<int, IStrategy> strategies = new Dictionary<int, IStrategy>();

		public Admiral(GameState gameState)
		{
			this.gameState = gameState;
		}

		public void Iteration(TurnState turnState)
		{
			turnState.stopwatch.Restart();

			NotifyStartTurn(turnState);

			gameState.forecaster.BuildForecast(turnState);
			var moves = Decide(turnState);
			var isDouble = Settings.USE_DOUBLE_PATHFINDING && turnState.stopwatch.ElapsedMilliseconds < Settings.DOUBLE_PATHFINDING_TIMELIMIT;
			if (isDouble)
				moves = Decide(turnState);

			for (var i = 0; i < turnState.myShips.Count; i++)
			{
				if (moves[i] == ShipMoveCommand.Wait)
				{
					gameState.GetCannoneer(turnState.myShips[i]).PrepareToFire(turnState);
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

			NotifyEndTurn(turnState);

			turnState.stopwatch.Stop();
			gameState.stats.Add(new TurnStat { isDouble = isDouble, time = turnState.stopwatch.ElapsedMilliseconds });
			Console.Error.WriteLine($"Decision made in {turnState.stopwatch.ElapsedMilliseconds} ms (isDouble = {isDouble})");
		}

		private void NotifyStartTurn(TurnState turnState)
		{
			foreach (var teamMember in gameState.GetTeam(turnState))
				teamMember.StartTurn(turnState);
		}

		private void NotifyEndTurn(TurnState turnState)
		{
			foreach (var teamMember in gameState.GetTeam(turnState))
				teamMember.EndTurn(turnState);
		}

		private List<ShipMoveCommand> Decide(TurnState turnState)
		{
			var moves = new List<ShipMoveCommand>();
			foreach (var ship in turnState.myShips)
			{
				var action = Decide(turnState, ship);
				var navigator = gameState.GetNavigator(ship);
				switch (action.type)
				{
					case DecisionType.Goto:
						var path = navigator.FindPath(turnState, action.fcoord);
						moves.Add(path.FirstOrDefault());
						gameState.forecaster.ApplyPath(turnState, ship, path);
						break;
					default:
						moves.Add(ShipMoveCommand.Wait);
						break;
				}
			}
			return moves;
		}

		private Decision Decide(TurnState turnState, Ship ship)
		{
			IStrategy strategy;
			if (!strategies.TryGetValue(ship.id, out strategy))
				strategies[ship.id] = strategy = new CollectBarrelsStrategy(ship.id, gameState);
			if (strategy is WalkAroundStrategy)
			{
				var switchStrategy = new CollectBarrelsStrategy(ship.id, gameState);
				var switchAction = switchStrategy.Decide(turnState);
				if (switchAction.type == DecisionType.Goto)
				{
					strategies[ship.id] = switchStrategy;
					return switchAction;
				}
			}
			var action = strategy.Decide(turnState);
			if (action.type == DecisionType.Unknown)
			{
				strategies[ship.id] = strategy = new WalkAroundStrategy(ship.id, gameState);
				action = strategy.Decide(turnState);
			}
			return action;
		}

		public void Dump(string admiralRef)
		{
			foreach (var strategy in strategies)
				Console.Error.WriteLine($"{admiralRef}.{nameof(strategies)}[{strategy.Key}] = {strategy.Value.Dump($"{admiralRef}.{nameof(gameState)}")};");
		}
	}
}