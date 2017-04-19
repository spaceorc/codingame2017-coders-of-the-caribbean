namespace Game.Entities
{
	public class Barrel : Entity
	{
		public int rum;

		public Barrel(int id, int x, int y, int rum) : base(id, EntityType.Barrel, x, y)
		{
			this.rum = rum;
		}
	}
}