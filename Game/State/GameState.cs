using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Entities;
using Game.FireTeam;
using Game.Geometry;
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
		public readonly IStrateg strateg;
		public int currentTurn;

		public GameState()
		{
			FastCoord.Init();
			FastShipPosition.Init();
			forecaster = new Forecaster(this);
			admiral = new Admiral(this);
			strateg = new Strateg(this);
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

		public IEnumerable<ITeamMember> GetTeam(TurnState turnState)
		{
			yield return forecaster;
			yield return strateg;
			foreach (var ship in turnState.myShips)
			{
				yield return GetCannoneer(ship);
				yield return GetNavigator(ship);
				yield return GetMiner(ship);
			}
		}

		public void Iteration(TextReader input)
		{
			currentTurn += 2;
			var turnState = TurnState.ReadFrom(input);
			Console.Error.WriteLine("Current turn: " + currentTurn);
			if (currentTurn == Settings.DUMP_TURN || Settings.DUMP_ALL)
			{
				turnState.WriteTo(Console.Error);
				Console.Error.WriteLine("===");
				Dump();
			}

			turnState.stopwatch.Restart();

			NotifyStartTurn(turnState);

			forecaster.BuildForecast(turnState);
			Console.Error.WriteLine($"Forecast made in {turnState.stopwatch.ElapsedMilliseconds} ms");

			admiral.Iteration(turnState);

			NotifyEndTurn(turnState);

			turnState.stopwatch.Stop();
			stats.Add(new TurnStat { time = turnState.stopwatch.ElapsedMilliseconds });
			Console.Error.WriteLine($"Decision made in {turnState.stopwatch.ElapsedMilliseconds} ms");

			if (currentTurn == Settings.DUMP_STAT_TURN)
				DumpStats();
		}

		public void Dump()
		{
			Console.Error.WriteLine($"var gameState = new GameState {{ {nameof(currentTurn)} = {currentTurn} }};");
			foreach (var cannoneer in cannoneers)
				Console.Error.WriteLine($"gameState.{nameof(cannoneers)}[{cannoneer.Key}] = {cannoneer.Value.Dump("gameState")};");
			foreach (var miner in miners)
				Console.Error.WriteLine($"gameState.{nameof(miners)}[{miner.Key}] = {miner.Value.Dump("gameState")};");
			foreach (var navigator in navigators)
				Console.Error.WriteLine($"gameState.{nameof(navigators)}[{navigator.Key}] = {navigator.Value.Dump("gameState")};");
			strateg.Dump($"gameState.{nameof(strateg)}");
		}

		public void DumpStats()
		{
			Console.Error.WriteLine("--- STATISTICS ---");
			Console.Error.WriteLine($"TotalCount: {stats.Count}");
			Console.Error.WriteLine($"Time_Max: {stats.Max(t => t.time)}");
			Console.Error.WriteLine($"Time_Avg: {stats.Average(t => t.time)}");
			Console.Error.WriteLine($"Time_95: {stats.Percentile(t => t.time, 95)}");
			Console.Error.WriteLine($"Time_50: {stats.Percentile(t => t.time, 50)}");
			Console.Error.WriteLine("---");
		}

		private void NotifyStartTurn(TurnState turnState)
		{
			foreach (var teamMember in GetTeam(turnState))
				teamMember.StartTurn(turnState);
		}

		private void NotifyEndTurn(TurnState turnState)
		{
			foreach (var teamMember in GetTeam(turnState))
				teamMember.EndTurn(turnState);
		}
	}
}