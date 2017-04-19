using System;
using System.IO;
using Game.State;

namespace Game
{
	public class Player
	{
		private static TurnState turnState;
		private static readonly GameState gameState = new GameState();

		private static void Main(string[] args)
		{
			// game loop
			var currentTurn = 0;
			while (true)
			{
				currentTurn += 2;
				Iteration(currentTurn, Console.In);
			}
		}

		public static void Iteration(int currentTurn, TextReader input)
		{
			turnState = TurnState.ReadFrom(input);
			Console.Error.WriteLine("Current turn: " + currentTurn);
			if (currentTurn == Settings.DUMP_TURN)
			{
				turnState.WriteTo(Console.Error);
				Console.Error.WriteLine("===");
				gameState.Dump();
			}

			gameState.admiral.Iteration(turnState);

			if (currentTurn == Settings.DUMP_STAT_TURN)
				gameState.DumpStats();
		}
	}
}