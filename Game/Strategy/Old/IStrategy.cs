using Game.State;

namespace Game.Strategy.Old
{
	public interface IStrategy
	{
		int? Decide(TurnState turnState);
		string Dump(string gameStateRef);
	}
}