namespace Game
{
	internal class TurnStat
	{
		public long time;
		public bool isDouble;

		public long CorrectedTime()
		{
			return isDouble ? time / 2 : time;
		}
	}
}