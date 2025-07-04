using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace B1_Apps.Apps.Notes
{
	public partial class NotesMainForm : Form
	{
		private IContainer components = null;

		private Panel buttonPanel;
		private Button btnSave;
		private Button btnLoad;

		private FlowLayoutPanel filesPanel; // změnil jsem na FlowLayoutPanel
		private RichTextBox textViewer;    // nový pro zobrazení textu

		public NotesMainForm()
		{
			InitializeComponent();
			InitializeUI();
		}

		private void InitializeComponent()
		{
			this.SuspendLayout();
			// 
			// NotesMainForm
			// 
			this.AutoScaleDimensions = new SizeF(7F, 15F);
			this.AutoScaleMode = AutoScaleMode.Font;
			this.ClientSize = new Size(1752, 962);
			this.Name = "NotesMainForm";
			this.Text = "Notes";
			this.ResumeLayout(false);
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		private void InitializeUI()
		{
			// Panel pro tlačítka nahoře (Save/Load)
			buttonPanel = new Panel
			{
				Dock = DockStyle.Top,
				Height = 50,
				Padding = new Padding(10),
				BackColor = Color.LightGray
			};
			Controls.Add(buttonPanel);

			btnSave = new Button
			{
				Text = "Save",
				Width = 80,
				Left = 10,
				Top = 10
			};
			btnSave.Click += BtnSave_Click;
			buttonPanel.Controls.Add(btnSave);

			btnLoad = new Button
			{
				Text = "Load",
				Width = 80,
				Left = btnSave.Right + 10,
				Top = 10
			};
			btnLoad.Click += BtnLoad_Click;
			buttonPanel.Controls.Add(btnLoad);

			// FlowLayoutPanel pod tlačítky pro načtené soubory s pevnou výškou a scrollbarem
			filesPanel = new FlowLayoutPanel
			{
				Dock = DockStyle.Top,
				Height = 150, // pevná výška
				AutoScroll = true,
				Padding = new Padding(10),
				BackColor = Color.WhiteSmoke,
				FlowDirection = FlowDirection.LeftToRight,
				WrapContents = true
			};
			Controls.Add(filesPanel);

			// RichTextBox pro zobrazení obsahu souboru, zabere zbytek prostoru
			textViewer = new RichTextBox
			{
				Dock = DockStyle.Fill,
				ReadOnly = true,
				Font = new Font("Consolas", 12),
				BackColor = Color.White
			};
			Controls.Add(textViewer);

			// Zajistí správné pořadí panelů nahoře -> dolů
			Controls.SetChildIndex(textViewer, 0);  // dole
			Controls.SetChildIndex(filesPanel, 1);
			Controls.SetChildIndex(buttonPanel, 2); // nahoře
		}



		private void BtnSave_Click(object sender, EventArgs e)
		{
			MessageBox.Show("Save clicked - ještě neimplementováno.");
		}

		private void BtnLoad_Click(object sender, EventArgs e)
		{
			using (OpenFileDialog dlg = new OpenFileDialog())
			{
				dlg.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
				dlg.Multiselect = true;

				if (dlg.ShowDialog() == DialogResult.OK)
				{
					foreach (var file in dlg.FileNames)
					{
						AddFileButton(file);
					}
				}
			}
		}

		private void AdjustFilesPanelHeight()
		{
			int buttonHeight = 30;   // výška jednoho tlačítka (případně s marginem)
			int buttonMargin = 10;   // předpokládám 5 px margin nahoře i dole, celkem 10

			int buttonsPerRow = Math.Max(1, filesPanel.ClientSize.Width / (150 + buttonMargin));
			// 150 je šířka tlačítka, uprav podle reálné šířky
			int rows = (int)Math.Ceiling((double)filesPanel.Controls.Count / buttonsPerRow);

			int newHeight = rows * (buttonHeight + buttonMargin) + 20; // + padding

			filesPanel.Height = Math.Min(newHeight, 300); // můžeš nastavit max výšku, pak se objeví scroll
		}

		private void AddFileButton(string filePath)
		{
			foreach (Button btn in filesPanel.Controls.OfType<Button>())
			{
				if (btn.Tag != null && btn.Tag.ToString() == filePath)
					return;
			}

			Button fileButton = new Button
			{
				Text = System.IO.Path.GetFileName(filePath),
				Width = 150,
				Height = 30,
				Margin = new Padding(5),
				Tag = filePath
			};

			fileButton.Click += (s, e) =>
			{
				try
				{
					string content = File.ReadAllText(filePath);
					textViewer.Text = content;
				}
				catch (Exception ex)
				{
					MessageBox.Show($"Chyba při načítání souboru:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			};

			filesPanel.Controls.Add(fileButton);
			AdjustFilesPanelHeight();
		}

	}
}
