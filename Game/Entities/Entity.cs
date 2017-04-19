using Game.Geometry;

namespace Game.Entities
{
	public abstract class Entity
	{
		public readonly Coord _coord;
		public readonly int fcoord;
		public readonly int id;
		public readonly EntityType type;

		protected Entity(int id, EntityType type, int x, int y)
		{
			this.id = id;
			this.type = type;
			_coord = new Coord(x, y);
			fcoord = FastCoord.Create(x, y);
		}

		public override string ToString()
		{
			return $"{type}[{id}] at ({_coord})";
		}
	}
}