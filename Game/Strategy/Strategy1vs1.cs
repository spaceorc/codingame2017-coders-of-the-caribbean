using System;
using System.Collections.Generic;
using System.Linq;
using Game.Entities;
using Game.Geometry;
using Game.State;

namespace Game.Strategy
{
	public class Strategy1vs1
	{
		public readonly Strateg strateg;

		public Strategy1vs1(Strateg strateg)
		{
			this.strateg = strateg;
		}

		public void MakeStrategicDecisions(TurnState turnState)
		{
			var ship = turnState.myShips[0];
			var enemyShip = turnState.enemyShips[0];
			StrategicDecision decision;
			strateg.decisions.TryGetValue(ship.id, out decision);
			var newDecision = MakeStrategicDecision(turnState, decision, ship, enemyShip);
			strateg.decisions[ship.id] = newDecision;
		}

		public StrategicDecision MakeStrategicDecision(TurnState turnState, StrategicDecision prevDecision, Ship ship, Ship enemyShip)
		{
			var barrels = CollectableBarrels(turnState, enemyShip);

			var nextShipPosition1 = FastShipPosition.GetFinalPosition(FastShipPosition.Move(ship.fposition, ShipMoveCommand.Faster));
			var nextShipPosition2 = FastShipPosition.GetFinalPosition(FastShipPosition.Move(ship.fposition, ShipMoveCommand.Wait));

			var target = barrels.FirstOrDefault(
				b => FastShipPosition.DistanceTo(nextShipPosition1, b.barrel.fcoord) < b.dist - 1
					|| FastShipPosition.DistanceTo(nextShipPosition2, b.barrel.fcoord) < b.dist - 1);

			if (target == null)
				return strateg.RunAway(turnState, ship, prevDecision);

			var barrelToFire = barrels.TakeWhile(b => b != target).LastOrDefault();

			return strateg.Collect(target.barrel).FireTo(barrelToFire?.barrel.fcoord);
		}

		public List<CollectableBarrel> CollectableBarrels(TurnState turnState, Ship enemyShip)
		{
			var enemyStartBarrel = FindNearestBarrelToCollect(turnState, enemyShip);
			var currentBarrel = enemyStartBarrel;
			var barrels = new List<CollectableBarrel>();
			while (currentBarrel != null)
			{
				barrels.Add(currentBarrel);
				var nextBarrel = FindNearestBarrelToCollect(turnState, currentBarrel.barrel.fcoord, new HashSet<int>(barrels.Select(b => b.barrel.id)));
				if (nextBarrel != null)
					nextBarrel.dist += currentBarrel.dist;
				currentBarrel = nextBarrel;
			}
			return barrels;
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
						var enemyTravelTime = dist / 2 + 1;
						int hitTurns;
						if (barrelsHitTurns.TryGetValue(barrel.id, out hitTurns) && hitTurns <= enemyTravelTime)
							continue;
						bestBarrel = barrel;
						bestDist = dist;
					}
					dist = FastShipPosition.DistanceTo(nextShipPosition2, barrel.fcoord);
					if (dist < bestDist)
					{
						var enemyTravelTime = dist / 2 + 1;
						int hitTurns;
						if (barrelsHitTurns.TryGetValue(barrel.id, out hitTurns) && hitTurns <= enemyTravelTime)
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
						if (barrelsHitTurns.TryGetValue(barrel.id, out hitTurns))
							continue;
						bestBarrel = barrel;
						bestDist = dist;
					}
				}
			return bestBarrel == null ? null : new CollectableBarrel { barrel = bestBarrel, dist = bestDist };
		}
	}
}