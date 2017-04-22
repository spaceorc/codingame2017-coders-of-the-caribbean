using System;
using System.Collections.Generic;
using Game.Entities;
using Game.State;

namespace Game.Strategy
{
	public class Strateg : ITeamMember
	{
		public readonly GameState gameState;
		public readonly Dictionary<int, IStrategy> strategies = new Dictionary<int, IStrategy>();

		public Strateg(GameState gameState)
		{
			this.gameState = gameState;
		}

		public void StartTurn(TurnState turnState)
		{
		}

		public void EndTurn(TurnState turnState)
		{
		}

		public List<Decision> MakeDecisions(TurnState turnState)
		{
			var result = new List<Decision>();
			foreach (var ship in turnState.myShips)
				result.Add(Decide(turnState, ship));
			return result;
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

		public void Dump(string strategRef)
		{
			foreach (var strategy in strategies)
				Console.Error.WriteLine($"{strategRef}.{nameof(strategies)}[{strategy.Key}] = {strategy.Value.Dump($"{strategRef}.{nameof(gameState)}")};");
		}
	}
}