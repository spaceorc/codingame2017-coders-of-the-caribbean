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
		public readonly Strategy1vs1 strategy1vs1;
		public readonly Strategy2vs2 strategy2vs2;
		public readonly Strategy3vs3 strategy3vs3;

		private static readonly int[] freeTargets =
		{
			new Coord(5, 5).ToFastCoord(),
			new Coord(5, Constants.MAP_HEIGHT - 5).ToFastCoord(),
			new Coord(Constants.MAP_WIDTH - 5, Constants.MAP_HEIGHT - 5).ToFastCoord(),
			new Coord(Constants.MAP_WIDTH - 5, 5).ToFastCoord()
		};

		private static readonly int[] runTargets =
		{
			new Coord(5, 5).ToFastCoord(),
			new Coord(5, Constants.MAP_HEIGHT - 5).ToFastCoord(),
			new Coord(Constants.MAP_WIDTH / 2, Constants.MAP_HEIGHT / 2).ToFastCoord(),
			new Coord(Constants.MAP_WIDTH - 5, Constants.MAP_HEIGHT - 5).ToFastCoord(),
			new Coord(Constants.MAP_WIDTH - 5, 5).ToFastCoord()
		};
		
		public Strateg(GameState gameState)
		{
			this.gameState = gameState;
			strategy1vs1 = new Strategy1vs1(this);
			strategy2vs2 = new Strategy2vs2(this);
			strategy3vs3 = new Strategy3vs3(this);
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

			var prevDecisions = decisions.ToDictionary(d => d.Key, d => d.Value);

			if (turnState.myShips.Count == 1 && turnState.enemyShips.Count == 1)
				strategy1vs1.MakeStrategicDecisions(turnState);
			else if (turnState.myShips.Count <= 2 && turnState.enemyShips.Count <= 2)
				strategy2vs2.MakeStrategicDecisions(turnState);
			else
				strategy3vs3.MakeStrategicDecisions(turnState);

			return turnState.myShips.Select(
				ship =>
				{
					StrategicDecision prevDecision, nextDecision;
					prevDecisions.TryGetValue(ship.id, out prevDecision);
					decisions.TryGetValue(ship.id, out nextDecision);
					if (nextDecision != prevDecision)
						Console.Error.WriteLine($"New decision for {ship.id}: {nextDecision}");
					return nextDecision;
				}).ToList();
		}

		public StrategicDecision Collect(Barrel barrel)
		{
			return new StrategicDecision { role = StrategicRole.Collector, targetCoord = barrel.fcoord, targetBarrelId = barrel.id };
		}

		public Barrel SelectEnemyBarrelToFire(TurnState turnState, Ship myShip, params CollectableBarrel[] barrels)
		{
			barrels = barrels.Where(b => turnState.cannonballs.All(x => x.fcoord != b.barrel.fcoord)).ToArray();
			foreach (var barrel in barrels)
			{
				var distance = FastCoord.Distance(myShip.fbow, barrel.barrel.fcoord);
				if (distance <= 10)
				{
					var travelTime = (int)(1 + Math.Round(distance / 3.0));
					var enemyTravelTime = barrel.dist / 2 + 1;
					if (travelTime < enemyTravelTime)
						return barrel.barrel;
				}
			}
			return null;
		}

		public StrategicDecision NavigateToShip(TurnState turnState, Ship ship, StrategicDecision prevDecision)
		{
			var nextShipPosition = FastShipPosition.GetFinalPosition(FastShipPosition.Move(ship.fposition, ShipMoveCommand.Faster));
			return new StrategicDecision { role = StrategicRole.Unknown, targetCoord = FastShipPosition.Coord(nextShipPosition) };
		}

		public StrategicDecision WalkFree(TurnState turnState, Ship ship, StrategicDecision prevDecision)
		{
			switch (prevDecision?.role)
			{
				case StrategicRole.Free:
					break;
				default:
					prevDecision = new StrategicDecision { role = StrategicRole.Free, targetCoord = freeTargets[0] };
					break;
			}
			if (FastShipPosition.DistanceTo(ship.fposition, prevDecision.targetCoord.Value) < Settings.FREE_WALK_TARGET_REACH_DIST)
			{
				var freeIndex = (Array.IndexOf(freeTargets, prevDecision.targetCoord.Value) + 1) % freeTargets.Length;
				return new StrategicDecision { role = StrategicRole.Free, targetCoord = freeTargets[freeIndex] };
			}
			return prevDecision;
		}

		public StrategicDecision RunAway(TurnState turnState, Ship ship, StrategicDecision prevDecision)
		{
			var used = new HashSet<int>();
			foreach (var myShip in turnState.myShips)
			{
				StrategicDecision otherDecision;
				if (myShip.id != ship.id && decisions.TryGetValue(myShip.id, out otherDecision) && otherDecision.role == StrategicRole.RunAway)
					used.Add(otherDecision.targetCoord.Value);
			}
			switch (prevDecision?.role)
			{
				case StrategicRole.RunAway:
					break;
				default:
					for (var i = 0; i < freeTargets.Length; i++)
					{
						if (!used.Contains(freeTargets[i]))
						{
							prevDecision = new StrategicDecision { role = StrategicRole.RunAway, targetCoord = runTargets[i] };
							break;
						}
					}
					break;
			}
			if (FastShipPosition.DistanceTo(ship.fposition, prevDecision.targetCoord.Value) < Settings.RUN_AWAY_TARGET_REACH_DIST)
			{
				var freeIndex = Array.IndexOf(runTargets, prevDecision.targetCoord.Value);
				var maxEnemyDist = -1;
				var newIndex = -1;
				for (var i = 0; i < freeTargets.Length; i++)
				{
					if (i != freeIndex && !used.Contains(freeTargets[i]))
					{
						var dist = turnState.enemyShips.Min(s => CalcShipMovingDistTo(s, freeTargets[i]));
						if (dist > maxEnemyDist)
						{
							maxEnemyDist = dist;
							newIndex = i;
						}
					}
				}
				
				return new StrategicDecision { role = StrategicRole.RunAway, targetCoord = runTargets[newIndex] };
			}
			return prevDecision;
		}

		private static int CalcShipMovingDistTo(Ship ship, int ftarget)
		{
			var nextShipPosition = FastShipPosition.GetFinalPosition(FastShipPosition.Move(ship.fposition, ShipMoveCommand.Faster));
			return FastShipPosition.DistanceTo(nextShipPosition, ftarget);
		}

		public void MakeStandardStrategicDecisions(TurnState turnState)
		{
			foreach (var ship in turnState.myShips)
			{
				StrategicDecision decision;
				if (!decisions.TryGetValue(ship.id, out decision))
				{
					var barrel = FindBestBarrelToCollect(turnState, ship);
					if (barrel != null)
						decisions[ship.id] = new StrategicDecision { role = StrategicRole.Collector, targetBarrelId = barrel.barrel.id, targetCoord = barrel.barrel.fcoord };
					else
						decisions[ship.id] = RunAway(turnState, ship, null);
				}
				else
				{
					switch (decision.role)
					{
						case StrategicRole.Collector:
							break;
						default:
							var barrel = FindBestBarrelToCollect(turnState, ship);
							if (barrel != null)
							{
								decisions[ship.id] = new StrategicDecision { role = StrategicRole.Collector, targetBarrelId = barrel.barrel.id, targetCoord = barrel.barrel.fcoord };
								break;
							}
							decisions[ship.id] = RunAway(turnState, ship, decision);
							break;
					}
				}
			}
		}

		public CollectableBarrel FindBestBarrelToCollect(TurnState turnState, Ship ship)
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

		public CollectableBarrel FindNearestBarrelToCollect(TurnState turnState, Ship ship, HashSet<int> used = null)
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

			var nextShipPosition1 = FastShipPosition.GetFinalPosition(FastShipPosition.Move(ship.fposition, ShipMoveCommand.Faster));
			var nextShipPosition2 = FastShipPosition.GetFinalPosition(FastShipPosition.Move(ship.fposition, ShipMoveCommand.Wait));
			var bestDist = int.MaxValue;
			Barrel bestBarrel = null;
			foreach (var barrel in turnState.barrels)
				if (used == null || !used.Contains(barrel.id))
				{
					var dist = FastShipPosition.DistanceTo(nextShipPosition1, barrel.fcoord);
					if (dist < bestDist)
					{
						int hitTurns;
						if (barrelsHitTurns.TryGetValue(barrel.id, out hitTurns) && dist >= hitTurns - 1)
							continue;
						bestBarrel = barrel;
						bestDist = dist;
					}
					dist = FastShipPosition.DistanceTo(nextShipPosition2, barrel.fcoord);
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

		public CollectableBarrel FindNearestBarrelToCollect(TurnState turnState, int fcoord, HashSet<int> used = null)
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

			var bestDist = int.MaxValue;
			Barrel bestBarrel = null;
			foreach (var barrel in turnState.barrels)
				if (used == null || !used.Contains(barrel.id))
				{
					var dist = FastCoord.Distance(fcoord, barrel.fcoord);
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