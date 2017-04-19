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
			MainLoop(Console.In);
		}

		public static void MainLoop(TextReader input)
		{
			while (true)
				Iteration(input);
		}

		public static void Iteration(TextReader input)
		{
			gameState.currentTurn += 2;
			turnState = TurnState.ReadFrom(input);
			Console.Error.WriteLine("Current turn: " + gameState.currentTurn);
			if (gameState.currentTurn == Settings.DUMP_TURN)
			{
				turnState.WriteTo(Console.Error);
				Console.Error.WriteLine("===");
				gameState.Dump();
			}
			gameState.admiral.Iteration(turnState);
			if (gameState.currentTurn == Settings.DUMP_STAT_TURN)
				gameState.DumpStats();
		}
	}
}