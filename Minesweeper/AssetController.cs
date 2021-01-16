using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Minesweeper
{
	public static class AssetController
	{
		private static Dictionary<string, Image> InterfaceComponents;
		private static Dictionary<string, Size> SizeMatrix;

		static AssetController()
		{

		}

		public static void Initialize()
		{
			InterfaceComponents = new Dictionary<string, Image>();
			SizeMatrix = new Dictionary<string, Size>();
			if (Directory.Exists("./assets/"))
			{
				FindAssetsRecursive("./assets/");
			}
		}

		public static void DrawAsset(PaintEventArgs e, string key, int x, int y)
		{

			Image image = InterfaceComponents[key];
			if (image != null)
			{
				e.Graphics.DrawImage(image, x, y);

			}
		}

		public static Size GetAssetSize(string key)
		{
			return SizeMatrix[key];
		}

		public static Image GetAsset(string key)
		{
			return InterfaceComponents[key];
		}

		public static void DrawAsset(PaintEventArgs e, string key, int x, int y, out int width, out int height)
		{

			Image image = InterfaceComponents[key];
			if (image != null)
			{
				e.Graphics.DrawImage(image, x, y);
				width = image.Size.Width;
				height = image.Size.Height;
				return;
			}

			width = height = -1;
		}

		private static void FindAssetsRecursive(string directory)
		{
			string[] files = Directory.GetFiles(directory);

			if (files.Length > 0)
			{
				Image image;
				string key;
				foreach (string file in files)
				{
					image = Image.FromFile(file);
					if (image != null)
					{
						key = Path.GetFileNameWithoutExtension(file);
						InterfaceComponents.Add(key, image);
						SizeMatrix.Add(key, image.Size);
					}
				}
			}

			string[] dirs = Directory.GetDirectories(directory);
			if (dirs.Length > 0)
			{
				foreach (string dir in dirs)
				{
					FindAssetsRecursive(dir);
				}
			}

		}

	}
}
