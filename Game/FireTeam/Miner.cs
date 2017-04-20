using Game.State;

namespace Game.FireTeam
{
	public class Miner : ITeamMember
	{
		public readonly GameState gameState;
		public readonly int shipId;
		public int cooldown;
		public bool canMine;
		public bool doMine;

		public Miner(int shipId, GameState gameState)
		{
			this.gameState = gameState;
			this.shipId = shipId;
		}

		public void StartTurn(TurnState turnState)
		{
			if (!Settings.USE_MINING)
				return;
			canMine = cooldown == 0;
			if (cooldown > 0)
				cooldown--;
			doMine = false;
		}

		public void EndTurn(TurnState turnState)
		{
		}

		public void PrepareToMine(TurnState turnState)
		{
			if (!canMine)
				return;
			// todo dont mine own ships!
			doMine = true;
		}

		public bool Mine(TurnState turnState)
		{
			if (!doMine)
				return false;
			var ship = turnState.myShipsById[shipId];
			ship.Mine();
			cooldown = Constants.MINING_COOLDOWN;
			return true;
		}
		
		public string Dump(string gameStateRef)
		{
			return $"new {nameof(Miner)}({shipId}, {gameStateRef}) {{ {nameof(cooldown)} = {cooldown} }}";
		}
	}
}