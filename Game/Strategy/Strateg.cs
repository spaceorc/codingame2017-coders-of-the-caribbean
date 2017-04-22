using System;
using System.Collections.Generic;
using System.Linq;
using Game.Entities;
using Game.Geometry;
using Game.State;

namespace Game.Strategy
{
	public enum StrategicRole
	{
		Unknown,
		Collector,
		Free
	}

	public class StrategicDecision
	{
		public StrategicRole role;
		public int? targetCoord;
		public int? targetBarrelId;

		public string Dump()
		{
			return $"new {nameof(StrategicDecision)} {{ {nameof(role)} = {nameof(StrategicRole)}.{role}," +
					$" {nameof(targetBarrelId)} = {(targetBarrelId.HasValue ? targetBarrelId.ToString() : "null")}, " +
					$" {nameof(targetCoord)} = {(targetCoord.HasValue ? targetCoord.ToString() : "null")} }}";
		}
	}

	public class Strateg : IStrateg
	{
		public readonly GameState gameState;
		public readonly Dictionary<int, StrategicDecision> decisions = new Dictionary<int, StrategicDecision>();

		private static readonly int[] freeTargets =
		{
			new Coord(5, 5).ToFastCoord(),
			new Coord(5, Constants.MAP_HEIGHT - 5).ToFastCoord(),
			new Coord(Constants.MAP_WIDTH - 5, Constants.MAP_HEIGHT - 5).ToFastCoord(),
			new Coord(Constants.MAP_WIDTH - 5, 5).ToFastCoord()
		};

		public Strateg(GameState gameState)
		{
			this.gameState = gameState;
		}

		public void StartTurn(TurnState turnState)
		{
		}

		public void EndTurn(TurnState turnState)
		{
		}

		public List<StrategicDecision> MakeDecisions(TurnState turnState)
		{
			CleanupObsoleteDecisions(turnState);

			var result = new List<StrategicDecision>();
			foreach (var ship in turnState.myShips)
			{
				StrategicDecision decision;
				if (!decisions.TryGetValue(ship.id, out decision))
				{
					var barrel = FindBestBarrelToCollect(turnState, ship);
					if (barrel != null)
						decisions[ship.id] = new StrategicDecision { role = StrategicRole.Collector, targetBarrelId = barrel.id, targetCoord = barrel.fcoord };
					else
						decisions[ship.id] = new StrategicDecision { role = StrategicRole.Free, targetCoord = freeTargets[0] };
				}
				else
				{
					switch (decision.role)
					{
						case StrategicRole.Free:
							var barrel = FindBestBarrelToCollect(turnState, ship);
							if (barrel != null)
							{
								decisions[ship.id] = new StrategicDecision { role = StrategicRole.Collector, targetBarrelId = barrel.id, targetCoord = barrel.fcoord };
								break;
							}
							if (FastShipPosition.DistanceTo(ship.fposition, decision.targetCoord.Value) < Settings.FREE_WALK_TARGET_REACH_DIST)
							{
								var freeIndex = (Array.IndexOf(freeTargets, decision.targetCoord.Value ) + 1)% freeTargets.Length;
								decisions[ship.id] = new StrategicDecision { role = StrategicRole.Free, targetCoord = freeTargets[freeIndex] };
							}
							break;
					}
				}
				result.Add(decisions[ship.id]);
			}

			return result;
		}

		private Barrel FindBestBarrelToCollect(TurnState turnState, Ship ship)
		{
			var used = new HashSet<int>();
			foreach (var myShip in turnState.myShips)
			{
				StrategicDecision otherDecision;
				if (myShip.id != ship.id && decisions.TryGetValue(myShip.id, out otherDecision) && otherDecision.role == StrategicRole.Collector)
					used.Add(otherDecision.targetBarrelId.Value);
			}
			var bestDist = int.MaxValue;
			Barrel bestBarrel = null;
			foreach (var barrel in turnState.barrels)
				if (!used.Contains(barrel.id))
				{
					var dist = FastShipPosition.DistanceTo(ship.fposition, barrel.fcoord);
					if (dist < bestDist)
					{
						bestBarrel = barrel;
						bestDist = dist;
					}
				}
			return bestBarrel;
		}

		private void CleanupObsoleteDecisions(TurnState turnState)
		{
			foreach (var kvp in decisions.ToList())
			{
				var shipId = kvp.Key;
				var decision = kvp.Value;
				if (turnState.FindMyShip(shipId) == null)
					decisions.Remove(shipId);
				else if (decision.role == StrategicRole.Collector && !turnState.barrelsById.ContainsKey(decision.targetBarrelId.Value))
					decisions.Remove(shipId);
			}
		}

		public void Dump(string strategRef)
		{
			foreach (var strategy in decisions)
				Console.Error.WriteLine($"{strategRef}.{nameof(decisions)}[{strategy.Key}] = {strategy.Value.Dump()};");
		}
	}
}