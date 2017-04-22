using System;
using System.Collections.Generic;
using System.Linq;
using Game.Entities;
using Game.Geometry;
using Game.State;

namespace Game.Strategy
{
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

			if (turnState.myShips.Count == 1 && turnState.enemyShips.Count == 1)
				return MakeOneVsOneStrategicDecisions(turnState);

			return MakeStandardStrategicDecisions(turnState);
		}

		private List<StrategicDecision> MakeOneVsOneStrategicDecisions(TurnState turnState)
		{
			var ship = turnState.myShips[0];
			var enemyShip = turnState.enemyShips[0];
			StrategicDecision decision;
			decisions.TryGetValue(ship.id, out decision);
			var newDecision = MakeOneVsOneStrategicDecision(turnState, decision, ship, enemyShip);
			if (!newDecision.Equals(decision))
				Console.Error.WriteLine($"New decision for {ship.id}: {newDecision}");
			decisions[ship.id] = newDecision;
			return new List<StrategicDecision> { newDecision };
		}

		private StrategicDecision MakeOneVsOneStrategicDecision(TurnState turnState, StrategicDecision prevDecision, Ship ship, Ship enemyShip)
		{
			var myBarrel = FindNearestBarrelToCollect(turnState, ship);
			var enemyBarrel1 = FindNearestBarrelToCollect(turnState, enemyShip);
			var enemyBarrel2 = enemyBarrel1 == null ? null : FindNearestBarrelToCollect(turnState, enemyShip, new HashSet<int> { enemyBarrel1.barrel.id });

			if (enemyBarrel1 == null)
			{
				if (myBarrel != null)
					return Collect(myBarrel.barrel);
				return WalkFree(turnState, ship, prevDecision);
			}

			if (enemyBarrel2 == null)
			{
				if (myBarrel != null && myBarrel.barrel == enemyBarrel1.barrel && enemyBarrel1.dist - myBarrel.dist > 3)
					return Collect(myBarrel.barrel);
				return FireBarrowIfNotYet(WalkFree(turnState, ship, prevDecision), turnState, enemyBarrel1.barrel);
			}

			if (myBarrel != null && myBarrel.barrel == enemyBarrel1.barrel && enemyBarrel1.dist - myBarrel.dist > 3 && myBarrel.dist <= 3)
				return FireBarrowIfNotYet(Collect(enemyBarrel1.barrel), turnState, enemyBarrel2.barrel);

			if (myBarrel != null && myBarrel.barrel != enemyBarrel1.barrel && myBarrel.dist <= 3)
				return FireBarrowIfNotYet(Collect(myBarrel.barrel), turnState, enemyBarrel1.barrel);

			return FireBarrowIfNotYet(Collect(enemyBarrel2.barrel), turnState, enemyBarrel1.barrel);
		}
		
		private static StrategicDecision Collect(Barrel barrel)
		{
			return new StrategicDecision { role = StrategicRole.Collector, targetCoord = barrel.fcoord, targetBarrelId = barrel.id };
		}

		private StrategicDecision FireBarrowIfNotYet(StrategicDecision prevDecision, TurnState turnState, Barrel barrel)
		{
			if (turnState.cannonballs.Any(x => x.fcoord == barrel.fcoord))
				return prevDecision;
			return prevDecision.FireTo(barrel.fcoord);
		}

		private static StrategicDecision WalkFree(TurnState turnState, Ship ship, StrategicDecision prevDecision)
		{
			switch (prevDecision?.role)
			{
				case null:
				case StrategicRole.Unknown:
				case StrategicRole.Collector:
					prevDecision = new StrategicDecision { role = StrategicRole.Free, targetCoord = freeTargets[0] };
					break;
				case StrategicRole.Free:
					break;
			}
			if (FastShipPosition.DistanceTo(ship.fposition, prevDecision.targetCoord.Value) < Settings.FREE_WALK_TARGET_REACH_DIST)
			{
				var freeIndex = (Array.IndexOf(freeTargets, prevDecision.targetCoord.Value) + 1) % freeTargets.Length;
				return new StrategicDecision { role = StrategicRole.Free, targetCoord = freeTargets[freeIndex] };
			}
			return prevDecision;
		}

		private List<StrategicDecision> MakeStandardStrategicDecisions(TurnState turnState)
		{
			var result = new List<StrategicDecision>();
			foreach (var ship in turnState.myShips)
			{
				StrategicDecision decision;
				if (!decisions.TryGetValue(ship.id, out decision))
				{
					var barrel = FindBestBarrelToCollect(turnState, ship);
					if (barrel != null)
						decisions[ship.id] = new StrategicDecision { role = StrategicRole.Collector, targetBarrelId = barrel.barrel.id, targetCoord = barrel.barrel.fcoord };
					else
						decisions[ship.id] = WalkFree(turnState, ship, null);
				}
				else
				{
					switch (decision.role)
					{
						case StrategicRole.Unknown:
						case StrategicRole.Free:
							var barrel = FindBestBarrelToCollect(turnState, ship);
							if (barrel != null)
							{
								decisions[ship.id] = new StrategicDecision { role = StrategicRole.Collector, targetBarrelId = barrel.barrel.id, targetCoord = barrel.barrel.fcoord };
								break;
							}
							decisions[ship.id] = WalkFree(turnState, ship, decision);
							break;
					}
				}
				result.Add(decisions[ship.id]);
			}

			return result;
		}

		private CollectableBarrel FindBestBarrelToCollect(TurnState turnState, Ship ship)
		{
			var used = new HashSet<int>();
			foreach (var myShip in turnState.myShips)
			{
				StrategicDecision otherDecision;
				if (myShip.id != ship.id && decisions.TryGetValue(myShip.id, out otherDecision) && otherDecision.role == StrategicRole.Collector)
					used.Add(otherDecision.targetBarrelId.Value);
			}
			return FindNearestBarrelToCollect(turnState, ship, used);
		}

		private CollectableBarrel FindNearestBarrelToCollect(TurnState turnState, Ship ship, HashSet<int> used = null)
		{
			var barrelsHitTurns = new Dictionary<int, int>();
			foreach (var barrel in turnState.barrels)
			{
				foreach (var cannonball in turnState.cannonballs)
				{
					if (cannonball.fcoord == barrel.fcoord)
					{
						int prevTurns;
						if (!barrelsHitTurns.TryGetValue(barrel.id, out prevTurns) || prevTurns > cannonball.turns)
							barrelsHitTurns[barrel.id] = cannonball.turns;
					}
				}
			}

			var nextShipPosition = FastShipPosition.GetFinalPosition(FastShipPosition.Move(ship.fposition, ShipMoveCommand.Faster));
			var bestDist = int.MaxValue;
			Barrel bestBarrel = null;
			foreach (var barrel in turnState.barrels)
				if (used == null || !used.Contains(barrel.id))
				{
					var dist = FastShipPosition.DistanceTo(nextShipPosition, barrel.fcoord);
					if (dist < bestDist)
					{
						int hitTurns;
						if (barrelsHitTurns.TryGetValue(barrel.id, out hitTurns) && dist >= hitTurns - 1)
							continue;
						bestBarrel = barrel;
						bestDist = dist;
					}
				}
			return bestBarrel == null ? null : new CollectableBarrel { barrel = bestBarrel, dist = bestDist };
		}

		private class CollectableBarrel
		{
			public Barrel barrel;
			public int dist;

			public override string ToString()
			{
				return $"{barrel}, {nameof(dist)}: {dist}";
			}
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
				Console.Error.WriteLine($"(({nameof(Strateg)}){strategRef}).{nameof(decisions)}[{strategy.Key}] = {strategy.Value.Dump()};");
		}
	}
}