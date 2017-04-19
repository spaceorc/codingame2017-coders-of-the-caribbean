using Game.State;

namespace Game.Strategy
{
	public interface IStrategy
	{
		Decision Decide(TurnState turnState);
		string Dump(string gameStateRef);
	}
}