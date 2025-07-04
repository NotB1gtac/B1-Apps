using System;
using System.Drawing;
using System.Windows.Forms;

namespace B1_Apps
{
	public class AppTile : UserControl
	{
		private PictureBox pictureBox = new PictureBox();
		private Label label = new Label();

		private Size normalSize;
		private Size hoverSize;

		public AppTile()
		{
			normalSize = new Size(120, 120);
			hoverSize = new Size((int)(normalSize.Width * 1.1), (int)(normalSize.Height * 1.1));

			this.Size = normalSize;
			this.Cursor = Cursors.Hand;
			this.BackColor = Color.FromArgb(30, 30, 30);

			pictureBox.Dock = DockStyle.Fill;
			pictureBox.SizeMode = PictureBoxSizeMode.Zoom;
			pictureBox.BackColor = Color.White;

			label.Dock = DockStyle.Bottom;
			label.TextAlign = ContentAlignment.MiddleCenter;
			label.Height = 25;
			label.ForeColor = Color.Black;
			label.BackColor = Color.FromArgb(180, 255, 255, 255);

			this.Controls.Add(pictureBox);
			this.Controls.Add(label);

			this.Margin = new Padding(35);

			this.Click += (s, e) => TileClicked?.Invoke(this, e);
			pictureBox.Click += (s, e) => TileClicked?.Invoke(this, e);
			label.Click += (s, e) => TileClicked?.Invoke(this, e);

			AddHoverHandlers(this);
			AddHoverHandlers(pictureBox);
			AddHoverHandlers(label);
		}

		private void AddHoverHandlers(Control ctrl)
		{
			ctrl.MouseEnter += (s, e) =>
			{
				this.BackColor = Color.FromArgb(60, 60, 60);
				this.Size = hoverSize;
				this.Invalidate();
			};

			ctrl.MouseLeave += (s, e) =>
			{
				// Check if mouse left the whole UserControl
				if (!this.ClientRectangle.Contains(this.PointToClient(Cursor.Position)))
				{
					this.BackColor = Color.FromArgb(30, 30, 30);
					this.Size = normalSize;
					this.Invalidate();
				}
			};
		}


		public Image Icon
		{
			get => pictureBox.Image;
			set => pictureBox.Image = value;
		}

		public string AppName
		{
			get => label.Text;
			set => label.Text = value;
		}

		public event EventHandler TileClicked;
	}
}
