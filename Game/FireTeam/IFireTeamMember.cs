using Game.State;

namespace Game.FireTeam
{
	public interface IFireTeamMember : ITeamMember
	{
		void PrepareToFire(TurnState turnState);
		bool Fire(TurnState turnState);
	}
}