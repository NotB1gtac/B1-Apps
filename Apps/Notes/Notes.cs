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
		private Button btnSaveAll;
		private Button btnNewFile;
		private Panel buttonPanel;
		private Button btnSave;
		private Button btnLoad;
		private string currentFilePath = null;
		private Dictionary<string, string> openedFilesContent = new Dictionary<string, string>();
		private TextBox renameBox = null; // pro přejmenování souboru
		private FlowLayoutPanel filesPanel; // změnil jsem na FlowLayoutPanel
		private RichTextBox notesTextBox;    // nový pro zobrazení textu
		ToolTip tip = new ToolTip();
		

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
			btnSaveAll = new Button
			{
				Text = "Save All",
				Width = 80,
				Left = btnSave.Right + 10,
				Top = 10
			};
			btnSaveAll.Click += BtnSaveAll_Click;
			buttonPanel.Controls.Add(btnSaveAll);

			btnLoad = new Button
			{
				Text = "Load",
				Width = 80,
				Left = btnSaveAll.Right + 10,
				Top = 10
			};
			btnLoad.Click += BtnLoad_Click;
			buttonPanel.Controls.Add(btnLoad);
			btnNewFile = new Button
			{
				Text = "New File",
				Width = 80,
				Left = btnLoad.Right + 10,
				Top = 10
			};
			btnNewFile.Click += BtnNewFile_Click;
			buttonPanel.Controls.Add(btnNewFile);

			// FlowLayoutPanel pod tlačítky pro načtené soubory s pevnou výškou a scrollbarem
			filesPanel = new FlowLayoutPanel
			{
				Dock = DockStyle.Top,
				AutoScroll = true,
				Padding = new Padding(10),
				BackColor = Color.WhiteSmoke,
				FlowDirection = FlowDirection.LeftToRight,
				WrapContents = true
				// Odstraněno pevné Height
			};
			Controls.Add(filesPanel);

			// RichTextBox pro zobrazení obsahu souboru, zabere zbytek prostoru
			notesTextBox = new RichTextBox
			{
				Dock = DockStyle.Fill,  // vyplní zbývající prostor pod panely
				ReadOnly = false,       // uživatel může editovat
				Font = new Font("Segoe UI", 12),
				BackColor = Color.White,
				ForeColor = Color.Black,
				BorderStyle = BorderStyle.FixedSingle
			};
			Controls.Add(notesTextBox);

			// Zajisti správné pořadí - textBox pod panely

			AdjustFilesPanelHeight();

			// Zajistí správné pořadí panelů nahoře -> dolů
			Controls.SetChildIndex(notesTextBox, 0);  // dole
			Controls.SetChildIndex(filesPanel, 1);
			Controls.SetChildIndex(buttonPanel, 2); // nahoře
		}



		private void BtnSave_Click(object sender, EventArgs e)
		{
			if (string.IsNullOrEmpty(currentFilePath))
			{
				MessageBox.Show("Nejdříve otevři soubor kliknutím na tlačítko souboru.");
				return;
			}

			try
			{
				System.IO.File.WriteAllText(currentFilePath, notesTextBox.Text);
				MessageBox.Show("Soubor uložen.");
			}
			catch (Exception ex)
			{
				MessageBox.Show("Chyba při ukládání: " + ex.Message);
			}
		}
		private void BtnSaveAll_Click(object sender, EventArgs e)
		{
			if (openedFilesContent == null || openedFilesContent.Count == 0)
			{
				MessageBox.Show("Žádné soubory nejsou otevřeny.");
				return;
			}

			int savedCount = 0;
			foreach (var kvp in openedFilesContent.ToList()) // ToList, protože budeme možná měnit dictionary
			{
				string filePath = kvp.Key;
				string originalContent = kvp.Value;

				// Pokud je tento soubor aktuálně otevřený v editoru
				if (currentFilePath == filePath)
				{
					// Porovnáme normalizované obsahy
					if (Normalize(notesTextBox.Text) != originalContent)
					{
						try
						{
							File.WriteAllText(filePath, notesTextBox.Text);
							openedFilesContent[filePath] = Normalize(notesTextBox.Text);
							savedCount++;
						}
						catch (Exception ex)
						{
							MessageBox.Show($"Chyba při ukládání souboru {Path.GetFileName(filePath)}: {ex.Message}");
						}
					}
				}
				else
				{
					// Pro soubory, které nejsou aktuálně otevřené v editoru,
					// bys musel mít způsob, jak zjistit jejich aktuální změny (pokud jsi implementoval vícenásobnou editaci)
					// Pokud ne, můžeš je přeskočit nebo přidat další logiku
				}
			}

			if (savedCount > 0)
				MessageBox.Show($"Uloženo {savedCount} souborů.");
			else
				MessageBox.Show("Žádné změny k uložení.");
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
			AdjustFilesPanelHeight();
		}
		private void BtnNewFile_Click(object sender, EventArgs e)
		{
			string folder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments); // nebo jiná cesta
			int i = 1;
			string newFilePath;

			// Najdi první volné jméno NewFileX.txt
			do
			{
				newFilePath = Path.Combine(folder, $"NewFile{i}.txt");
				i++;
			} while (File.Exists(newFilePath));

			try
			{
				File.WriteAllText(newFilePath, ""); // vytvoří prázdný soubor
				AddFileButton(newFilePath);          // přidá ho do UI
			}
			catch (Exception ex)
			{
				MessageBox.Show("Chyba při vytváření souboru: " + ex.Message);
			}
			AdjustFilesPanelHeight();
		}

		private void AdjustFilesPanelHeight()
		{
			int buttonHeight = 60;   // výška tlačítka
			int buttonMargin = 1;   // margin (vertikální) mezi tlačítky

			int effectiveWidth = filesPanel.ClientSize.Width - filesPanel.Padding.Left - filesPanel.Padding.Right;
			int buttonsPerRow = Math.Max(1, effectiveWidth / (150 + buttonMargin)); // 150 je šířka tlačítka

			int rows = (int)Math.Ceiling((double)filesPanel.Controls.Count / buttonsPerRow);

			int newHeight = rows * (buttonHeight + buttonMargin) + filesPanel.Padding.Top + filesPanel.Padding.Bottom;

			filesPanel.Height = Math.Min(newHeight, 300);
		}


		private void AddFileButton(string filePath)
		{
			foreach (Control ctrl in filesPanel.Controls)
			{
				if (ctrl.Tag?.ToString() == filePath)
					return;
			}

			Panel fileContainer = new Panel
			{
				Width = 160,
				Height = 65, // zvýšená výška, aby se vešel label pod tlačítko
				Margin = new Padding(5),
				BackColor = Color.Transparent,
				Tag = filePath
			};

			Button fileButton = new Button
			{
				Text = Path.GetFileName(filePath),
				Size = new Size(135, 30),
				Location = new Point(0, 2),
				Tag = filePath,
				BackColor = Color.LightBlue,
				FlatStyle = FlatStyle.Flat
			};

			Label fileLabel = new Label
			{
				Text = Path.GetFileName(filePath),
				AutoSize = false,
				Size = new Size(135, 25),
				Location = new Point(0, fileButton.Bottom + 1),
				Tag = filePath,
				BackColor = Color.Transparent,
				ForeColor = Color.Black,
				TextAlign = ContentAlignment.MiddleCenter,
				Cursor = Cursors.Hand
			};

			Button closeButton = new Button
			{
				Text = "×",
				Size = new Size(20, 20),
				Location = new Point(fileContainer.Width - 22, 7),
				FlatStyle = FlatStyle.Flat,
				BackColor = Color.Red,
				ForeColor = Color.White,
				Font = new Font("Segoe UI", 8, FontStyle.Bold),
				Visible = false
			};

			tip.SetToolTip(closeButton, "Zavřít soubor");

			// Hover efekty
			System.Windows.Forms.Timer hideTimer = new System.Windows.Forms.Timer { Interval = 200 };
			hideTimer.Tick += (s, e) => { closeButton.Visible = false; hideTimer.Stop(); };

			void ShowCloseButton(object sender, EventArgs e) { hideTimer.Stop(); closeButton.Visible = true; }
			void HideCloseButtonDelayed(object sender, EventArgs e) { hideTimer.Start(); }

			fileContainer.MouseEnter += ShowCloseButton;
			fileContainer.MouseLeave += HideCloseButtonDelayed;
			fileButton.MouseEnter += ShowCloseButton;
			fileButton.MouseLeave += HideCloseButtonDelayed;
			closeButton.MouseEnter += ShowCloseButton;
			closeButton.MouseLeave += HideCloseButtonDelayed;

			// Klik na label = otevření souboru
			fileLabel.Click += (s, e) =>
			{
				currentFilePath = filePath;
				try
				{
					string content = File.ReadAllText(filePath);
					notesTextBox.Text = content;
					notesTextBox.Modified = false;
					openedFilesContent[filePath] = content;
				}
				catch (Exception ex)
				{
					MessageBox.Show("Chyba při načítání souboru: " + ex.Message);
				}
				AdjustFilesPanelHeight();
			};

			// Dvojklik na label = přejmenování
			fileLabel.DoubleClick += (s, e) =>
			{
				TextBox renameBox = new TextBox
				{
					Text = Path.GetFileName(filePath),
					Size = fileLabel.Size,
					Location = fileLabel.Location,
					Font = fileLabel.Font
				};

				fileContainer.Controls.Add(renameBox);
				fileLabel.Visible = false;
				renameBox.BringToFront();
				renameBox.Focus();
				renameBox.SelectAll();

				void RenameConfirmed()
				{
					string newFileName = renameBox.Text.Trim();
					if (string.IsNullOrEmpty(newFileName) || newFileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
					{
						MessageBox.Show("Neplatné jméno souboru.");
						CancelRename();
						return;
					}

					string newFullPath = Path.Combine(Path.GetDirectoryName(filePath), newFileName);
					if (File.Exists(newFullPath))
					{
						MessageBox.Show("Soubor s tímto jménem již existuje.");
						CancelRename();
						return;
					}

					try
					{
						File.Move(filePath, newFullPath);
						fileLabel.Text = newFileName;
						fileContainer.Tag = newFullPath;
						fileButton.Tag = newFullPath;
						fileLabel.Tag = newFullPath;

						if (openedFilesContent.ContainsKey(filePath))
						{
							openedFilesContent[newFullPath] = openedFilesContent[filePath];
							openedFilesContent.Remove(filePath);
						}
						if (currentFilePath == filePath)
							currentFilePath = newFullPath;

						filePath = newFullPath;
						renameBox.Dispose();
						fileLabel.Visible = true;
					}
					catch (Exception ex)
					{
						MessageBox.Show("Chyba při přejmenování: " + ex.Message);
						CancelRename();
					}
				}

				void CancelRename()
				{
					renameBox.Dispose();
					fileLabel.Visible = true;
				}

				renameBox.KeyDown += (sender2, e2) =>
				{
					if (e2.KeyCode == Keys.Enter) RenameConfirmed();
					else if (e2.KeyCode == Keys.Escape) CancelRename();
				};

				renameBox.LostFocus += (sender2, e2) => RenameConfirmed();
			};

			// Kliknutí na zavření
			closeButton.Click += (s, e) =>
			{
				bool isCurrent = currentFilePath == filePath;
				bool hasChanges = isCurrent &&
					openedFilesContent.ContainsKey(filePath) &&
					notesTextBox.Text != openedFilesContent[filePath];

				if (hasChanges)
				{
					var result = MessageBox.Show("Soubor nebyl uložen. Chceš ho uložit před zavřením?", "Uložit změny?", MessageBoxButtons.YesNoCancel);
					if (result == DialogResult.Cancel) return;
					if (result == DialogResult.Yes)
					{
						try
						{
							File.WriteAllText(currentFilePath, notesTextBox.Text);
							notesTextBox.Modified = false;
							openedFilesContent[filePath] = notesTextBox.Text;
						}
						catch (Exception ex)
						{
							MessageBox.Show("Chyba při ukládání: " + ex.Message);
							return;
						}
					}
				}

				filesPanel.Controls.Remove(fileContainer);
				openedFilesContent.Remove(filePath);

				if (isCurrent)
				{
					currentFilePath = null;
					notesTextBox.Clear();
					notesTextBox.Modified = false;
				}
				AdjustFilesPanelHeight();
			};

			fileContainer.Controls.Add(fileButton);
			fileContainer.Controls.Add(fileLabel);
			fileContainer.Controls.Add(closeButton);
			closeButton.BringToFront();
			filesPanel.Controls.Add(fileContainer);
		}



		private string Normalize(string text)
		{
			if (string.IsNullOrEmpty(text))
				return string.Empty;

			// Normalizace konců řádků na LF
			text = text.Replace("\r\n", "\n").Replace("\r", "\n");

			// Trim a odstranění kontrolních znaků
			var sb = new System.Text.StringBuilder();
			foreach (char c in text.Trim())
			{
				if (!char.IsControl(c) || c == '\n')
					sb.Append(c);
			}
			return sb.ToString();
		}
		




	}
}
