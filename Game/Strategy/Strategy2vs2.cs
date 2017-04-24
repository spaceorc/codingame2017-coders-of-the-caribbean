using System;
using System.Linq;
using Game.Entities;
using Game.Geometry;
using Game.State;

namespace Game.Strategy
{
	public class Strategy2vs2
	{
		public readonly Strateg strateg;

		public Strategy2vs2(Strateg strateg)
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
			if (turnState.myShips.Count == 1)
			{
				strateg.decisions[turnState.myShips[0].id] = CollectFreeBarrels(turnState, turnState.myShips[0]);
				return;
			}

			if (turnState.enemyShips.Count == 1)
			{
				CollectEnemyBarrels(turnState, turnState.enemyShips[0], turnState.enemyShips[0]);
				return;
			}

			var ship1 = turnState.myShips[0];
			var ship2 = turnState.myShips[1];
			var enemyShip1 = turnState.enemyShips[0];
			var enemyShip2 = turnState.enemyShips[1];

			if (FastShipPosition.DistanceTo(ship1.fposition, enemyShip1.fcoord) > FastShipPosition.DistanceTo(ship2.fposition, enemyShip1.fcoord))
			{
				enemyShip1 = turnState.enemyShips[1];
				enemyShip2 = turnState.enemyShips[0];
			}

			CollectEnemyBarrels(turnState, enemyShip1, enemyShip2);
		}
		
		public void CollectEnemyBarrels(TurnState turnState, Ship enemyShip1, Ship enemyShip2)
		{
			var barrels1 = strateg.strategy1vs1.CollectableBarrels(turnState, enemyShip1);
			var barrels2 = strateg.strategy1vs1.CollectableBarrels(turnState, enemyShip2);

			var ship1 = turnState.myShips[0];
			var ship2 = turnState.myShips[1];

			var nextShip1Position1 = FastShipPosition.GetFinalPosition(FastShipPosition.Move(ship1.fposition, ShipMoveCommand.Faster));
			var nextShip1Position2 = FastShipPosition.GetFinalPosition(FastShipPosition.Move(ship1.fposition, ShipMoveCommand.Wait));

			var nextShip2Position1 = FastShipPosition.GetFinalPosition(FastShipPosition.Move(ship2.fposition, ShipMoveCommand.Faster));
			var nextShip2Position2 = FastShipPosition.GetFinalPosition(FastShipPosition.Move(ship2.fposition, ShipMoveCommand.Wait));

			var target1 = barrels1.FirstOrDefault(
				b => FastShipPosition.DistanceTo(nextShip1Position1, b.barrel.fcoord) < b.dist - 1
					|| FastShipPosition.DistanceTo(nextShip1Position2, b.barrel.fcoord) < b.dist - 1);

			var target2 = barrels2.FirstOrDefault(
				b => FastShipPosition.DistanceTo(nextShip2Position1, b.barrel.fcoord) < b.dist - 1
					|| FastShipPosition.DistanceTo(nextShip2Position2, b.barrel.fcoord) < b.dist - 1);

			while (true)
			{
				if (target1 == null)
				{
					StrategicDecision decision;
					strateg.decisions.TryGetValue(ship1.id, out decision);
					strateg.decisions[ship1.id] = strateg.RunAway(turnState, ship1, decision);
				}

				if (target2 == null)
				{
					StrategicDecision decision;
					strateg.decisions.TryGetValue(ship2.id, out decision);
					strateg.decisions[ship2.id] = strateg.RunAway(turnState, ship2, decision);
				}

				if (target1 == null && target2 == null)
					return;

				if (target1 != null && target2 == null)
				{
					var barrelToFire = barrels1.TakeWhile(b => b != target1).LastOrDefault();
					strateg.decisions[ship1.id] = strateg.Collect(target1.barrel).FireTo(barrelToFire?.barrel.fcoord);
					return;
				}

				if (target2 != null && target1 == null)
				{
					var barrelToFire = barrels2.TakeWhile(b => b != target2).LastOrDefault();
					strateg.decisions[ship2.id] = strateg.Collect(target2.barrel).FireTo(barrelToFire?.barrel.fcoord);
					return;
				}

				if (target1.barrel != target2.barrel)
				{
					var barrelToFire1 = barrels1.TakeWhile(b => b.barrel != target1.barrel && b.barrel != target2.barrel).LastOrDefault();
					strateg.decisions[ship1.id] = strateg.Collect(target1.barrel).FireTo(barrelToFire1?.barrel.fcoord);

					var barrelToFire2 = barrels2.TakeWhile(b => b.barrel != target1.barrel && b.barrel != target2.barrel && b.barrel != barrelToFire1?.barrel).LastOrDefault();
					strateg.decisions[ship2.id] = strateg.Collect(target2.barrel).FireTo(barrelToFire2?.barrel.fcoord);
					return;
				}

				var dist1 = Math.Min(FastShipPosition.DistanceTo(nextShip1Position1, target1.barrel.fcoord),
					FastShipPosition.DistanceTo(nextShip1Position2, target1.barrel.fcoord));
				var dist2 = Math.Min(FastShipPosition.DistanceTo(nextShip2Position1, target1.barrel.fcoord),
					FastShipPosition.DistanceTo(nextShip2Position2, target1.barrel.fcoord));
				if (dist1 < dist2)
					target2 = barrels2.FirstOrDefault(
						b => b.barrel != target1.barrel && (FastShipPosition.DistanceTo(nextShip2Position1, b.barrel.fcoord) < b.dist - 1
											|| FastShipPosition.DistanceTo(nextShip2Position2, b.barrel.fcoord) < b.dist - 1));
				else
					target1 = barrels1.FirstOrDefault(
						b => b.barrel != target2.barrel && (FastShipPosition.DistanceTo(nextShip1Position1, b.barrel.fcoord) < b.dist - 1
											|| FastShipPosition.DistanceTo(nextShip1Position2, b.barrel.fcoord) < b.dist - 1));
			}
		}

		public StrategicDecision CollectFreeBarrels(TurnState turnState, Ship ship)
		{
			StrategicDecision prevDecision;
			strateg.decisions.TryGetValue(ship.id, out prevDecision);
			var barrel = strateg.FindBestBarrelToCollect(turnState, ship);
			if (barrel != null)
				return strateg.Collect(barrel.barrel);
			return strateg.WalkFree(turnState, ship, prevDecision);
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