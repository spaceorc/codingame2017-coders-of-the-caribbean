namespace Game
{
	// pack: 0
	public static class Settings
	{
		public const bool USE_MINING = false;

		public const bool USE_DOUBLE_PATHFINDING = true;
		public const int DOUBLE_PATHFINDING_TIMELIMIT = 18;
		public const int NAVIGATION_PATH_DEPTH = 5;

		public const int NEAR_ENEMY_SHIP_MIN_DIST = 4;
		public const int NEAR_ENEMYSHIP_VIRTUAL_DAMAGE = 0; // 0 to disable

		public const int FREE_WALK_TARGET_REACH_DIST = 5;
		
		public const int CANNONBALLS_FORECAST_TRAVEL_TIME_LIMIT = 1; // 0 to disable

		public const int CANNONS_TRAVEL_TIME_LIMIT = 2;

		public const int DUMP_TURN = -1;
		public const int DUMP_STAT_TURN = -1;
		public const bool DUMP_BEST_PATH = false;
	}
}