using System.Collections.Generic;
using Game.Cannons;
using Game.Entities;

namespace Game.State
{
	public class GameState
	{
		public readonly Dictionary<int, CannonMaster> cannonMasters = new Dictionary<int, CannonMaster>();

		public CannonMaster GetCannonMaster(Ship ship)
		{
			CannonMaster cannonMaster;
			if (!cannonMasters.TryGetValue(ship.id, out cannonMaster))
				cannonMasters.Add(ship.id, cannonMaster = new CannonMaster(ship, this));
			return cannonMaster;
		}

		public void Dump()
		{
			// todo strategies
			// todo cannons
			// todo miners
		}
	}
}