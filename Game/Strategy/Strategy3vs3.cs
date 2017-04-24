using System;
using System.Collections.Generic;
using System.Linq;
using Game.Entities;
using Game.Geometry;
using Game.State;

namespace Game.Strategy
{
	public class Strategy3vs3
	{
		public readonly Strateg strateg;

		public Strategy3vs3(Strateg strateg)
		{
			this.strateg = strateg;
		}

		public void MakeStrategicDecisions(TurnState turnState)
		{
			if (turnState.barrels.Any())
				CollectBarrels(turnState);
			else
				RunOrSuicide(turnState);
		}

		private void CollectBarrels(TurnState turnState)
		{
			if (turnState.myShips.Count < 3)
			{
				foreach (var myShip in turnState.myShips)
					strateg.decisions[myShip.id] = strateg.strategy2vs2.CollectFreeBarrels(turnState, myShip);
				return;
			}

			var enemyShips = turnState.enemyShips.ToList();
			while (enemyShips.Count < turnState.myShips.Count)
				enemyShips.Add(enemyShips.Last());

			CollectEnemyBarrels(turnState, enemyShips);
		}

		public void CollectEnemyBarrels(TurnState turnState, List<Ship> enemyShips)
		{
			var barrelses = new List<List<CollectableBarrel>>();
			for (var i = 0; i < enemyShips.Count; i++)
			{
				var enemyShip = enemyShips[i];
				var barrels = strateg.strategy1vs1.CollectableBarrels(turnState, enemyShip);
				barrelses.Add(barrels);
			}

			var nextPositionses1 = new List<int>();
			var nextPositionses2 = new List<int>();
			for (int i = 0; i < turnState.myShips.Count; i++)
			{
				var ship = turnState.myShips[i];
				var nextPosition1 = FastShipPosition.GetFinalPosition(FastShipPosition.Move(ship.fposition, ShipMoveCommand.Faster));
				var nextPosition2 = FastShipPosition.GetFinalPosition(FastShipPosition.Move(ship.fposition, ShipMoveCommand.Wait));
				nextPositionses1.Add(nextPosition1);
				nextPositionses2.Add(nextPosition2);
			}

			var targets = new List<CollectableBarrel>();
			for (int i = 0; i < turnState.myShips.Count; i++)
			{
				var barrels = barrelses[i];
				var nextPosition1 = nextPositionses1[i];
				var nextPosition2 = nextPositionses2[i];
				var target = barrels.FirstOrDefault(
					b => FastShipPosition.DistanceTo(nextPosition1, b.barrel.fcoord) < b.dist - 1
						|| FastShipPosition.DistanceTo(nextPosition2, b.barrel.fcoord) < b.dist - 1);
				targets.Add(target);
			}

			while (true)
			{
				int? singleNotNull = null;
				for (int i = 0; i < targets.Count; i++)
				{
					var ship = turnState.myShips[i];
					var target = targets[i];
					if (target == null)
					{
						StrategicDecision decision;
						strateg.decisions.TryGetValue(ship.id, out decision);
						strateg.decisions[ship.id] = strateg.RunAway(turnState, ship, decision);
					}
					else
					{
						if (singleNotNull == null)
							singleNotNull = i;
						else
							singleNotNull = -1;
					}
				}
				if (targets.All(t => t == null))
					return;

				if (singleNotNull.HasValue && singleNotNull.Value != -1)
				{
					var barrels = barrelses[singleNotNull.Value];
					var target = targets[singleNotNull.Value];
					var ship = turnState.myShips[singleNotNull.Value];
					var barrelToFire = barrels.TakeWhile(b => b != target).LastOrDefault();
					strateg.decisions[ship.id] = strateg.Collect(target.barrel).FireTo(barrelToFire?.barrel.fcoord);
					return;
				}

				var usedBarrels = new HashSet<Barrel>();
				bool hasConflicts = false;
				for (int i = 0; i < targets.Count - 1; i++)
				{
					var target = targets[i];
					var nextPosition1 = nextPositionses1[i];
					var nextPosition2 = nextPositionses2[i];
					var barrels = barrelses[i];
					if (target != null)
					{
						for (int k = i + 1; i < targets.Count; i++)
						{
							var otherTarget = targets[k];
							var otherNextPosition1 = nextPositionses1[k];
							var otherNextPosition2 = nextPositionses2[k];
							var otherBarrels = barrelses[k];
							if (otherTarget != null)
							{
								if (target.barrel == otherTarget.barrel)
								{
									hasConflicts = true;
									var dist = Math.Min(FastShipPosition.DistanceTo(nextPosition1, target.barrel.fcoord),
										FastShipPosition.DistanceTo(nextPosition2, target.barrel.fcoord));
									var otherDist = Math.Min(FastShipPosition.DistanceTo(otherNextPosition1, target.barrel.fcoord),
										FastShipPosition.DistanceTo(otherNextPosition2, target.barrel.fcoord));
									if (dist < otherDist)
									{
										otherTarget = otherBarrels.FirstOrDefault(
											b =>
											{
												if (usedBarrels.Contains(b.barrel))
													return false;
												if (b.barrel == target.barrel)
													return false;
												return FastShipPosition.DistanceTo(otherNextPosition1, b.barrel.fcoord) < b.dist - 1
														|| FastShipPosition.DistanceTo(otherNextPosition2, b.barrel.fcoord) < b.dist - 1;
											});
										targets[k] = otherTarget;
									}
									else
									{
										target = barrels.FirstOrDefault(
											b =>
											{
												if (usedBarrels.Contains(b.barrel))
													return false;
												if (b.barrel == otherTarget.barrel)
													return false;
												return FastShipPosition.DistanceTo(nextPosition1, b.barrel.fcoord) < b.dist - 1
														|| FastShipPosition.DistanceTo(nextPosition2, b.barrel.fcoord) < b.dist - 1;
											});
										targets[i] = target;
									}
								}
							}
							if (target == null)
								break;
						}
						if (target != null)
							usedBarrels.Add(target.barrel);
					}
				}

				if (!hasConflicts)
					break;
			}

			var usedBarrelsToFire = new HashSet<Barrel>();
			for (int i = 0; i < targets.Count; i++)
			{
				var target = targets[i];
				if (target != null)
					usedBarrelsToFire.Add(target.barrel);
			}

			for (int i = 0; i < targets.Count; i++)
			{
				var target = targets[i];
				if (target != null)
				{
					var ship = turnState.myShips[i];
					var barrels = barrelses[i];
					var barrelToFire = barrels.TakeWhile(b => !usedBarrelsToFire.Contains(b.barrel)).LastOrDefault();
					strateg.decisions[ship.id] = strateg.Collect(target.barrel).FireTo(barrelToFire?.barrel.fcoord);
					if (barrelToFire != null)
						usedBarrelsToFire.Add(barrelToFire.barrel);
				}
			}
		}
		
