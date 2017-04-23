using Game.Geometry;

namespace Game.Navigation
{
	public class PathItem
	{
		public int sourcePosition;
		public int targetPosition;
		public ShipMoveCommand command;
	}
}