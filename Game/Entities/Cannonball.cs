namespace Game.Entities
{
	public class Cannonball : Entity
	{
		public readonly int firedBy;
		public readonly int turns;

		public Cannonball(int id, int x, int y, int firedBy, int turns) : base(id, EntityType.Cannonball, x, y)
		{
			this.firedBy = firedBy;
			this.turns = turns;
		}
	}
}