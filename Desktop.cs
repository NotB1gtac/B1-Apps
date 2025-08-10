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
			// Top panel for regular app tiles
			var tilePanel = new FlowLayoutPanel
			{
				Dock = DockStyle.Top,
				AutoSize = true,
				AutoSizeMode = AutoSizeMode.GrowAndShrink,
				WrapContents = true,
				Padding = new Padding(20),
				FlowDirection = FlowDirection.LeftToRight
			};

			tilePanel.Controls.Add(CreateTile("Calculator (1.5)", Properties.Resources.Calculator,
				(s, e) => new B1_Apps.Apps.Calculator.CalculatorForm().Show()));
			tilePanel.Controls.Add(CreateTile("Notes (2.1)", Properties.Resources.Notes,
				(s, e) => new B1_Apps.Apps.Notes.NotesMainForm().Show()));
			tilePanel.Controls.Add(CreateTile("KybFighter (4.3)", Properties.Resources.Game,
				(s, e) => new B1_Apps.Apps.Game.Form1().Show()));
			tilePanel.Controls.Add(CreateTile("YT Downloader (2.0)", Properties.Resources.YTdownloader,
				(s, e) => new B1_Apps.Apps.YTdownloader.YTdownloader().Show()));
			tilePanel.Controls.Add(CreateTile("Audio Player (2.1)", Properties.Resources.AudioPlayer,
				(s, e) => new B1_Apps.Apps.AudioPlayer.AudioPlayer().Show()));
			tilePanel.Controls.Add(CreateTile("Video Player (2.2)", Properties.Resources.VideoPlayer,
				(s, e) => new B1_Apps.Apps.VideoPlayer.VideoPlayerForm().Show()));
			tilePanel.Controls.Add(CreateTile("Audio/Video Convert", Properties.Resources.Converter,
				(s, e) => new B1_Apps.Apps.FormatConverter.FormatConverterForm().Show()));

			Controls.Add(tilePanel);

			// Bottom panel to center the large button
			var buttonPanel = new FlowLayoutPanel
			{
				Dock = DockStyle.Top,
				AutoSize = true,
				AutoSizeMode = AutoSizeMode.GrowAndShrink,
				FlowDirection = FlowDirection.LeftToRight,
				WrapContents = false,
				Padding = new Padding(20),
				Anchor = AnchorStyles.Top | AnchorStyles.Left
			};

			var largeBtn = new Button
			{
				Text = "",
				Font = new Font("Segoe UI", 16, FontStyle.Bold),
				Size = new Size(615, 406),
				FlatStyle = FlatStyle.Flat,
				Image = Properties.Resources.LargeIcon, // use your desired image
				ImageAlign = ContentAlignment.MiddleCenter,
				TextAlign = ContentAlignment.BottomCenter,
			};
			largeBtn.Click += (s, e) => MessageBox.Show("Developed by B1gtac© \r\n Distributing this software as YOURS is not cool, otherwise go ahead \r\n USED Nugget packages:" +
				" \r\n AngouriMath \r\n Clipper2 \r\n MathNet \r\n NAudio \r\n YT-dlp \r\n DLSharp \r\n FFMPEG \r\n FUCK THE SEMICOLON AND GRAPHICAL LOOKS");

			buttonPanel.Controls.Add(largeBtn);
			buttonPanel.SetFlowBreak(largeBtn, true); // ensures it's on its own line

			Controls.Add(buttonPanel);
		}

		// Helper method for creating app tiles
		private AppTile CreateTile(string name, Image icon, EventHandler onClick)
		{
			var tile = new AppTile
			{
				AppName = name,
				Icon = icon
			};
			tile.TileClicked += onClick;
			return tile;
		}

	}
}
