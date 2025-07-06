using System;
using System.Drawing;
using System.Windows.Forms;
using B1_Apps.Apps.Game;



namespace B1_Apps
{
	public partial class Desktop : Form
	{
		public Desktop()
		{
			InitializeComponent();
			InitializeAppTiles();
		}

		private void InitializeAppTiles()
		{
			


			// First tile
			AppTile tile1 = new AppTile
			{
				AppName = "Calculator (1.5)",
				Icon = Properties.Resources.Calculator

			};
			tile1.TileClicked += (s, e) =>
			{
				var calculatorForm = new B1_Apps.Apps.Calculator.CalculatorForm();
				calculatorForm.Show(); // nebo ShowDialog(), pokud chceš modální
			};

			// Second tile
			AppTile tile2 = new AppTile
			{
				AppName = "Notes (2.1)",
				Icon = Properties.Resources.Notes
			};
			tile2.TileClicked += (s, e) =>
			{
				var notesForm = new B1_Apps.Apps.Notes.NotesMainForm();
				notesForm.Show(); // nebo ShowDialog(), pokud chceš modální
			};

			// Third tile
			AppTile tile3 = new AppTile
			{
				AppName = "KybFighter (4.3) ",
				Icon = Properties.Resources.Game
			};
			tile3.TileClicked += (s, e) =>
			{
				var gameForm = new B1_Apps.Apps.Game.Form1();
				gameForm.Show(); // nebo ShowDialog(), pokud chceš modální
			};
			AppTile tile4 = new AppTile
			{
				AppName = "YT Downloader (0.1)",
				Icon = Properties.Resources.YTdownloader
			};
			tile4.TileClicked += (s, e) =>
			{
				var YTdownloaderForm = new B1_Apps.Apps.YTdownloader.YTdownloader();
				YTdownloaderForm.Show(); // nebo ShowDialog(), pokud chceš modální
			};
			// ====================================================================
			FlowLayoutPanel panel = new FlowLayoutPanel();
			panel.Dock = DockStyle.Fill;
			panel.AutoScroll = true;
			panel.WrapContents = true;
			panel.Padding = new Padding(20);
			panel.Margin = new Padding(20);
			panel.FlowDirection = FlowDirection.LeftToRight;

			panel.Controls.Add(tile1);
			panel.Controls.Add(tile2);
			panel.Controls.Add(tile3);
			panel.Controls.Add(tile4);
			
			this.Controls.Add(panel);
			

		}
	}
}