		private void RunOrSuicide(TurnState turnState)
		{
			if (turnState.myShips.Max(s => s.rum) > turnState.enemyShips.Max(s => s.rum) || turnState.myShips.Min(s => s.rum) > 50)
			{
				foreach (var ship in turnState.myShips)
				{
					StrategicDecision prevDecision;
					strateg.decisions.TryGetValue(ship.id, out prevDecision);
					strateg.decisions[ship.id] = strateg.RunAway(turnState, ship, prevDecision);
				}
				return;
			}

			if (turnState.myShips.Count == 1)
			{
				var ship = turnState.myShips[0];
				StrategicDecision prevDecision;
				strateg.decisions.TryGetValue(ship.id, out prevDecision);
				strateg.decisions[ship.id] = strateg.RunAway(turnState, ship, prevDecision);
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
				strateg.decisions.TryGetValue(last.id, out prevDecision);
				strateg.decisions[last.id] = strateg.RunAway(turnState, last, prevDecision);
			}

			if (FastCoord.Distance(ship1.fbow, ship2.fbow) <= 4)
			{
				StrategicDecision prevDecision;
				strateg.decisions.TryGetValue(ship1.id, out prevDecision);
				if (prevDecision?.role == StrategicRole.Fire || prevDecision?.role == StrategicRole.Explicit)
				{
					strateg.decisions[ship1.id] = new StrategicDecision { role = StrategicRole.Explicit, explicitCommand = ShipMoveCommand.Slower };
					strateg.decisions[ship2.id] = new StrategicDecision { role = StrategicRole.Approach, targetCoord = prevDecision.fireToCoord };
				}
				else
				{
					var nextShip1Position = FastShipPosition.GetFinalPosition(FastShipPosition.Move(ship1.fposition, ShipMoveCommand.Wait));
					nextShip1Position = FastShipPosition.GetFinalPosition(FastShipPosition.Move(nextShip1Position, ShipMoveCommand.Slower));
					strateg.decisions[ship1.id] = new StrategicDecision { role = StrategicRole.Fire, fireToCoord = FastShipPosition.Coord(nextShip1Position) };
					strateg.decisions[ship2.id] = new StrategicDecision { role = StrategicRole.Approach, targetCoord = FastShipPosition.Coord(nextShip1Position) };
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

				strateg.decisions[ship1.id] = new StrategicDecision { role = StrategicRole.Approach, targetCoord = FastCoord.Create(x, y) };
				strateg.decisions[ship2.id] = new StrategicDecision { role = StrategicRole.Approach, targetCoord = FastCoord.Create(x, y) };
			}
		}
	}
}