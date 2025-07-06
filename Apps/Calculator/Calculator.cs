using System;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace B1_Apps.Apps.Calculator
{
	public partial class CalculatorForm : Form
	{
		private IContainer components = null;
		private TextBox display;

		public CalculatorForm()
		{
			InitializeComponent();
			SetupUI();
		}

		private void InitializeComponent()
		{
			this.components = new Container();
			this.AutoScaleMode = AutoScaleMode.Font;
			this.ClientSize = new Size(340, 500);
			this.Text = "Calculator";
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
				components.Dispose();
			base.Dispose(disposing);
		}

		private void SetupUI()
		{
			this.FormBorderStyle = FormBorderStyle.FixedSingle;
			this.MaximizeBox = false;

			var mainLayout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1 };
			mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));
			mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
			Controls.Add(mainLayout);

			display = new TextBox
			{
				Name = "DisplayBox",
				Font = new Font("Segoe UI", 24),
				ReadOnly = true,
				TextAlign = HorizontalAlignment.Right,
				Dock = DockStyle.Fill,
				Text = "0",
				BackColor = Color.White
			};
			mainLayout.Controls.Add(display, 0, 0);

			var grid = new TableLayoutPanel { RowCount = 5, ColumnCount = 4, Dock = DockStyle.Fill, Padding = new Padding(5) };
			for (int i = 0; i < 4; i++) grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
			for (int i = 0; i < 5; i++) grid.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));
			mainLayout.Controls.Add(grid, 0, 1);

			string[,] buttons = {
				{ "7", "8", "9", "/" },
				{ "4", "5", "6", "*" },
				{ "1", "2", "3", "-" },
				{ "0", ".", "=", "+" },
				{ "C", "(", ")", "" }
			};

			for (int idx = 0; idx < buttons.Length; idx++)
			{
				string text = buttons[idx / 4, idx % 4];
				if (string.IsNullOrWhiteSpace(text)) continue;
				int row = idx / 4, col = idx % 4;
				var btn = new Button
				{
					Text = text,
					Font = new Font("Segoe UI", 18),
					Dock = DockStyle.Fill,
					FlatStyle = FlatStyle.Flat,
					Tag = text
				};
				btn.Click += OnButtonClick;
				grid.Controls.Add(btn, col, row);
			}
		}

		private void OnButtonClick(object sender, EventArgs e)
		{
			if (sender is Button btn && btn.Tag is string value)
				HandleInput(value);
		}

		private void HandleInput(string value)
		{
			if (value == "C")
			{
				display.Text = "0";
				return;
			}
			if (value == "=")
			{
				EvaluateExpression();
				return;
			}

			if (display.Text == "0" && value != ".")
				display.Text = value;
			else
				display.Text += value;
		}

		private void EvaluateExpression()
		{
			try
			{
				// Normalize comma to dot for evaluation
				string expr = display.Text.Replace(",", ".");

				// Temporarily set culture to InvariantCulture
				var originalCulture = Thread.CurrentThread.CurrentCulture;
				Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

				// Evaluate expression
				var resultObj = new DataTable().Compute(expr, "");

				// Restore original culture
				Thread.CurrentThread.CurrentCulture = originalCulture;

				// Display result
				display.Text = Convert.ToDouble(resultObj).ToString("G", CultureInfo.InvariantCulture);
			}
			catch
			{
				display.Text = "Error";
			}
		}

	}
}
