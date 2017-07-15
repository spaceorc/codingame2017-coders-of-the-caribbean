using System;
using System.IO;
using Game.State;

namespace Game
{
    // packOptions: compact
	public class EntryPoint
	{
		private static readonly GameState gameState = new GameState();

		private static void Main(string[] args)
		{
			MainLoop(Console.In);
		}

		public static void MainLoop(TextReader input)
		{
			while (true)
				gameState.Iteration(input);
		}
	}
}