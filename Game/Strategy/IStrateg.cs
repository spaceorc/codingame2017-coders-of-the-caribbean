using System.Collections.Generic;
using Game.State;

namespace Game.Strategy
{
	public interface IStrateg : ITeamMember
	{
		List<StrategicDecision> MakeDecisions(TurnState turnState);
		void Dump(string strategRef);
	}
}