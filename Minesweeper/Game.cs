using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace Minesweeper
{
	public partial class Game : Form
	{
		public const int GameWidth = 20;
		public const int GameHeight = 20;
		private const int Mines = 30;
		private const bool NumberClickReveal = true;
		private const int PlayAreaPadding = 10;
		private const int PlayAreaPaddingTop = 52;
		private const int TileSize = 16;
		private const int FaceButtonSize = 26;

		public static Tile[,] Board { get; private set; }

		private Pen gridPen;
		private Random random;
		private Thread gameTick;
		private DateTime gameStartTime;
		private int mineCount;
		private int duration;
		private int flaggedMines;
		private Face faceStatus;
		private int faceX;
		private int faceY;
		private Tile selectedTile;
		private bool timerShutdown;
		private bool selectingFace;
		private int tilesDrawn;
		private bool lose;
		private bool win;

		public Game()
		{
			InitializeComponent();

			gridPen = new Pen(Color.FromArgb(255, 123, 123, 123));

			AssetController.Initialize();
			InitializeGame();
		}

		private void Tick()
		{
			TimeSpan diff;
			while (!timerShutdown)
			{
				if (IsGameOver())
				{
					timerShutdown = true;
					return;
				}
				diff = DateTime.Now - gameStartTime;
				duration = diff.Seconds;
				this.Invalidate();
			}
			Thread.Sleep(1000);
		}

		private void InitializeGame()
		{
			if (Mines > GameWidth * GameHeight)
			{
				throw new Exception("Uh.. bomb amount larger than the game board..");
			}

			Board = new Tile[GameWidth, GameHeight];
			random = new Random();
			mineCount = Mines;
			timerShutdown = true;
			duration = flaggedMines = 0;
			faceStatus = Face.Idle;
			selectedTile = null;
			selectingFace = lose = win = false;


			//initialize board

			for (int col = 0; col < GameWidth; col++)
			{
				for (int row = 0; row < GameHeight; row++)
				{
					Board[col, row] = new Tile(col, row);
				}
			}

			//set bomb locations.

			int x, y;
			Tile result;
			for (int i = 0; i < Mines;)
			{
				x = random.Next(0, GameWidth);
				y = random.Next(0, GameHeight);
				result = Board[x, y];
				if (!result.Mine)
				{
					result.Mine = true;
					i++;
				}
			}

			//calculate adjacent bombs in each cell.

			int hits;
			Tile adjacent;

			for (x = 0; x < GameWidth; x++)
			{
				for (y = 0; y < GameHeight; y++)
				{
					result = Board[x, y];
					if (!result.Mine)
					{
						hits = 0;
						for (int col = x - 1; col <= x + 1; col++)
						{
							if (col < 0 || col >= GameWidth) continue;
							for (int row = y - 1; row <= y + 1; row++)
							{
								if (col == x && row == y || row < 0 || row >= GameHeight) continue;
								adjacent = Board[col, row];
								if (adjacent.Mine) hits++;
							}
						}
						result.AdjacentMines = hits;
					}
				}
			}
		}

		private void UpdateCounters(PaintEventArgs e, int number, int x, int y)
		{
			bool negative = false;
			string text;
			if (number < 0)
			{
				number *= -1;
				negative = true;
				text = number.ToString();
				if (text.Length >= 3) text = (number % 100).ToString();
			}
			else
			{
				text = number.ToString();
				if (number > 999) text = "999";
			}

			if (text.Length == 1) text = "00" + text;
			else if (text.Length == 2) text = "0" + text;

			int width, height;

			for (int i = 0; i < 3; i++)
			{
				AssetController.DrawAsset(e, negative && i == 0 ? "counter-dash" : "counter-" + text[i].ToString(), x, y, out width, out height);
				x += width + 2;
			}
		}


		private bool SelectedButton(MouseEventArgs e, out int x, out int y)
		{
			x = e.X - PlayAreaPadding;
			y = e.Y - PlayAreaPaddingTop;
			if (x > 0 && x < GameWidth * TileSize && y > 0 && y < GameHeight * TileSize)
			{
				x = (x - (x % TileSize)) / TileSize;
				y = (y - (y % TileSize)) / TileSize;
				return true;
			}
			return false;
		}

		private bool FaceSelected(MouseEventArgs e)
		{
			return (e.X >= faceX && e.X <= faceX + (FaceButtonSize - 2) && e.Y >= faceY && e.Y <= faceY + (FaceButtonSize - 2));
		}

		/**
		 * Clear all adjacent blank tiles. Recursive has limitations and cannot reveal mines for a huge board, queue system works. 
		 */
		private void RevealBlankTiles(Tile tile)
		{
			Stack<Tile> revealQueue = new Stack<Tile>();
			revealQueue.AddRange(tile.GetAdjacentTiles());

			Tile[] adjacentTiles;

			while (revealQueue.Count > 0)
			{
				tile = revealQueue.Pop();
				if (tile != null)
				{
					if (!tile.Mine && !tile.Revealed && !tile.Flagged)
					{
						if (tile.AdjacentMines == 0)
						{
							adjacentTiles = tile.GetAdjacentTiles();
							if (adjacentTiles != null)
							{
								revealQueue.AddRange(adjacentTiles);
							}
						}
						tile.Revealed = true;
					}
				}

			}
		}


		private bool IsGameOver()
		{
			return (win || lose);
		}

		private void PlayerDead()
		{
			//reveal all mines
			Tile result;
			for (int col = 0; col < GameWidth; col++)
			{
				for (int row = 0; row < GameHeight; row++)
				{
					result = Board[col, row];
					if (!result.Revealed && result.Mine)
					{
						if (result.Tentative) result.Flagged = result.Tentative = false;
						if (!result.Flagged) result.Revealed = true;
					}
				}
			}

			faceStatus = Face.Dead;
			lose = true;
		}

		public void StartGameTimer()
		{
			timerShutdown = false;
			gameStartTime = DateTime.Now;
			gameTick = new Thread(new ThreadStart(Tick));
			gameTick.Start();
		}

		private void Game_Paint(object sender, PaintEventArgs e)
		{
			/**
			 * piece together interface
             */

			int drawX = 0, drawY = 0;
			int assetWidth, assetHeight;
			int boardWidth, boardHeight;
			Image component;
			Tile result;

			/**
			 * Draw top left portion of the interface
			 */

			AssetController.DrawAsset(e, "top_left", drawX, drawY, out assetWidth, out assetHeight);
			drawX += assetWidth;

			/**
			 * Draw middle portion of the interface
			 */

			for (int i = 0; i < GameWidth; i++)
			{
				AssetController.DrawAsset(e, "top_middle", drawX, drawY, out assetWidth, out assetHeight);
				if (assetWidth > -1 && assetHeight > -1)
				{
					drawX += assetWidth;
				}
				else
				{
					throw new Exception("image not loaded.");
				}
			}

			/**
			 * Draw top right corner of the interface.
			 */

			AssetController.DrawAsset(e, "top_right", drawX, drawY, out assetWidth, out assetHeight);
			drawY += assetHeight;

			//the width boundary of the game
			boardWidth = drawX + assetWidth;

			/**
			 * Draw right edge of the interface.
			 */
			for (int i = 0; i < GameHeight; i++)
			{
				AssetController.DrawAsset(e, "right_side", drawX, drawY, out assetWidth, out assetHeight);
				if (assetWidth > -1 && assetHeight > -1)
				{
					drawY += assetHeight;
				}
				else
				{
					throw new Exception("image not loaded.");
				}
			}


			/**
			 * Draw bottom right corner of the interface.
			 */
			AssetController.DrawAsset(e, "bottom_right_corner", drawX, drawY, out assetWidth, out assetHeight);

			//the height boundary of the game
			boardHeight = drawY + assetHeight;

			drawX -= assetWidth + 6;

			/**
			 * Draw bottom edge of the interface
			 */
			for (int i = 0; i < GameWidth; i++)
			{
				AssetController.DrawAsset(e, "bottom", drawX, drawY, out assetWidth, out assetHeight);
				if (assetWidth > -1 && assetHeight > -1)
				{
					drawX -= assetWidth;
				}
				else
				{
					throw new Exception("image not loaded.");
				}
			}


			/**
			 * Draw bottom left corner of the interface
			 */
			component = AssetController.GetAsset("bottom_left_corner");

			drawX += component.Width - 5;
			e.Graphics.DrawImage(component, drawX, drawY);
			drawY -= component.Height + 6;

			/**
			 * Draw left edge of the interface.
			 */
			for (int i = 0; i < GameHeight; i++)
			{
				AssetController.DrawAsset(e, "left_side", drawX, drawY, out assetWidth, out assetHeight);
				if (assetWidth > -1 && assetHeight > -1)
				{
					drawY -= assetHeight;
				}
				else
				{
					throw new Exception("image not loaded.");
				}
			}

			/**
			 * Draw bomb counter.
			 */
			e.Graphics.FillRectangle(Brushes.Black, new Rectangle(drawX = 14, drawY = 14, 39, 23));
			UpdateCounters(e, mineCount - flaggedMines, drawX + 1, drawY + 1);

			/**
			 * Draw face button
			 */

			string componentName = "face_";
			switch (faceStatus)
			{
				case Face.Idle:
					componentName += "idle";
					break;
				case Face.Ooh:
					componentName += "ooh";
					break;
				case Face.Down:
					componentName += "down";
					break;
				case Face.Dead:
					componentName += "dead";
					break;
				case Face.Win:
					componentName += "win";
					break;
			}
			component = AssetController.GetAsset(componentName);
			e.Graphics.DrawImage(component, (faceX = ((boardWidth / 2) - (component.Size.Width / 2))) - 1, (faceY = (drawY - 1)));

			/**
			 * Draw duration counter.
			 */
			e.Graphics.FillRectangle(Brushes.Black, new Rectangle(drawX = boardWidth - 16 - 39, drawY, 39, 23));
			UpdateCounters(e, duration, drawX + 1, drawY + 1);

			/**
			 * Draw gridlines
			 */

			for (int y = 0; y < GameHeight; ++y)
			{
				e.Graphics.DrawLine(gridPen, PlayAreaPadding, PlayAreaPaddingTop + (y * TileSize), boardWidth - 11, PlayAreaPaddingTop + (y * TileSize));
			}

			for (int x = 0; x < GameWidth; ++x)
			{
				e.Graphics.DrawLine(gridPen, PlayAreaPadding + (x * TileSize), PlayAreaPaddingTop, PlayAreaPadding + (x * TileSize), boardHeight - 11);
			}

			/**
			 * Draw buttons
			 */

			tilesDrawn = 0;
			drawX = PlayAreaPadding;
			for (int col = 0; col < GameWidth; col++)
			{
				drawY = PlayAreaPaddingTop;
				for (int row = 0; row < GameHeight; row++)
				{
					result = Board[col, row];
					/*	if (result.Mine)
						{
							AssetController.DrawAsset(e, "question", drawX, drawY);
							tilesDrawn++;
							drawY += TileSize;
							continue;
						}*/
					if (result.Revealed)
					{
						if (result.Mine)
						{
							AssetController.DrawAsset(e, result.Hit ? "mine-hit" : "mine", drawX + 1, drawY + 1);
						}
						else
						{
							AssetController.DrawAsset(e, result.AdjacentMines.ToString(), drawX + 1, drawY + 1);
						}
					}
					else if (result.Selected)
					{
						AssetController.DrawAsset(e, result.Tentative ? "question-down" : "0", drawX + 1, drawY + 1);
					}
					else if (IsGameOver() && result.Flagged && !result.Tentative && !result.Mine)
					{
						AssetController.DrawAsset(e, "no-mine", drawX + 1, drawY + 1);
					}
					else
					{
						AssetController.DrawAsset(e, result.Flagged ? result.Tentative ? "question" : "flag" : "button", drawX, drawY);
						tilesDrawn++;
					}
					drawY += TileSize;
				}
				drawX += TileSize;
			}

			if (!win && tilesDrawn == Mines)
			{
				win = true;
				faceStatus = Face.Win;
				this.Invalidate();
			}
		}

		private void button1_Click_1(object sender, EventArgs e)
		{
			foreach (var tile in Board)
			{
				tile.Revealed = true;
			}
			this.Invalidate();
		}

		private void button2_Click(object sender, EventArgs e)
		{
			InitializeGame();
			this.Invalidate();
		}

		private void Game_MouseDown(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				if (FaceSelected(e))
				{
					faceStatus = Face.Down;
					selectingFace = true;
					this.Invalidate();
					return;
				}
				else if (!IsGameOver())
				{
					faceStatus = Face.Ooh;

					Tile result;
					if (SelectedButton(e, out int x, out int y))
					{
						result = Board[x, y];
						if (e.Button == MouseButtons.Left && (!result.Flagged || result.Tentative) && !result.Revealed)
						{
							result.Selected = true;
							selectedTile = result;
						}
					}
				}
			}
			else if (e.Button == MouseButtons.Right && selectedTile == null && !IsGameOver())
			{
				Tile result;
				if (SelectedButton(e, out int x, out int y))
				{
					result = Board[x, y];
					if (!result.Revealed)
					{
						if (!result.Flagged)
						{
							result.Flagged = true;
							flaggedMines++;
						}
						else if (result.Flagged && !result.Tentative)
						{
							result.Tentative = true;
							flaggedMines--;
						}
						else if (result.Flagged && result.Tentative)
						{
							result.Flagged = false;
							result.Tentative = false;
						}

					}
				}
			}
			this.Invalidate();

		}

		private void Game_MouseMove(object sender, MouseEventArgs e)
		{
			Tile result;
			if (e.Button.HasFlag(MouseButtons.Left))
			{
				if (selectingFace)
				{
					faceStatus = FaceSelected(e) ? Face.Down : lose ? Face.Dead : win ? Face.Win : Face.Idle;
					this.Invalidate();
				}
				else if (!IsGameOver())
				{
					if (SelectedButton(e, out int x, out int y))
					{
						result = Board[x, y];
						if (!result.Flagged || result.Tentative)
						{
							if (!result.Revealed)
							{
								if (selectedTile != null && (x != selectedTile.X || y != selectedTile.Y))
								{
									selectedTile.Selected = false;
								}
								result.Selected = true;
								selectedTile = result;
							}
							else if (selectedTile != null)
							{
								selectedTile.Selected = false;
								selectedTile = null;
							}
						}
						else if (selectedTile != null && selectedTile.Selected)
						{
							selectedTile.Selected = false;
							selectedTile = null;
						}
					}
					else if (selectedTile != null && selectedTile.Selected)
					{
						selectedTile.Selected = false;
						selectedTile = null;
					}
				}

				this.Invalidate();
			}
		}

		private void Game_MouseUp(object sender, MouseEventArgs e)
		{
			/**
			 * Check if face button is on down state. If mouse is still in range of the button, reset the game.
			 */
			if (e.Button == MouseButtons.Left)
			{
				if (selectingFace && FaceSelected(e))
				{
					InitializeGame();
				}
				else if (!IsGameOver())
				{
					if (selectedTile != null && selectedTile.Selected)
					{
						if (timerShutdown) StartGameTimer();
						selectedTile.Selected = false;
						if (e.Button == MouseButtons.Left && (!selectedTile.Flagged || selectedTile.Tentative) && !selectedTile.Revealed)
						{
							if (selectedTile.Flagged && selectedTile.Tentative)
							{
								selectedTile.Flagged = selectedTile.Tentative = false;
							}
							if (selectedTile.Mine)
							{
								selectedTile.Hit = true;
								PlayerDead();
							}
							else if (selectedTile.AdjacentMines == 0)
							{
								RevealBlankTiles(selectedTile);
							}
							selectedTile.Revealed = true;
						}
						selectedTile = null;
					}

				}
			}
			selectingFace = false;
			if (!IsGameOver()) faceStatus = Face.Idle;
			this.Invalidate();
		}

		private void Game_MouseDoubleClick(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left && NumberClickReveal && selectedTile == null && !IsGameOver())
			{
				Tile result;
				if (SelectedButton(e, out int x, out int y))
				{
					result = Board[x, y];
					if (result.Revealed && !result.Flagged && result.AdjacentMines > 0)
					{
						Tile[] adjacentTiles = result.GetAdjacentTiles();
						if (adjacentTiles != null)
						{
							int flags = 0;
							foreach (Tile adjacent in adjacentTiles)
							{
								if (adjacent != null && adjacent.Flagged && !adjacent.Tentative) flags++;
							}

							if (result.AdjacentMines == flags)
							{
								foreach (Tile adjacent in adjacentTiles)
								{
									if (adjacent != null && !adjacent.Revealed && !adjacent.Flagged)
									{
										if (adjacent.Mine)
										{
											adjacent.Hit = true;
											PlayerDead();
										}
										else if (adjacent.AdjacentMines == 0)
										{
											RevealBlankTiles(adjacent);
										}
										adjacent.Revealed = true;
									}
								}
								this.Invalidate();
							}
						}
					}
				}
			}
		}

		private void Game_FormClosing(object sender, FormClosingEventArgs e)
		{
			timerShutdown = true;
		}
	}

	public enum Face
	{
		Idle,
		Down,
		Ooh,
		Dead,
		Win
	}
}
