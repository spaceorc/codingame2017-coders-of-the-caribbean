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
				Make1vs1StrategicDecisions(turnState);
			else if (turnState.myShips.Count <= 2 && turnState.enemyShips.Count <= 2)
				Make2vs2StrategicDecisions(turnState);
			else
				Make3vs3StrategicDecisions(turnState);

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

		private void Make3vs3StrategicDecisions(TurnState turnState)
		{
			if (turnState.barrels.Any())
			{
				MakeStandardStrategicDecisions(turnState);
				return;
			}

			if (turnState.myShips.Max(s => s.rum) > turnState.enemyShips.Max(s => s.rum) || turnState.myShips.Min(s => s.rum) > 50)
			{
				foreach (var ship in turnState.myShips)
				{
					StrategicDecision prevDecision;
					decisions.TryGetValue(ship.id, out prevDecision);
					decisions[ship.id] = RunAway(turnState, ship, prevDecision);
				}
				return;
			}

			if (turnState.myShips.Count == 1)
			{
				var ship = turnState.myShips[0];
				StrategicDecision prevDecision;
				decisions.TryGetValue(ship.id, out prevDecision);
				decisions[ship.id] = RunAway(turnState, ship, prevDecision);
				return;
			}

			var maxRum = turnState.myShips.Max(s => s.rum);
			var minRum = turnState.myShips.Min(s => s.rum);
			var ship1 = turnState.myShips.First(s => s.rum == minRum);
			var ship2 = turnState.myShips.Last(s => s.rum == maxRum);
			var last = turnState.myShips.FirstOrDefault(s => s != ship1 && s != ship2);
			if (last != null)
			{
				StrategicDecision prevDecision;
				decisions.TryGetValue(last.id, out prevDecision);
				decisions[last.id] = RunAway(turnState, last, prevDecision);
			}

			if (FastCoord.Distance(ship1.fbow, ship2.fbow) <= 4)
			{
				StrategicDecision prevDecision;
				decisions.TryGetValue(ship1.id, out prevDecision);
				if (prevDecision?.role == StrategicRole.Fire || prevDecision?.role == StrategicRole.Explicit)
				{
					decisions[ship1.id] = new StrategicDecision { role = StrategicRole.Explicit, explicitCommand = ShipMoveCommand.Slower };
					decisions[ship2.id] = new StrategicDecision { role = StrategicRole.Approach, targetCoord = prevDecision.fireToCoord };
				}
				else
				{
					var nextShip1Position = FastShipPosition.GetFinalPosition(FastShipPosition.Move(ship1.fposition, ShipMoveCommand.Wait));
					nextShip1Position = FastShipPosition.GetFinalPosition(FastShipPosition.Move(nextShip1Position, ShipMoveCommand.Slower));
					decisions[ship1.id] = new StrategicDecision { role = StrategicRole.Fire, fireToCoord = FastShipPosition.Coord(nextShip1Position) };
					decisions[ship2.id] = new StrategicDecision { role = StrategicRole.Approach, targetCoord = FastShipPosition.Coord(nextShip1Position) };
				}
			}
			else
			{
				var x = (FastCoord.GetX(ship1.fcoord) + FastCoord.GetX(ship2.fcoord)) / 2;
				var y = (FastCoord.GetY(ship1.fcoord) + FastCoord.GetY(ship2.fcoord)) / 2;
				if (x < 5)
					x = 5;
				if (x > Constants.MAP_WIDTH - 6)
					x = Constants.MAP_WIDTH - 6;
				if (y < 5)
					y = 5;
				if (y > Constants.MAP_HEIGHT - 6)
					x = Constants.MAP_HEIGHT - 6;

				decisions[ship1.id] = new StrategicDecision { role = StrategicRole.Approach, targetCoord = FastCoord.Create(x, y) };
				decisions[ship2.id] = new StrategicDecision { role = StrategicRole.Approach, targetCoord = FastCoord.Create(x, y) };
			}
		}

		private void Make2vs2StrategicDecisions(TurnState turnState)
		{
			if (turnState.barrels.Any())
			{
				MakeStandardStrategicDecisions(turnState);
				return;
			}
			if (turnState.myShips.Max(s => s.rum) > turnState.enemyShips.Max(s => s.rum) || turnState.myShips.Min(s => s.rum) > 50)
			{
				foreach (var ship in turnState.myShips)
				{
					StrategicDecision prevDecision;
					decisions.TryGetValue(ship.id, out prevDecision);
					decisions[ship.id] = RunAway(turnState, ship, prevDecision);
				}
				return;
			}

			if (turnState.myShips.Count == 1)
			{
				var ship = turnState.myShips[0];
				StrategicDecision prevDecision;
				decisions.TryGetValue(ship.id, out prevDecision);
				decisions[ship.id] = RunAway(turnState, ship, prevDecision);
				return;
			}

			var ship1 = turnState.myShips[0];
			var ship2 = turnState.myShips[1];
			if (ship1.rum > ship2.rum)
			{
				var tmp = ship1;
				ship1 = ship2;
				ship2 = tmp;
			}
			if (FastCoord.Distance(ship1.fbow, ship2.fbow) <= 4)
			{
				StrategicDecision prevDecision;
				decisions.TryGetValue(ship1.id, out prevDecision);
				if (prevDecision?.role == StrategicRole.Fire || prevDecision?.role == StrategicRole.Explicit)
				{
					decisions[ship1.id] = new StrategicDecision { role = StrategicRole.Explicit, explicitCommand = ShipMoveCommand.Slower };
					decisions[ship2.id] = new StrategicDecision { role = StrategicRole.Approach, targetCoord = prevDecision.fireToCoord };
				}
				else
				{
					var nextShip1Position = FastShipPosition.GetFinalPosition(FastShipPosition.Move(ship1.fposition, ShipMoveCommand.Wait));
					nextShip1Position = FastShipPosition.GetFinalPosition(FastShipPosition.Move(nextShip1Position, ShipMoveCommand.Slower));
					decisions[ship1.id] = new StrategicDecision { role = StrategicRole.Fire, fireToCoord = FastShipPosition.Coord(nextShip1Position) };
					decisions[ship2.id] = new StrategicDecision { role = StrategicRole.Approach, targetCoord = FastShipPosition.Coord(nextShip1Position) };
				}
			}
			else
			{
				var x = (FastCoord.GetX(ship1.fcoord) + FastCoord.GetX(ship2.fcoord)) / 2;
				var y = (FastCoord.GetY(ship1.fcoord) + FastCoord.GetY(ship2.fcoord)) / 2;
				if (x < 5)
					x = 5;
				if (x > Constants.MAP_WIDTH - 6)
					x = Constants.MAP_WIDTH - 6;
				if (y < 5)
					y = 5;
				if (y > Constants.MAP_HEIGHT - 6)
					x = Constants.MAP_HEIGHT - 6;

				decisions[ship1.id] = new StrategicDecision { role = StrategicRole.Approach, targetCoord = FastCoord.Create(x, y) };
				decisions[ship2.id] = new StrategicDecision { role = StrategicRole.Approach, targetCoord = FastCoord.Create(x, y) };
			}
		}

		private void Make1vs1StrategicDecisions(TurnState turnState)
		{
			var ship = turnState.myShips[0];
			var enemyShip = turnState.enemyShips[0];
			StrategicDecision decision;
			decisions.TryGetValue(ship.id, out decision);
			var newDecision = Make1vs1StrategicDecision(turnState, decision, ship, enemyShip);
			decisions[ship.id] = newDecision;
		}

		private StrategicDecision Make1vs1StrategicDecision(TurnState turnState, StrategicDecision prevDecision, Ship ship, Ship enemyShip)
		{
			var myBarrel = FindNearestBarrelToCollect(turnState, ship);
			var enemyBarrel1 = FindNearestBarrelToCollect(turnState, enemyShip);
			var enemyBarrel2 = enemyBarrel1 == null ? null : FindNearestBarrelToCollect(turnState, enemyShip, new HashSet<int> { enemyBarrel1.barrel.id });
			var enemyBarrel3 = enemyBarrel2 == null ? null : FindNearestBarrelToCollect(turnState, enemyShip, new HashSet<int> { enemyBarrel1.barrel.id, enemyBarrel2.barrel.id });

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
				if (myBarrel != null)
					return Collect(myBarrel.barrel).FireTo(GetBarrowIfNotFired(turnState, enemyBarrel1.barrel)?.fcoord);
				return NavigateToShip(turnState, enemyShip, prevDecision).FireTo(GetBarrowIfNotFired(turnState, enemyBarrel1.barrel)?.fcoord);
			}

			if (enemyBarrel3 == null)
			{
				if (myBarrel != null && myBarrel.barrel == enemyBarrel1.barrel && enemyBarrel1.dist - myBarrel.dist > 3)
					return Collect(myBarrel.barrel).FireTo(GetBarrowIfNotFired(turnState, enemyBarrel2.barrel)?.fcoord);

				if (myBarrel != null && myBarrel.barrel == enemyBarrel2.barrel && enemyBarrel2.dist - myBarrel.dist > 3)
					return Collect(myBarrel.barrel).FireTo(GetBarrowIfNotFired(turnState, enemyBarrel1.barrel)?.fcoord);

				if (myBarrel != null && myBarrel.dist < 3)
					return Collect(myBarrel.barrel).FireTo((GetBarrowIfNotFired(turnState, enemyBarrel1.barrel) ?? GetBarrowIfNotFired(turnState, enemyBarrel2.barrel))?.fcoord);

				return NavigateToShip(turnState, enemyShip, prevDecision).FireTo((GetBarrowIfNotFired(turnState, enemyBarrel1.barrel) ?? GetBarrowIfNotFired(turnState, enemyBarrel2.barrel))?.fcoord);
			}

			if (myBarrel != null && myBarrel.barrel == enemyBarrel1.barrel && enemyBarrel1.dist - myBarrel.dist > 3)
				return Collect(myBarrel.barrel).FireTo((GetBarrowIfNotFired(turnState, enemyBarrel2.barrel) ?? GetBarrowIfNotFired(turnState, enemyBarrel3.barrel))?.fcoord);

			if (myBarrel != null && myBarrel.barrel == enemyBarrel2.barrel && enemyBarrel2.dist - myBarrel.dist > 3)
				return Collect(myBarrel.barrel).FireTo((GetBarrowIfNotFired(turnState, enemyBarrel1.barrel) ?? GetBarrowIfNotFired(turnState, enemyBarrel3.barrel))?.fcoord);

			if (myBarrel != null && myBarrel.barrel == enemyBarrel3.barrel && enemyBarrel2.dist - myBarrel.dist > 3)
				return Collect(myBarrel.barrel).FireTo((GetBarrowIfNotFired(turnState, enemyBarrel1.barrel) ?? GetBarrowIfNotFired(turnState, enemyBarrel2.barrel))?.fcoord);

			if (myBarrel != null && myBarrel.dist < 3)
				return Collect(myBarrel.barrel).FireTo((GetBarrowIfNotFired(turnState, enemyBarrel1.barrel) ?? GetBarrowIfNotFired(turnState, enemyBarrel2.barrel) ?? GetBarrowIfNotFired(turnState, enemyBarrel3.barrel))?.fcoord);

			return NavigateToShip(turnState, enemyShip, prevDecision).FireTo((GetBarrowIfNotFired(turnState, enemyBarrel1.barrel) ?? GetBarrowIfNotFired(turnState, enemyBarrel2.barrel) ?? GetBarrowIfNotFired(turnState, enemyBarrel3.barrel))?.fcoord);
		}
		
		private static StrategicDecision Collect(Barrel barrel)
		{
			return new StrategicDecision { role = StrategicRole.Collector, targetCoord = barrel.fcoord, targetBarrelId = barrel.id };
		}

		private Barrel GetBarrowIfNotFired(TurnState turnState, Barrel barrel)
		{
			if (barrel == null)
				return null;
			if (turnState.cannonballs.Any(x => x.fcoord == barrel.fcoord))
				return null;
			return barrel;
		}

		private static StrategicDecision NavigateToShip(TurnState turnState, Ship ship, StrategicDecision prevDecision)
		{
			var nextShipPosition = FastShipPosition.GetFinalPosition(FastShipPosition.Move(ship.fposition, ShipMoveCommand.Faster));
			return new StrategicDecision { role = StrategicRole.Unknown, targetCoord = FastShipPosition.Coord(nextShipPosition) };
		}

		private static StrategicDecision WalkFree(TurnState turnState, Ship ship, StrategicDecision prevDecision)
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

		private StrategicDecision RunAway(TurnState turnState, Ship ship, StrategicDecision prevDecision)
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

		private void MakeStandardStrategicDecisions(TurnState turnState)
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