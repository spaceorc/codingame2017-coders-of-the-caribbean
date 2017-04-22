using System.Collections.Generic;
using Game.State;

namespace Game.Strategy
{
	public class StrategicDecision
	{
		public string role;
		public int? targetCoord;
	}

	public interface IStrateg : ITeamMember
	{
		List<StrategicDecision> MakeDecisions(TurnState turnState);
		void Dump(string strategRef);
	}

	public class Strateg : IStrateg
	{
		public readonly GameState gameState;
		public readonly Dictionary<int, StrategicDecision> strategies = new Dictionary<int, StrategicDecision>();
		
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

		public List<StrategicDecision> MakeDecisions(TurnState turnState)
		{
			return null;
		}

		public void Dump(string strategRef)
		{
			// todo
		}
	}
}