using System;

namespace Game.Geometry
{
	[Flags]
	public enum CollisionType
	{
		None = 0,
		MyWall = 1,
		MyMove = 2,
		OtherWall = 4,
		OtherMove = 8,
		MyRotation = 16,
		OtherRotation = 16
	}
}