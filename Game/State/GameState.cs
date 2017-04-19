using System;
using System.Collections.Generic;
using System.Linq;
using Game.Cannons;
using Game.Entities;
using Game.Mining;
using Game.Navigation;
using Game.Prediction;
using Game.Statistics;
using Game.Strategy;

namespace Game.State
{
	public class GameState
	{
		public readonly Dictionary<int, Cannoneer> cannoneers = new Dictionary<int, Cannoneer>();
		public readonly Dictionary<int, Miner> miners = new Dictionary<int, Miner>();
		public readonly Dictionary<int, Navigator> navigators = new Dictionary<int, Navigator>();
		public readonly List<TurnStat> stats = new List<TurnStat>();
		public readonly Forecaster forecaster;
		public readonly Admiral admiral;

		public GameState()
		{
			forecaster = new Forecaster(this);
			admiral = new Admiral(this);
		}

		public Cannoneer GetCannoneer(Ship ship)
		{
			Cannoneer cannoneer;
			if (!cannoneers.TryGetValue(ship.id, out cannoneer))
				cannoneers.Add(ship.id, cannoneer = new Cannoneer(ship.id, this));
			return cannoneer;
		}

		public Miner GetMiner(Ship ship)
		{
			Miner miner;
			if (!miners.TryGetValue(ship.id, out miner))
				miners.Add(ship.id, miner = new Miner(ship.id, this));
			return miner;
		}

		public Navigator GetNavigator(Ship ship)
		{
			Navigator navigator;
			if (!navigators.TryGetValue(ship.id, out navigator))
				navigators.Add(ship.id, navigator = new Navigator(ship.id, this));
			return navigator;
		}

		public void Dump()
		{
			Console.Error.WriteLine("var gameState = new GameState();");
			foreach (var cannoneer in cannoneers)
				Console.Error.WriteLine($"gameState.{nameof(cannoneers)}[{cannoneer.Key}] = {cannoneer.Value.Dump("gameState")};");
			foreach (var miner in miners)
				Console.Error.WriteLine($"gameState.{nameof(miners)}[{miner.Key}] = {miner.Value.Dump("gameState")};");
			foreach (var navigator in navigators)
				Console.Error.WriteLine($"gameState.{nameof(navigators)}[{navigator.Key}] = {navigator.Value.Dump("gameState")};");
			admiral.Dump("gameState.admiral");
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