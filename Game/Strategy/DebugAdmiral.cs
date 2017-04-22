using System.Collections.Generic;
using System.Linq;
using Game.Geometry;
using Game.State;

namespace Game.Strategy
{
	public class DebugAdmiral
	{
		public readonly GameState gameState;
		public int stage;

		public DebugAdmiral(GameState gameState)
		{
			this.gameState = gameState;
		}

		public void Iteration(TurnState turnState)
		{
			var moves = DebugFindBestMoveCommands(turnState);
			for (var i = 0; i < turnState.myShips.Count; i++)
				turnState.myShips[i].Move(moves[i]);
		}

		private List<ShipMoveCommand> DebugFindBestMoveCommands(TurnState turnState)
		{
			var navs = Debug.startPositions.Select((x, i) => gameState.GetDebugNavigator(turnState.myShips[i])).ToList();

			if (stage < Debug.preCommands.Length)
				return Debug.postCommands[stage++].ToList();

			if (stage == Debug.preCommands.Length)
			{
				var paths = navs.Select((n, i) => n.FindPath(turnState, Debug.startPositions[i])).ToArray();
				if (paths.All(p => p.Count == 0))
					stage++;
				else
					return paths.Select(p => p.FirstOrDefault()).ToList();
			}
			var cmdIndex = stage - Debug.preCommands.Length - 1;
			stage++;
			if (cmdIndex < Debug.postCommands.Length)
				return Debug.postCommands[cmdIndex].ToList();
			return new ShipMoveCommand[navs.Count].ToList();
		}
	}
}