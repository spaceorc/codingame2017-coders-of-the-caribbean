using System;
using System.Collections.Generic;
using System.Linq;
using Game.Cannons;
using Game.Entities;
using Game.Statistics;

namespace Game.State
{
	public class GameState
	{
		public readonly Dictionary<int, CannonMaster> cannonMasters = new Dictionary<int, CannonMaster>();
		public readonly List<TurnStat> stats = new List<TurnStat>();

		public CannonMaster GetCannonMaster(Ship ship)
		{
			CannonMaster cannonMaster;
			if (!cannonMasters.TryGetValue(ship.id, out cannonMaster))
				cannonMasters.Add(ship.id, cannonMaster = new CannonMaster(ship.id, this));
			return cannonMaster;
		}
		
		public void Dump()
		{
			Console.Error.WriteLine("var gameState = new GameState();");
			foreach (var cannonMaster in cannonMasters)
				Console.Error.WriteLine($"gameState.cannonMasters[{cannonMaster.Key}] = {cannonMaster.Value.Dump("gameState")}");
			// todo strategies
			// todo miners
		}

		public void DumpStats()
		{
			Console.Error.WriteLine("--- STATISTICS ---");
			Console.Error.WriteLine($"TotalCount: {stats.Count}");
			Console.Error.WriteLine($"DoublePathCount: {stats.Count(t => t.isDouble)}");
			Console.Error.WriteLine($"Time_Max: {stats.Max(t => t.time)}");
			Console.Error.WriteLine($"Time_Avg: {stats.Average(t => t.time)}");
			Console.Error.WriteLine($"Time_95: {stats.Percentile(t => t.time, 95)}");
			Console.Error.WriteLine($"Time_50: {stats.Percentile(t => t.time, 50)}");
			Console.Error.WriteLine($"TimeCorrected_Avg: {stats.Average(t => t.CorrectedTime())}");
			Console.Error.WriteLine($"TimeCorrected_95: {stats.Percentile(t => t.CorrectedTime(), 95)}");
			Console.Error.WriteLine($"TimeCorrected_50: {stats.Percentile(t => t.CorrectedTime(), 50)}");
			Console.Error.WriteLine("---");
		}
	}
}