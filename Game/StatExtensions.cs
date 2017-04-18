using System;
using System.Collections.Generic;
using System.Linq;

namespace Game
{
	internal static class StatExtensions
	{
		public static long Percentile<T>(this IEnumerable<T> enumerable, Func<T, long> selector, int percentile)
		{
			var items = enumerable.Select(selector).ToList();
			items.Sort();
			if (items.Count == 0)
				return -1;
			var index = percentile * items.Count / 100 - 1;
			if (index < 0)
				index = 0;
			if (index >= items.Count)
				index = items.Count - 1;
			return items[index];
		}
	}
}