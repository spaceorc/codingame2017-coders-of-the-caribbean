namespace Game.State
{
	public interface ITeamMember
	{
		void StartTurn(TurnState turnState);
		void EndTurn(TurnState turnState);
	}
}