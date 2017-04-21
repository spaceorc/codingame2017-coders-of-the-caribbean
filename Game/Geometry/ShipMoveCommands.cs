using System;
using System.Linq;

namespace Game.Geometry
{
	public static class ShipMoveCommands
	{
		public static readonly ShipMoveCommand[] all = Enum.GetValues(typeof(ShipMoveCommand)).Cast<ShipMoveCommand>().ToArray();
	}
}