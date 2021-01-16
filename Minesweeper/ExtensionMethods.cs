using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Minesweeper
{
	public static class ExtensionMethods
	{
		public static void AddRange<T>(this Stack<T> queue, IEnumerable<T> enu)
		{
			foreach (T obj in enu)
				queue.Push(obj);
		}

	}
}
