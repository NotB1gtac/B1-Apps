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
				AppName = "Calculator",
				Icon = Properties.Resources.Calculator

			};
			tile1.TileClicked += (s, e) => MessageBox.Show("Launching Calculator");

			// Second tile
			AppTile tile2 = new AppTile
			{
				AppName = "Notes",
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
				AppName = "Game",
				Icon = Properties.Resources.Game
			};
			tile3.TileClicked += (s, e) =>
			{
				var gameForm = new B1_Apps.Apps.Game.Form1();
				gameForm.Show(); // nebo ShowDialog(), pokud chceš modální
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
			panel.Controls.Add(tile1);
			panel.Controls.Add(tile2);
			panel.Controls.Add(tile3);
			this.Controls.Add(panel);
			

		}
	}
}
