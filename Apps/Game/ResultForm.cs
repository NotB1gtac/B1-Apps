using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace B1_Apps.Apps.Game
{
	public partial class ResultForm : Form
	{
		private TextBox resultsTextBox;
		public ResultForm()
		{
			InitializeComponent();
			InitializeUI();
			LoadResults();
		}
		private void InitializeUI()
		{
			this.Text = "Historie zápasů";
			this.Size = new Size(600, 400);

			resultsTextBox = new TextBox();
			resultsTextBox.Multiline = true;
			resultsTextBox.ReadOnly = true;
			resultsTextBox.Dock = DockStyle.Fill;
			resultsTextBox.ScrollBars = ScrollBars.Vertical;

			this.Controls.Add(resultsTextBox);
		}

		private void LoadResults()
		{
			string filePath = "results.txt";
			if (File.Exists(filePath))
			{
				resultsTextBox.Text = File.ReadAllText(filePath);
			}
			else
			{
				resultsTextBox.Text = "Zatím žádné výsledky.";
			}
		}
	}
}
