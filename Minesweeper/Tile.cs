using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Minesweeper
{
	public class Tile
	{
		public int X { get; set; }
		public int Y { get; set; }
		public bool Flagged
		{
			get
			{
				return this._flagged || this._tentative;
			}
			set
			{
				this._flagged = value;
			}
		}

		public bool Tentative
		{
			get
			{
				return _tentative;
			}
			set
			{
				this._tentative = value;
			}
		}
		public bool Revealed { get; set; }
		public bool Mine { get; set; }
		public bool Hit { get; set; }
		public bool Selected { get; set; }
		public int AdjacentMines { get; set; }

		private bool _tentative;
		private bool _flagged;

		public Tile[] GetAdjacentTiles()
		{
			Tile[] tiles = new Tile[8];
			int index = 0;
			for (int col = X - 1; col <= X + 1; col++)
			{
				if (col < 0 || col >= Game.GameWidth) continue;
				for (int row = Y - 1; row <= Y + 1; row++)
				{
					if (col == X && row == Y || row < 0 || row >= Game.GameHeight) continue;
					tiles[index++] = Game.Board[col, row];
				}
			}
			return tiles;
		}

		public Tile(int x, int y)
		{
			this.X = x;
			this.Y = y;
		}
	}
}
