namespace B1_Apps.Apps.Game
{
	using System;
	using System.Collections.Generic;
	using System.Drawing;
	using System.Windows.Forms;
	using System.Threading.Tasks;
	using System.Linq;
	using static B1_Apps.Apps.Game.Character;
	using System.Diagnostics;
	using static B1_Apps.Apps.Game.Form1;
	using System.Media;
	using System.Reflection;
	using Clipper2Lib; 



	public partial class Form1 : Form
	{
		private ListBox listBoxCharacters;
		private Button btnSelectPlayer1;
		private Button btnSelectPlayer2;
		private Label lblPlayer1;
		private Label lblPlayer2;
		private PictureBox pictureBoxCharacter;

		private List<Character> allCharacters = new List<Character>();
		private Character player1;
		private Character player2;
		private PictureBox Player_1;
		private PictureBox Player_2;
		private Button btnStart;
		private HashSet<Keys> keysPressed = new HashSet<Keys>();
		private Timer movementTimer;
		private Label lblPlayer1Name;
		private Label lblPlayer2Name;
		// Status bars
		private Panel statusBar;
		private PictureBox portrait1, portrait2;
		private Label lblP1Tag, lblP2Tag;
		private Label lblP1Name, lblP2Name;
		private Label lblP1HP, lblP2HP;
		private Label lblP1Ammo;
		private Label lblP2Ammo;
		private ProgressBar magazineBarP1;
		private ProgressBar magazineBarP2;
		private int player1CurrentAmmo;
		private int player2CurrentAmmo;
		private Label lblDescription;
		Timer fireTimerP1 = new Timer();
		Timer fireTimerP2 = new Timer();
		bool canFireP1 = true;
		bool canFireP2 = true;
		List<Bullet> bullets = new List<Bullet>();
		Timer gameTimer = new Timer();
		const int bulletMaxLifetime = 2000;
		private bool isReloadingP1 = false;
		private bool isReloadingP2 = false;

		private Timer reloadTimerP1 = new Timer();
		private Timer reloadTimerP2 = new Timer();
		
		private bool isFiringP1 = false;
		private Timer holdFireTimerP1;
		private bool isShootingP1 = false;
		private bool isShootingP2 = false;

		private Timer shootingTimerP1;
		private Timer shootingTimerP2;
		private int player1AccurateShots = 0;
		private int player2AccurateShots = 0;
		private const int AccurateShotsCount = 4;
		private DateTime lastShootTimeP1 = DateTime.MinValue;
		private DateTime lastShootTimeP2 = DateTime.MinValue;
		//private List<Rectangle> obstacles = new List<Rectangle>();
		List<Path64> obstaclePolygons = new List<Path64>();
		private Label lblCoordinates;
		private bool shooting = false;

		private List<Explosion> activeExplosions = new List<Explosion>();
		private List<DamagePopup> damagePopups = new List<DamagePopup>();

		public int ExplosiveDamage { get; set; } = 600; //explosive damage value

		private Button btnShowResults;
		private Timer powerCooldownTimer = new Timer();
		private int cooldownDurationMs = 5000; // třeba 5 sekund
		private int cooldownRemainingP1 = 0;
		private int cooldownRemainingP2 = 0;
		private ProgressBar cooldownBarP1;
		private ProgressBar cooldownBarP2;
		private List<string> songs = new List<string>();
		private int currentSongIndex = 0;
		private SoundPlayer soundPlayer = new SoundPlayer();
		private bool isMusicPlaying = false;
		private Button btnPlayPause;
		private Button btnPrevious;
		private Button btnNext;
		private Label lblSongName;
		private ProgressBar shieldBar1;
		private ProgressBar shieldBar2;
		LoopingSoundPlayer player1SoundPlayer = new LoopingSoundPlayer();
		LoopingSoundPlayer player2SoundPlayer = new LoopingSoundPlayer();
		private ProgressBar nowallBar1;
		private ProgressBar nowallBar2;










		private async Task ReloadAsync(bool isPlayer1)
		{
			if (player1 != null || player2 != null)
			{
				if (isPlayer1)
				{
					if (isReloadingP1) return;
					isReloadingP1 = true;
					canFireP1 = false;

					await Task.Delay(player1.ReloadTime);

					player1CurrentAmmo = player1.MagazineSize;
					isReloadingP1 = false;
					canFireP1 = true;
					UpdateMagazineBars();
				}
				else
				{
					if (isReloadingP2) return;
					isReloadingP2 = true;
					canFireP2 = false;

					await Task.Delay(player2.ReloadTime);

					player2CurrentAmmo = player2.MagazineSize;
					isReloadingP2 = false;
					canFireP2 = true;
					UpdateMagazineBars();
				}
			}
			else
			{ 
				Task.Delay(1000).Wait(); // čekej 1 sekundu, pokud nejsou hráči vybráni
				if (isPlayer1)
				{
					if (isReloadingP1) return;
					isReloadingP1 = true;
					canFireP1 = false;

					await Task.Delay(player1.ReloadTime);

					player1CurrentAmmo = player1.MagazineSize;
					isReloadingP1 = false;
					canFireP1 = true;
					UpdateMagazineBars();
				}
				else
				{
					if (isReloadingP2) return;
					isReloadingP2 = true;
					canFireP2 = false;

					await Task.Delay(player2.ReloadTime);

					player2CurrentAmmo = player2.MagazineSize;
					isReloadingP2 = false;
					canFireP2 = true;
					UpdateMagazineBars();
				}
			}
			
		}







		private void UpdateShieldBarForPlayer(Character player)
		{
			if (player == null) return; // pokud hráč není vybrán, nic nedělej
			ProgressBar bar = player == player1 ? shieldBar1 : shieldBar2;

			if (player.IsShieldActive)
			{
				bar.Visible = true;
				bar.Value = Math.Max(0, Math.Min(bar.Maximum, player.ShieldHealth));
			}
			else
			{
				bar.Visible = false;
			}
		}
		private void UpdateNowallBarForPlayer(Character player)
		{
			if (player == null) return; // pokud hráč není vybrán, nic nedělej
			ProgressBar bar = player == player1 ? nowallBar1 : nowallBar2;

			if (player.IsNoWallActive)
			{
				bar.Visible = true;
				bar.Value = Math.Max(0, Math.Min(bar.Maximum, player.NoWallHealth));
			}
			else
			{
				bar.Visible = false;
			}
		}

		public Form1()
		{
			InitializeComponent();
			InitializeGameUI();
			LoadCharacters();
			using (var ms = new MemoryStream())
			{
				Properties.GameResources.Explosion_BIG.CopyTo(ms);
				explosionBigSound = ms.ToArray();
			}

			using (var ms = new MemoryStream())
			{
				Properties.GameResources.Explosion_small.CopyTo(ms);
				explosionSmallSound = ms.ToArray();
			}
			using (var ms = new MemoryStream())
			{
				Properties.GameResources.Explosion_wallpierce.CopyTo(ms);
				explosionWallPierceSound = ms.ToArray();
			}
			using (var ms = new MemoryStream())
			{
				Properties.GameResources.Ability_Dash.CopyTo(ms);
				dashSound = ms.ToArray();
			}
			using (var ms = new MemoryStream())
			{
				Properties.GameResources.Ability_Shield_Start.CopyTo(ms);
				shieldStartSound = ms.ToArray();
			}
			using (var ms = new MemoryStream())
			{
				Properties.GameResources.Ability_Shield_End.CopyTo(ms);
				shieldEndSound = ms.ToArray();
			}



			this.KeyPreview = true;
			this.KeyDown += Form1_KeyDown;
			this.KeyUp += Form1_KeyUp;

			movementTimer = new Timer();
			movementTimer.Interval = 20;
			movementTimer.Tick += MovementTimer_Tick;
			movementTimer.Start();
			btnShowResults.TabStop = false;


			fireTimerP1 = new Timer();
			fireTimerP1.Tick += (s, e) =>
			{
				canFireP1 = true;
				fireTimerP1.Stop();
			};

			fireTimerP2 = new Timer();
			fireTimerP2.Tick += (s, e) =>
			{
				canFireP2 = true;
				fireTimerP2.Stop();
			};
			gameTimer.Interval = 15; //  60 FPS
			gameTimer.Tick += GameTimer_Tick;
			gameTimer.Start();
			reloadTimerP1.Interval = 1000; // default
			reloadTimerP2.Interval = 1000; // default
			shootingTimerP1 = new Timer();
			
			shootingTimerP1 = new Timer();
			shootingTimerP1.Interval = 500; 
			shootingTimerP1.Tick += (s, e) =>
			{
				if (!isReloadingP1)
				{
					Shoot(player1, Player_1.Location, true);
				}
			};

			shootingTimerP2 = new Timer();
			shootingTimerP2.Interval = 500;
			shootingTimerP2.Tick += (s, e) =>
			{
				if (!isReloadingP2)
				{
					Shoot(player2, Player_2.Location, false);
				}
			};


			movementTimer = new Timer();
			movementTimer.Interval = 20;
			movementTimer.Tick += MovementTimer_Tick;
			movementTimer.Start();
			this.DoubleBuffered = true; // pro plynulé vykreslování
			powerCooldownTimer.Interval = 100; // 100 ms update
			powerCooldownTimer.Tick += PowerCooldownTimer_Tick;
			powerCooldownTimer.Start();

			InitObstacles();
			foreach (var c in allCharacters)
			{
				c.AssignPower(this);

			}
			string musicFolder = Path.Combine(Application.StartupPath, "Music");

			// Load all WAV files in the folder
			
			try
			{
				songs = Directory.GetFiles(musicFolder, "*.wav").ToList();
				if (songs.Count == 0)
				{
					MessageBox.Show("No songs found in /Music folder.");
					return;
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Error loading songs: {ex.Message}");
				return;
			}
			

			currentSongIndex = 0;
			LoadCurrentSong();
			btnPlayPause.Text = "Play Music";

			


		}
		class Bullet
		{
			public PointF Position;
			public PointF Velocity;
			public int Lifetime; // ms
			public Character Owner { get; set; }
			public bool IsExplosive => Owner.isExplosive;
			public bool IsWallExplosive { get; set; } = false;

			public bool IsWallPiercing { get; set; } = false;

		}
		private void PowerCooldownTimer_Tick(object sender, EventArgs e)
		{
			if (cooldownRemainingP1 > 0)
			{
				cooldownRemainingP1 -= powerCooldownTimer.Interval;
				cooldownBarP1.Value = Math.Max(0, 100 - (cooldownRemainingP1 * 100 / cooldownDurationMs));
				
			}
			else cooldownBarP1.Value = 100;

			if (cooldownRemainingP2 > 0)
			{
				cooldownRemainingP2 -= powerCooldownTimer.Interval;
				cooldownBarP2.Value = Math.Max(0, 100 - (cooldownRemainingP2 * 100 / cooldownDurationMs));
			}
			else cooldownBarP2.Value = 100;
			//Text = $"Cooldown P1: {cooldownBarP1.Value}"; // zobrazí zbývající čas v sekundách
		}



		private void InitializeGameUI()
		{

			{
				btnShowResults = new Button();
				btnShowResults.Text = "Zobrazit historii zápasů";
				btnShowResults.AutoSize = true;
				btnShowResults.Location = new Point(310, 10); // přizpůsob si pozici
				btnShowResults.Click += BtnShowResults_Click;

				this.Controls.Add(btnShowResults);
			}

			// ListBox for characters
			listBoxCharacters = new ListBox
			{
				Location = new Point(10, 10),
				Size = new Size(200, 150)
			};
			listBoxCharacters.SelectedIndexChanged += ListBoxCharacters_SelectedIndexChanged;

			Controls.Add(listBoxCharacters);
			lblDescription = new Label
			{
				Location = new Point(220, 360),
				Size = new Size(360, 80),
				ForeColor = Color.Black,
				BackColor = Color.Transparent,
				AutoSize = false,
				Visible = true,
				MaximumSize = new Size(360, 90) // pro zalamování textu
			};
			Controls.Add(lblDescription);


			// Button to select Player 1 // the button of insanity which cost me around 60  hours of fucking debugging
			//i fucking hate you i hate evrything i hate this fucking button i hate this fucking game i hate everything
			// i swear to god if i have to deal with this again Ill fucking kill myself


			btnSelectPlayer1 = new NoFocusButton
			{
				Text = "Select for Player 1",
				Location = new Point(220, 10)
			};
			btnSelectPlayer1.Click += BtnSelectPlayer1_Click;
			Controls.Add(btnSelectPlayer1);

			// Button to select Player 2
			btnSelectPlayer2 = new NoFocusButton
			{
				Text = "Select for Player 2",
				Location = new Point(220, 50)
			};
			btnSelectPlayer2.Click += BtnSelectPlayer2_Click;
			Controls.Add(btnSelectPlayer2);

			// Label for Player 1
			lblPlayer1 = new Label
			{
				Text = "Player 1: none",
				Location = new Point(10, 170),
				AutoSize = true
			};
			Controls.Add(lblPlayer1);

			// Label for Player 2
			lblPlayer2 = new Label
			{
				Text = "Player 2: none",
				Location = new Point(10, 200),
				AutoSize = true
			};
			Controls.Add(lblPlayer2);

			// PictureBox for character portrait
			pictureBoxCharacter = new PictureBox
			{
				Location = new Point(220, 100),
				Size = new Size(150, 150),
				SizeMode = PictureBoxSizeMode.StretchImage,
				BorderStyle = BorderStyle.FixedSingle
			};
			Controls.Add(pictureBoxCharacter);
			btnStart = new NoFocusButton
			{
				Text = "Start",
				Location = new Point(220, 480)
			};
			btnStart.Click += BtnStart_Click;
			Controls.Add(btnStart);


			// Player 1 PictureBox (hidden initially)
			Player_1 = new PictureBox
			{
				Location = new Point(50, 50),
				BorderStyle = BorderStyle.FixedSingle,
				SizeMode = PictureBoxSizeMode.StretchImage,
				Visible = false // hidden until start
			};
			Controls.Add(Player_1);

			// Player 2 PictureBox (hidden initially)
			Player_2 = new PictureBox
			{
				Location = new Point(1850, 1000),
				BorderStyle = BorderStyle.FixedSingle,
				SizeMode = PictureBoxSizeMode.StretchImage,
				Visible = false // hidden until start
			};
			Controls.Add(Player_2);
			// Label for Player 1 name
			lblPlayer1Name = new Label
			{
				AutoSize = true,
				BackColor = Color.Transparent,
				ForeColor = Color.Black,
				Font = new Font("Arial", 10, FontStyle.Bold),
				Text = "Player 1",
				Visible = false
			};
			Controls.Add(lblPlayer1Name);

			// Label for Player 2 name
			lblPlayer2Name = new Label
			{
				AutoSize = true,
				BackColor = Color.Transparent,
				ForeColor = Color.Black,
				Font = new Font("Arial", 10, FontStyle.Bold),
				Text = "Player 2",
				Visible = false
			};
			Controls.Add(lblPlayer2Name);
			// === BOTTOM STATUS BAR ===
			statusBar = new Panel
			{
				Height = 120,
				Dock = DockStyle.Bottom,
				BackColor = Color.FromArgb(30, 30, 30)
			};
			Controls.Add(statusBar);

			// === PLAYER 1 UI (Bottom-Left) ===
			portrait1 = new PictureBox
			{
				Size = new Size(80, 80),
				Location = new Point(10, 20),
				SizeMode = PictureBoxSizeMode.StretchImage
			};
			statusBar.Controls.Add(portrait1);

			lblP1Tag = new Label
			{
				Text = "Player 1",
				Font = new Font("Arial", 9, FontStyle.Bold),
				ForeColor = Color.White,
				Location = new Point(100, 20),
				AutoSize = true
			};
			statusBar.Controls.Add(lblP1Tag);

			lblP1Name = new Label
			{
				Font = new Font("Arial", 9, FontStyle.Regular),
				ForeColor = Color.LightGray,
				Location = new Point(100, 45),
				AutoSize = true
			};
			statusBar.Controls.Add(lblP1Name);

			lblP1HP = new Label
			{
				Font = new Font("Arial", 9, FontStyle.Regular),
				ForeColor = Color.Lime,
				Location = new Point(100, 70),
				AutoSize = true
			};
			statusBar.Controls.Add(lblP1HP);
			magazineBarP1 = new ProgressBar
			{
				Size = new Size(80, 8), // smaller height
				Location = new Point(100, 95), // below HP label or wherever fits
				Maximum = 100,
				Value = 100
			};
			statusBar.Controls.Add(magazineBarP1);

			// Player 1 Magazine ammo label
			lblP1Ammo = new Label
			{

				Font = new Font("Arial", 8, FontStyle.Regular),
				ForeColor = Color.LightGray,
				Location = new Point(100, 105), // just below the bar
				AutoSize = true
			};
			statusBar.Controls.Add(lblP1Ammo);

			// === PLAYER 2 UI (Bottom-Right) ===
			portrait2 = new PictureBox
			{
				Size = new Size(80, 80),
				SizeMode = PictureBoxSizeMode.StretchImage
			};
			statusBar.Controls.Add(portrait2);

			lblP2Tag = new Label
			{
				Text = "Player 2",
				Font = new Font("Arial", 9, FontStyle.Bold),
				ForeColor = Color.White
			};
			statusBar.Controls.Add(lblP2Tag);

			lblP2Name = new Label
			{
				Font = new Font("Arial", 9, FontStyle.Regular),
				ForeColor = Color.LightGray
			};
			statusBar.Controls.Add(lblP2Name);

			lblP2HP = new Label
			{
				Font = new Font("Arial", 9, FontStyle.Regular),
				ForeColor = Color.Lime
			};
			statusBar.Controls.Add(lblP2HP);
			btnPlayPause = new NoFocusButton
			{
				Text = "Play Music",
				AutoSize = true,
				Location = new Point(500, 10),
				TabStop = false
			};
			btnPlayPause.Click += btnPlayPause_Click;
			Controls.Add(btnPlayPause);

			btnPrevious = new NoFocusButton
			{
				Text = "<<",
				AutoSize = true,
				Location = new Point(420, 10),
				TabStop = false
			};
			btnPrevious.Click += btnPrevious_Click;
			Controls.Add(btnPrevious);

			btnNext = new NoFocusButton
			{
				Text = ">>",
				AutoSize = true,
				Location = new Point(580, 10),
				TabStop = false

			};
			btnNext.Click += btnNext_Click;
			Controls.Add(btnNext);
			lblSongName = new Label
			{
				Text = "No song loaded",
				AutoSize = true,
				Location = new Point(700, 10),
				ForeColor = Color.White,
				BackColor = Color.Black,
				Font = new Font("Consolas", 10)
			};
			

			// Position Player 2 elements on the bottom-right
			int padding = 10;
			int rightX = statusBar.Width - 80 - padding;

			portrait2.Location = new Point(rightX, 20);
			lblP2Tag.Location = new Point(rightX - 100, 20);
			lblP2Name.Location = new Point(rightX - 100, 45);
			lblP2HP.Location = new Point(rightX - 100, 70);

			// Optional: reposition on resize
			this.Resize += (s, e) =>
			{
				int right = statusBar.Width - 80 - padding;
				portrait2.Location = new Point(right, 20);
				lblP2Tag.Location = new Point(right - 100, 20);
				lblP2Name.Location = new Point(right - 100, 45);
				lblP2HP.Location = new Point(right - 100, 70);
			};

			// Player 2 Magazine Bar
			magazineBarP2 = new ProgressBar
			{
				Size = new Size(80, 8),
				Location = new Point(portrait2.Location.X - 90, 95),
				Maximum = 100,
				Value = 100
			};
			statusBar.Controls.Add(magazineBarP2);

			// Player 2 Magazine ammo label
			lblP2Ammo = new Label
			{

				Font = new Font("Arial", 8, FontStyle.Regular),
				ForeColor = Color.LightGray,
				Location = new Point(portrait2.Location.X - 90, 105),
				AutoSize = true
			};
			statusBar.Controls.Add(lblP2Ammo);

			Label lblCoordinates = new Label
			{
				AutoSize = true,
				Location = new Point(500, 500),
				ForeColor = Color.White,
				BackColor = Color.Black,
				Font = new Font("Consolas", 10),
				Name = "lblCoordinates"
			};
			this.Controls.Add(lblCoordinates);
			cooldownBarP1 = new ProgressBar
			{
				Size = new Size(80, 6),
				Location = new Point(10, 105),
				Maximum = 100,
				Value = 100 // plně nabitý na začátku
			};
			statusBar.Controls.Add(cooldownBarP1);

			// Player 2 Cooldown Bar
			cooldownBarP2 = new ProgressBar
			{
				Size = new Size(80, 6),
				Location = new Point(portrait2.Location.X, 105),
				Maximum = 100,
				Value = 100
			};
			statusBar.Controls.Add(cooldownBarP2);

			shieldBar1 = new ProgressBar();
			shieldBar1.Location = new Point(50, 50);
			shieldBar1.Size = new Size(100, 10);
			shieldBar1.Maximum = 800;
			shieldBar1.Visible = false;
			Controls.Add(shieldBar1);

			shieldBar2 = new ProgressBar();
			shieldBar2.Location = new Point(this.ClientSize.Width - 150, 50);
			shieldBar2.Size = new Size(100, 10);
			shieldBar2.Maximum = 800;
			shieldBar2.Visible = false;
			Controls.Add(shieldBar2);

			nowallBar1 = new ProgressBar();
			nowallBar1.Location = new Point(50, 50);
			nowallBar1.Size = new Size(100, 10);
			nowallBar1.Maximum = 800;
			nowallBar1.Visible = false;
			Controls.Add(nowallBar1);

			nowallBar2 = new ProgressBar();
			nowallBar2.Location = new Point(this.ClientSize.Width - 150, 50);
			nowallBar2.Size = new Size(100, 10);
			nowallBar2.Maximum = 800;
			nowallBar2.Visible = false;
			Controls.Add(nowallBar2);


		}

		private void BtnShowResults_Click(object sender, EventArgs e)
		{
			ResultForm resultsForm = new ResultForm();
			resultsForm.ShowDialog(); // modalní okno
		}
		private void LoadCurrentSong()
		{
			soundPlayer.Stop();
			soundPlayer.SoundLocation = songs[currentSongIndex];
			soundPlayer.Load();

			if (isMusicPlaying)
				soundPlayer.PlayLooping();

			UpdateSongLabel(); // optional
		}
		private void btnPlayPause_Click(object sender, EventArgs e)
		{
			if (!isMusicPlaying)
			{
				soundPlayer.PlayLooping();
				btnPlayPause.Text = "Stop Music";
				isMusicPlaying = true;
			}
			else
			{
				soundPlayer.Stop();
				btnPlayPause.Text = "Play Music";
				isMusicPlaying = false;
			}
		}
		private void btnNext_Click(object sender, EventArgs e)
		{
			if (songs.Count == 0) return;

			currentSongIndex = (currentSongIndex + 1) % songs.Count;
			LoadCurrentSong();
		}
		private void btnPrevious_Click(object sender, EventArgs e)
		{
			if (songs.Count == 0) return;

			currentSongIndex = (currentSongIndex - 1 + songs.Count) % songs.Count;
			LoadCurrentSong();
		}
		private void UpdateSongLabel()
		{
			lblSongName.Text = Path.GetFileNameWithoutExtension(songs[currentSongIndex]);
		}


		private void Form1_KeyDown(object sender, KeyEventArgs e)
		{
			if (player1 !=null && player2 != null)
			{


				// Already pressed? Ignore this keydown (helps with semi-auto detection)
				bool alreadyPressed = keysPressed.Contains(e.KeyCode);

				if (!alreadyPressed)
					keysPressed.Add(e.KeyCode);

				// === Player 1 ===
				if (e.KeyCode == Keys.Space && !isReloadingP1 )
				{
					// Full auto
					if (player1.isFullAuto)
					{
						if (!shootingTimerP1.Enabled)
						{
							Shoot(player1, Player_1.Location, true);
							shootingTimerP1.Start();
							shooting = true;
						}
					}
					// Semi auto - shoot only if this is a new keypress
					else if (!alreadyPressed)
					{
						Shoot(player1, Player_1.Location, true);
					}
				}
				// === Manual Reloads ===
				if (e.KeyCode == Keys.R && !isReloadingP1 && player1CurrentAmmo < player1.MagazineSize )
				{
					_ = ReloadAsync(true); // Player 1 reload
					player1.IsShooting = false; // zastav střelbu

					return;
				}

				if (e.KeyCode == Keys.NumPad0 && !isReloadingP2 && player2CurrentAmmo < player2.MagazineSize )
				{
					_ = ReloadAsync(false); // Player 2 reload
					player2.IsShooting = false; // zastav střelbu
					Form1_KeyUp(this, new KeyEventArgs(Keys.NumPad1));
					return;
				}
				// === SuperPower Activation ===
				if (e.KeyCode == Keys.Q && player1.SuperPower?.CanActivate() == true)
				{
					player1.SuperPower.Activate();
				}

				if (e.KeyCode == Keys.NumPad2 && player2.SuperPower?.CanActivate() == true)
				{
					player2.SuperPower.Activate();
				}



				// === Player 2 ===
				if (e.KeyCode == Keys.NumPad1 && !isReloadingP2)
				{
					if (player2.isFullAuto)
					{
						if (!shootingTimerP2.Enabled)
						{
							Shoot(player2, Player_2.Location, false);
							shootingTimerP2.Start();
							shooting = true;
						}
					}
					else if (!alreadyPressed)
					{
						Shoot(player2, Player_2.Location, false);
					}
				}
			}
		}


		
		private void Form1_KeyUp(object sender, KeyEventArgs e)
		{
			keysPressed.Remove(e.KeyCode);

			if (e.KeyCode == Keys.Space)
			{
				shootingTimerP1.Stop();
				player1AccurateShots = 0;
				shooting = false;
				player1.IsShooting = false; // zastav střelbu
			}

			if (e.KeyCode == Keys.NumPad1)
			{
				shootingTimerP2.Stop();
				player2AccurateShots = 0;
				shooting = false;
				player2.IsShooting = false; // zastav střelbu
			}
		}



		private void GameTimer_Tick(object sender, EventArgs e)
		{
			if (player2 == null && player1 == null) return;
			// === Posun střel ===
			List<Bullet> bulletsToRemove = new List<Bullet>();

			foreach (var b in bullets.ToList()) // snapshot seznamu
			{
				b.Position = new PointF(b.Position.X + b.Velocity.X, b.Position.Y + b.Velocity.Y);
				b.Lifetime -= gameTimer.Interval;

				if (b.Lifetime <= 1 ||
					b.Position.X < 0 || b.Position.X > this.Width ||
					b.Position.Y < 0 || b.Position.Y > this.Height)
				{
					bulletsToRemove.Add(b);
					continue;
				}

				CheckBulletCollision(b); // může způsobit odstranění střely (nutné mít odděleně)
			}

			foreach (var b in bulletsToRemove)
			{
				bullets.Remove(b);
			}

			Invalidate();  // aby se překreslily kulky

			// === Zdraví a portréty ===
			if (player1 != null)
			{
				UpdateHealthDisplay(lblP1HP, player1.Health, player1.Health);
				portrait1.Image = player1.Portrait;
			}

			if (player2 != null)
			{
				UpdateHealthDisplay(lblP2HP, player2.Health, player2.Health);
				portrait2.Image = player2.Portrait;
			}

			// === Souřadnice ===
			if (lblCoordinates != null)
			{
				lblCoordinates.Text = $"Player 1: ({Player_1.Left}, {Player_1.Top}) | " +
									  $"Player 2: ({Player_2.Left}, {Player_2.Top})";
			}


			// === Cooldown bary ===
			UpdateCooldownBar(player1, cooldownBarP1);
			UpdateCooldownBar(player2, cooldownBarP2);
			UpdateShieldBarForPlayer(player1);
			UpdateShieldBarForPlayer(player2);
			UpdateNowallBarForPlayer(player1);
			UpdateNowallBarForPlayer(player2);
			// === Střelecké zvuky ===
			// Player 1
			// Player 1
			
			if(isReloadingP1 && player1 != null)
			{
				player1.IsShooting = false;
			}
			if (isReloadingP2 && player2 != null)
			{
				player2.IsShooting = false;
			}
			if (player1 != null)
			{
				if (player1.IsShooting && !isReloadingP1)
				{
					player1SoundPlayer.StartLoop(player1.ShootingSoundBytes);
				}
				else
				{
					player1SoundPlayer.StopLoopAfterCurrent();
				}
			}

			// Player 2
			if (player2 != null)
			{
				if (player2.IsShooting && !isReloadingP2)
				{
					player2SoundPlayer.StartLoop(player2.ShootingSoundBytes);
				}
				else
				{
					player2SoundPlayer.StopLoopAfterCurrent();
				}
			}



		}
		private void UpdateShieldBars()
		{
			UpdateShieldBarForPlayer(player1, shieldBar1);
			UpdateShieldBarForPlayer(player2, shieldBar2);
		}
		private void UpdateNowallBars()
		{
			UpdateNoWallBarForPlayer(player1, nowallBar1);
			UpdateNoWallBarForPlayer(player2, nowallBar2);
		}

		private void UpdateShieldBarForPlayer(Character player, ProgressBar bar)
		{
			if (player.IsShieldActive)
			{
				bar.Visible = true;
				bar.Value = Math.Max(0, Math.Min(bar.Maximum, player.ShieldHealth));
			}
			else
			{
				bar.Visible = false;
			}
		}
		private void UpdateNoWallBarForPlayer(Character player, ProgressBar bar)
		{
			if (player.IsNoWallActive)
			{
				bar.Visible = true;
				bar.Value = Math.Max(0, Math.Min(bar.Maximum, player.NoWallHealth));
			}
			else
			{
				bar.Visible = false;
			}
		}


		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);

			if (player1 == null || player2 == null)
				return;

			// === BULLETS ===
			foreach (Bullet b in bullets)
			{
				int width = b.Owner.BulletSize;
				int height = (int)(b.Owner.BulletSize * 0.4f);
				RectangleF bulletRect = new RectangleF(b.Position.X, b.Position.Y, width, height);
				e.Graphics.FillRectangle(Brushes.Red, bulletRect);
			}

			// === WALLS ===
			foreach (var path in obstaclePolygons)
			{
				PointF[] points = path.Select(p => new PointF(p.X, p.Y)).ToArray();
				e.Graphics.FillPolygon(Brushes.Gray, points);
			}


			// === EXPLOSIONS ===
			foreach (var explosion in activeExplosions.ToList())
			{
				double elapsed = (DateTime.Now - explosion.StartTime).TotalMilliseconds;

				if (elapsed > explosion.DurationMs)
				{
					activeExplosions.Remove(explosion);
					continue;
				}

				int alpha = (int)(255 * (1 - elapsed / explosion.DurationMs));
				alpha = Math.Clamp(alpha, 0, 255);

				using (Brush b = new SolidBrush(Color.FromArgb(alpha, Color.OrangeRed)))
				{
					e.Graphics.FillEllipse(
						b,
						explosion.Center.X - explosion.Radius,
						explosion.Center.Y - explosion.Radius,
						explosion.Radius * 2,
						explosion.Radius * 2
					);
				}
			}

			// === DAMAGE POPUPS ===
			var now = DateTime.Now;
			var expired = new List<DamagePopup>();

			foreach (var popup in damagePopups)
			{
				float elapsed = (float)(now - popup.StartTime).TotalSeconds;
				if (elapsed > popup.Lifetime)
				{
					expired.Add(popup);
					continue;
				}

				// Float upwards
				float offsetY = -30f * (elapsed / popup.Lifetime);
				PointF drawPos = new PointF(popup.Position.X, popup.Position.Y + offsetY * 20);

				// Fade out
				int alpha = (int)(255 * (1 - elapsed / popup.Lifetime));
				using (Brush brush = new SolidBrush(Color.FromArgb(alpha, popup.Color)))
				using (Font font = new Font("Arial", 12, FontStyle.Bold))
				{
					e.Graphics.DrawString(popup.Text, font, brush, drawPos);
				}
			}

			foreach (var popup in expired)
				damagePopups.Remove(popup);

			// === SHIELD EFFECT ===
			if (player1.SuperPower is ShieldPower shield1 && shield1.IsActive)
				DrawShield(e.Graphics, Player_1);

			if (player2.SuperPower is ShieldPower shield2 && shield2.IsActive)
				DrawShield(e.Graphics, Player_2);

			// === NOWALL EFFECT ===
			if (player1.SuperPower is noWallPower nowall1 && nowall1.IsActive)
				DrawNowallEffect(e.Graphics, Player_1);

			if (player2.SuperPower is noWallPower nowall2 && nowall2.IsActive)
				DrawNowallEffect(e.Graphics, Player_2);
		}

		private void DrawShield(Graphics g, Control playerControl)
		{
			int padding = 20; // space between the player and the shield
			Rectangle shieldRect = new Rectangle(
				playerControl.Left - padding,
				playerControl.Top - padding,
				playerControl.Width + padding * 2,
				playerControl.Height + padding * 2
			);

			using (Brush shieldBrush = new SolidBrush(Color.FromArgb(80, Color.LightBlue)))
			using (Pen shieldPen = new Pen(Color.Cyan, 2))
			{
				g.FillEllipse(shieldBrush, shieldRect);
				g.DrawEllipse(shieldPen, shieldRect);
			}
		}
		private void DrawNowallEffect(Graphics g, Control playerControl)
		{
			int padding = 20; // space between the player and the shield
			Rectangle shieldRect = new Rectangle(
								playerControl.Left - padding,
												playerControl.Top - padding,
																playerControl.Width + padding * 2,
																				playerControl.Height + padding * 2
																							);

			using (Brush shieldBrush = new SolidBrush(Color.FromArgb(80, Color.Purple)))
			using (Pen shieldPen = new Pen(Color.Magenta, 2))
			{
				g.FillEllipse(shieldBrush, shieldRect);
				g.DrawEllipse(shieldPen, shieldRect);
			}
		}


		private bool IsPlayerMoving(bool isPlayer1)
		{
			if (isPlayer1)
				return keysPressed.Contains(Keys.A) || keysPressed.Contains(Keys.D) ||
					   keysPressed.Contains(Keys.W) || keysPressed.Contains(Keys.S);
			else
				return keysPressed.Contains(Keys.Left) || keysPressed.Contains(Keys.Right) ||
					   keysPressed.Contains(Keys.Up) || keysPressed.Contains(Keys.Down);
		}
		private void CheckBulletCollision(Bullet bullet)
		{
			int width = bullet.Owner.BulletSize;
			int height = (int)(bullet.Owner.BulletSize * 0.4f);

			RectangleF bulletRect = new RectangleF(bullet.Position.X, bullet.Position.Y, width, height);

			// Kolize s cílem
			Character shooter = bullet.Owner;
			Character target = shooter == player1 ? player2 : player1;
			PictureBox targetControl = shooter == player1 ? Player_2 : Player_1;

			RectangleF targetRect = new RectangleF(
				targetControl.Left,
				targetControl.Top,
				targetControl.Width,
				targetControl.Height
			);

			if (bulletRect.IntersectsWith(targetRect) )
			{
				if (bullet.IsExplosive)
				{
					int totalDamage = shooter.Damage + shooter.ExplosiveDamage;
					OnBulletHit(target, shooter, totalDamage);
					Explode(bullet);
				}
				else
				{
					OnBulletHit(target, shooter, shooter.Damage);
					bullets.Remove(bullet);
				}
				return;
			}

			// Kolize se zdmi
			foreach (Path64 wall in obstaclePolygons)
			{
				if (IntersectsPolygon(wall, bulletRect))
				{
					if (bullet.Owner.IsNoWallActive)
					{
						DestroyPartOfWallTiny(wall,
							new Point((int)bullet.Position.X, (int)bullet.Position.Y),
							bullet.Owner.BulletSize,
							bullet.Velocity
						);
						return;
					}
					else if (bullet.IsWallExplosive)
					{
						DestroyPartOfWall(wall, new Point((int)bullet.Position.X, (int)bullet.Position.Y));
						Explode(bullet);
					}
					else if (bullet.IsExplosive)
					{
						Explode(bullet);
					}
					else
					{
						bullets.Remove(bullet);
					}
					return;
				}


			}

			// Mimo mapu?
			if (!ClientRectangle.Contains(Point.Round(bullet.Position)))
			{
				if (bullet.IsExplosive)
					Explode(bullet);
				else
					bullets.Remove(bullet);
			}

		}

		private bool IntersectsPolygon(Path64 polygon, RectangleF rect)
		{
			Path64 rectPath = new Path64
			{
				new Point64((long)rect.Left, (long)rect.Top),
				new Point64((long)rect.Right, (long)rect.Top),
				new Point64((long)rect.Right, (long)rect.Bottom),
				new Point64((long)rect.Left, (long)rect.Bottom)
			};

			Clipper64 clipper = new Clipper64();
			clipper.AddSubject(new Paths64 { polygon });
			clipper.AddClip(new Paths64 { rectPath });

			Paths64 result = new Paths64();
			clipper.Execute(ClipType.Intersection, FillRule.NonZero, result);

			return result.Count > 0;
		}


		private void Explode(Bullet bullet)
		{
			var shooter = bullet.Owner;
			PointF explosionCenter = bullet.Position;
			int range = shooter.ExplosiveRange;
			int maxDamage = shooter.ExplosiveDamage;

			// Odeber kulku
			bullets.Remove(bullet);

			// Zvuk
			if (maxDamage > 150)
				SoundHelper.PlayOnce(explosionBigSound, 1.0f);
			else
				SoundHelper.PlayOnce(explosionSmallSound, Math.Clamp(maxDamage / 150f, 0.2f, 0.9f));

			// Výbuch pro grafiku
			activeExplosions.Add(new Explosion()
			{
				Center = explosionCenter,
				Radius = range,
				StartTime = DateTime.Now
			});

			// Poškození hráčů
			Character[] targets = new Character[] { player1, player2 };
			PictureBox[] controls = new PictureBox[] { Player_1, Player_2 };

			for (int i = 0; i < targets.Length; i++)
			{
				Character target = targets[i];
				PictureBox targetControl = controls[i];

				PointF targetCenter = new PointF(
					targetControl.Left + targetControl.Width / 2,
					targetControl.Top + targetControl.Height / 2
				);

				float dx = targetCenter.X - explosionCenter.X;
				float dy = targetCenter.Y - explosionCenter.Y;
				float distance = (float)Math.Sqrt(dx * dx + dy * dy);

				if (distance <= range)
				{
					bool blocked = false;

					// Vytvoř polygonový line segment mezi výbuchem a hráčem
					// Reprezentujeme ho jako tenký obdélník (šířka 1px)
					Path64 ray = new Path64
					{
						new Point64((long)explosionCenter.X, (long)explosionCenter.Y),
						new Point64((long)targetCenter.X, (long)targetCenter.Y)
					};

					 
					foreach (var wall in obstaclePolygons)
					{
						if (LineIntersectsPolygon(explosionCenter, targetCenter, wall))
						{
							blocked = true;
							break;
						}
					}


					if (!blocked)
					{
						float percent = 1f - (distance / range);
						percent = Math.Max(percent, 0.0f);
						float scaled = 0.3f + percent * 0.7f;

						int damage = (int)(maxDamage * scaled);
						OnBulletHit(target, shooter, damage);
					}
				}
			}

			// Vykreslení výbuchu
			Rectangle explosionArea = new Rectangle(
				(int)(explosionCenter.X - range),
				(int)(explosionCenter.Y - range),
				range * 2,
				range * 2
			);

			using (Graphics g = CreateGraphics())
			using (Brush b = new SolidBrush(Color.FromArgb(100, Color.OrangeRed)))
			{
				g.FillEllipse(b, explosionArea);
			}

			Console.WriteLine(" Výbuch! (center: " + explosionCenter + ")");
		}

		byte[] explosionBigSound;
		byte[] explosionSmallSound;
		private byte[] explosionWallPierceSound;

		private bool LineIntersectsPolygon(PointF p1, PointF p2, Path64 polygon)
		{
			for (int i = 0; i < polygon.Count; i++)
			{
				Point64 a = polygon[i];
				Point64 b = polygon[(i + 1) % polygon.Count];

				PointF aF = new PointF(a.X, a.Y);
				PointF bF = new PointF(b.X, b.Y);

				if (DoLinesIntersect(p1, p2, aF, bF))
					return true;
			}
			return false;
		}

		private bool DoLinesIntersect(PointF p1, PointF p2, PointF q1, PointF q2)
		{
			float o1 = Orientation(p1, p2, q1);
			float o2 = Orientation(p1, p2, q2);
			float o3 = Orientation(q1, q2, p1);
			float o4 = Orientation(q1, q2, p2);

			if (o1 != o2 && o3 != o4)
				return true;

			return false;
		}

		private int Orientation(PointF a, PointF b, PointF c)
		{
			float val = (b.Y - a.Y) * (c.X - b.X) - (b.X - a.X) * (c.Y - b.Y);
			if (val == 0) return 0; // kolineární
			return (val > 0) ? 1 : 2; // 1: clockwise, 2: counterclockwise
		}




		private bool LineIntersectsRect(PointF p1, PointF p2, Rectangle rect)
		{
			return LineIntersectsLine(p1, p2, new PointF(rect.Left, rect.Top), new PointF(rect.Right, rect.Top)) ||
				   LineIntersectsLine(p1, p2, new PointF(rect.Right, rect.Top), new PointF(rect.Right, rect.Bottom)) ||
				   LineIntersectsLine(p1, p2, new PointF(rect.Right, rect.Bottom), new PointF(rect.Left, rect.Bottom)) ||
				   LineIntersectsLine(p1, p2, new PointF(rect.Left, rect.Bottom), new PointF(rect.Left, rect.Top));
		}

		private bool LineIntersectsLine(PointF a1, PointF a2, PointF b1, PointF b2)
		{
			float denominator = ((a2.X - a1.X) * (b2.Y - b1.Y)) - ((a2.Y - a1.Y) * (b2.X - b1.X));

			if (denominator == 0)
				return false;

			float ua = ((b2.X - b1.X) * (a1.Y - b1.Y) - (b2.Y - b1.Y) * (a1.X - b1.X)) / denominator;
			float ub = ((a2.X - a1.X) * (a1.Y - b1.Y) - (a2.Y - a1.Y) * (a1.X - b1.X)) / denominator;

			return (ua >= 0 && ua <= 1) && (ub >= 0 && ub <= 1);
		}

		private async void Shoot(Character shooter, Point startPosition, bool isPlayer1)
		{
			// Kontrola přebíjení
			if ((isPlayer1 && isReloadingP1) || (!isPlayer1 && isReloadingP2))
				return;

			DateTime now = DateTime.Now;
			TimeSpan fireInterval = GetFireRateInterval(shooter);

			if (isPlayer1)
			{
				if ((now - lastShootTimeP1) < fireInterval)
					return;
				lastShootTimeP1 = now;
			}
			else
			{
				if ((now - lastShootTimeP2) < fireInterval)
					return;
				lastShootTimeP2 = now;
			}

			// Munice
			int currentAmmo = isPlayer1 ? player1CurrentAmmo : player2CurrentAmmo;
			if (currentAmmo <= 0)
			{
				await ReloadAsync(isPlayer1);
				return;
			}

			// Odečti munici
			if (isPlayer1) player1CurrentAmmo--;
			else player2CurrentAmmo--;

			UpdateMagazineBars();

			// Pozice střelce a nepřítele
			Point enemyPos = isPlayer1
				? new Point(Player_2.Left + Player_2.Width / 2, Player_2.Top + Player_2.Height / 2)
				: new Point(Player_1.Left + Player_1.Width / 2, Player_1.Top + Player_1.Height / 2);

			Point shooterCenter = new Point(startPosition.X + shooter.Width / 2, startPosition.Y + shooter.Height / 2);
			float dx = enemyPos.X - shooterCenter.X;
			float dy = enemyPos.Y - shooterCenter.Y;
			double baseAngle = Math.Atan2(dy, dx);

			bool isMoving = IsPlayerMoving(isPlayer1);
			Random rand = new Random();

			int maxSpreadDegrees = 5;
			double spreadFactor = isMoving ? 4.0 : 1.0;
			int precision = Math.Max(1, shooter.Precision);
			double spreadDegrees = maxSpreadDegrees * (100 - precision) / 150.0 * spreadFactor;

			// === Shotgun mód ===
			if (shooter.isShotgun)
			{
				int pelletCount = shooter.PelletCount;
				double shotgunBaseSpreadDegrees = shooter.Precision;
				double shotgunSpreadDegrees = shotgunBaseSpreadDegrees * (1 + shooter.Precision) / 100.0;

				for (int i = 0; i < pelletCount; i++)
				{
					double angleOffset = (rand.NextDouble() * 2 - 1) * (shotgunSpreadDegrees * Math.PI / 180.0);
					double finalAngle = baseAngle + angleOffset;

					float velocityX = (float)(Math.Cos(finalAngle) * shooter.BulletSpeed);
					float velocityY = (float)(Math.Sin(finalAngle) * shooter.BulletSpeed);

					bullets.Add(new Bullet
					{
						Position = new PointF(shooterCenter.X, shooterCenter.Y),
						Velocity = new PointF(velocityX, velocityY),
						Lifetime = bulletMaxLifetime,
						Owner = shooter
					});
				}
			}
			else
			{
				// Normální střela
				double spreadOffset = (rand.NextDouble() * 2 - 1) * (spreadDegrees * Math.PI / 180.0);
				double finalAngle = baseAngle + spreadOffset;

				float velocityX = (float)(Math.Cos(finalAngle) * shooter.BulletSpeed);
				float velocityY = (float)(Math.Sin(finalAngle) * shooter.BulletSpeed);

				bullets.Add(new Bullet
				{
					Position = new PointF(shooterCenter.X, shooterCenter.Y),
					Velocity = new PointF(velocityX, velocityY),
					Lifetime = bulletMaxLifetime,
					Owner = shooter
				});
			}

			// === Zvuk střelby ===
			if (isPlayer1)
				player1.IsShooting = true;
			else
				player2.IsShooting = true;

			// Pokud došla munice, přebij
			if ((isPlayer1 && player1CurrentAmmo == 0 && !isReloadingP1) ||
				(!isPlayer1 && player2CurrentAmmo == 0 && !isReloadingP2))
			{
				await ReloadAsync(isPlayer1);
				player1.IsShooting = false;
				player1.IsShooting = false; // zastav střelbu

			}
			
		}
		

		private bool isGameOver = false;

		private void OnBulletHit(Character target, Character shooter, int damage)
		{
			if (isGameOver || target == null) return;

			// 1. Absorpce štítem
			if (target.IsShieldActive && target.ShieldHealth > 0)
			{
				int absorbed = Math.Min(target.ShieldHealth, damage);
				target.ShieldHealth -= absorbed;
				damage -= absorbed;

				ShowDamagePopup(target, absorbed, Color.Cyan); // modrý popup pro štít

				if (target.ShieldHealth <= 0)
				{
					target.IsShieldActive = false;
					target.ShieldHealth = 0;
				}

				// === Aktualizuj progress bar ===
				UpdateShieldBarForPlayer(target);

				if (damage <= 0)
					return; // všechno absorbováno
			}

			// 2. Poškození zdraví
			target.Health -= damage;
			ShowDamagePopup(target, damage, Color.Red); // červený popup pro HP

			// 3. Detekce smrti
			if (target.Health <= 0)
			{
				isGameOver = true;

				string result;
				if (target == shooter)
				{
					string playerName = shooter.Name;
					string playerLabel = shooter == player1 ? "Player 1" : "Player 2";
					result = $"{player1.Name} | Player 1 vs Player 2 | {player2.Name}  Result: {playerName} ({playerLabel}) died by suicide";
					MessageBox.Show($"{playerName} ({playerLabel}) Died by suicide!");
				}
				else
				{
					string winnerName = shooter.Name;
					string winnerPlayer = shooter == player1 ? "Player 1" : "Player 2";
					result = $"{player1.Name} | Player 1 vs Player 2 | {player2.Name}  Winner: {winnerName} ({winnerPlayer})";
					MessageBox.Show($"{winnerName} ({winnerPlayer}) WON!");
				}
				player1SoundPlayer.Dispose();
				player2SoundPlayer.Dispose();

				SaveResultToFile(result);
				AskForRestartChoice();
			}
			if (isGameOver)
			{
				
			}
		}
		private void ShowDamagePopup(Character target, int amount, Color color)
		{
			PictureBox targetControl = target == player1 ? Player_1 : Player_2;
			PointF popupPos = new PointF(
				targetControl.Left + targetControl.Width / 2f,
				targetControl.Top - 10
			);

			damagePopups.Add(new DamagePopup
			{
				Position = popupPos,
				Text = amount.ToString(),
				StartTime = DateTime.Now,
				Color = color
			});
		}

		private void AskForRestartChoice()
		{
			
			Form1_KeyUp(this, new KeyEventArgs(Keys.Space)); // Player 1 fire
			Form1_KeyUp(this, new KeyEventArgs(Keys.W));
			Form1_KeyUp(this, new KeyEventArgs(Keys.A));
			Form1_KeyUp(this, new KeyEventArgs(Keys.S));
			Form1_KeyUp(this, new KeyEventArgs(Keys.D));
			Form1_KeyUp(this, new KeyEventArgs(Keys.Q));     // Player 1 ability left
			Form1_KeyUp(this, new KeyEventArgs(Keys.E));     // Player 1 ability right

			Form1_KeyUp(this, new KeyEventArgs(Keys.Up));
			Form1_KeyUp(this, new KeyEventArgs(Keys.Left));
			Form1_KeyUp(this, new KeyEventArgs(Keys.Down));
			Form1_KeyUp(this, new KeyEventArgs(Keys.Right));
			Form1_KeyUp(this, new KeyEventArgs(Keys.NumPad0)); // Player 2 fire
			Form1_KeyUp(this, new KeyEventArgs(Keys.NumPad1)); // Player 2 ability left
			Form1_KeyUp(this, new KeyEventArgs(Keys.NumPad2)); // Player 2 ability right
			shooting = false;





			var result = MessageBox.Show(
				"What would you like to do?\n\nYes = Restart with same characters\nNo = Choose new characters\nCancel = Exit the game",
				"Game Over", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

			switch (result)
			{
				case DialogResult.Yes:
					shooting = false;
					RestartWithSameCharacters();
					Form1_KeyUp(this, new KeyEventArgs(Keys.Space));
					Form1_KeyUp(this, new KeyEventArgs(Keys.NumPad1));
					break;
				case DialogResult.No:
					shooting = false;
					Form1_KeyUp(this, new KeyEventArgs(Keys.Space));
					Form1_KeyUp(this, new KeyEventArgs(Keys.NumPad1));
					RestartWithNewCharacters();
					break;
				case DialogResult.Cancel:
					Application.Exit();
					break;
			}
		}

		private void RestartWithSameCharacters()
		{
			
			Form1_KeyUp(this, new KeyEventArgs(Keys.Space));
			Form1_KeyUp(this, new KeyEventArgs(Keys.NumPad1));
			Task.Delay(1000).Wait(); // Optional delay before resetting
						 // Resetuj pozice hráčů, statistiky a UI
			ResetGameState();
			player1.Health = player1.MaxHealth; // Reset health to initial value
			player2.Health = player2.MaxHealth; // Reset health to initial value
			// Spusť znovu hru s aktuálními postavami
			BtnStart_Click(null, null);
			InitObstacles();
		}
		private void RestartWithNewCharacters()
		{
			Form1_KeyUp(this, new KeyEventArgs(Keys.Space));
			Form1_KeyUp(this, new KeyEventArgs(Keys.NumPad1));
			Task.Delay(1000).Wait(); // Optional delay before resetting
			// Skryj herní prvky a ukaž výběr postav
			HideGameUI();
			ShowCharacterSelection();
			ResetGameState();

			player1 = null;
			player2 = null;
		}
		private void ResetGameState()
		{
			isGameOver = false;

			// Reset characters (you need to implement Reset method on Character)
			player1?.Reset();
			player2?.Reset();
		
			// Reset ammo
			player1CurrentAmmo = player1?.MagazineSize ?? 0;
			player2CurrentAmmo = player2?.MagazineSize ?? 0;

			lblP1HP.Text = $"HP: {player1?.Health}";
			lblP2HP.Text = $"HP: {player2?.Health}";

			// Reset player positions
			Player_1.Location = new Point(50, 50);
			Player_2.Location = new Point(1850, 1000);

			UpdateMagazineBars();

			// Optionally reset cooldowns, powerups, projectiles, etc.
			bullets.Clear();
			foreach (var bullet in Controls.OfType<PictureBox>().Where(p => p.Tag?.ToString() == "Bullet").ToList())
				Controls.Remove(bullet);
		}

		private void HideGameUI()
		{
			Player_1.Visible = false;
			Player_2.Visible = false;
			statusBar.Visible = false;
			lblPlayer1Name.Visible = false;
			lblPlayer2Name.Visible = false;
		}

		private void ShowCharacterSelection()
		{
			listBoxCharacters.Visible = true;
			btnSelectPlayer1.Visible = true;
			btnSelectPlayer2.Visible = true;
			pictureBoxCharacter.Visible = true;
			lblPlayer1.Visible = true;
			lblPlayer2.Visible = true;
			btnStart.Visible = true;
			lblDescription.Visible = true;
			btnShowResults.Visible = true;
			statusBar.Visible = true;
		}




		




		private void SaveResultToFile(string result)
		{
			string filePath = "results.txt";
			try
			{
				using (StreamWriter writer = new StreamWriter(filePath, append: true))
				{
					writer.WriteLine(result);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("Chyba při ukládání výsledku: " + ex.Message);
			}
		}

		// Funkce, která převede firerate (int ms) na TimeSpan
		private TimeSpan GetFireRateInterval(Character character)
		{
			return TimeSpan.FromMilliseconds(character.Firerate);
		}

		private void UpdateMagazineBars()
		{
			if (player1 != null)
			{
				int p1AmmoPercent = (int)((float)player1CurrentAmmo / player1.MagazineSize * 100);
				magazineBarP1.Value = Math.Max(0, Math.Min(100, p1AmmoPercent));
				lblP1Ammo.Text = $"Ammo: {player1CurrentAmmo} / {player1.MagazineSize}";
				lblP1Ammo.ForeColor = GetAmmoColor(player1CurrentAmmo, player1.MagazineSize);
				magazineBarP1.ForeColor = GetAmmoColor(player1CurrentAmmo, player1.MagazineSize); // pokud ProgressBar to podporuje
			}

			if (player2 != null)
			{
				int p2AmmoPercent = (int)((float)player2CurrentAmmo / player2.MagazineSize * 100);
				magazineBarP2.Value = Math.Max(0, Math.Min(100, p2AmmoPercent));
				lblP2Ammo.Text = $"Ammo: {player2CurrentAmmo} / {player2.MagazineSize}";
				lblP2Ammo.ForeColor = GetAmmoColor(player2CurrentAmmo, player2.MagazineSize);
				magazineBarP2.ForeColor = GetAmmoColor(player2CurrentAmmo, player2.MagazineSize);
			}
		}

		// Přidej prosím taky tuto pomocnou metodu, kterou voláš výše:

		private Color GetAmmoColor(int currentAmmo, int maxAmmo)
		{
			if (maxAmmo == 0) return Color.Gray;

			float percent = (float)currentAmmo / maxAmmo;

			if (percent >= 0.9f)
				return Color.DarkGreen;
			else if (percent >= 0.7f)
				return Color.LightGreen;
			else if (percent >= 0.5f)
				return Color.Yellow;
			else if (percent >= 0.3f)
				return Color.Orange;
			else
				return Color.Red;
		}

		private void MovePlayer(PictureBox player, int dx, int dy)
		{
			Point newPos = new Point(player.Left + dx, player.Top + dy);

			// Clamp to window and prevent entry into status bar
			newPos.X = Math.Max(0, Math.Min(ClientSize.Width - player.Width, newPos.X));
			newPos.Y = Math.Max(0, Math.Min(statusBar.Top - player.Height, newPos.Y));

			// Nový obdélník hráče po pohybu
			RectangleF futureBounds = new RectangleF(newPos, player.Size);

			// Kontrola kolize s překážkami (polygony)
			foreach (Path64 polygon in obstaclePolygons)
			{
				if (IntersectsPolygon(polygon, futureBounds))
					return; // Kolize - nepohybuj hráčem
			}

			// Žádná kolize - pohnout
			player.Location = newPos;
		}


		private void InitObstacles()
		{
			obstaclePolygons.Clear(); // smaž staré překážky pokud existují

			AddRectObstacle(350, 300, 300, 40);
			AddRectObstacle(300, 600, 40, 300);
			AddRectObstacle(600, 700, 250, 40);
			AddRectObstacle(700, 850, 40, 150);

			AddRectObstacle(900, 200, 40, 600);
			AddRectObstacle(700, 700, 500, 40);
			AddRectObstacle(1200, 650, 40, 350);

			AddRectObstacle(1400, 350, 350, 40);
			AddRectObstacle(1700, 600, 40, 300);
			AddRectObstacle(1300, 750, 300, 40);
			AddRectObstacle(1350, 850, 40, 150);
		}

		// Pomocná metoda pro přidání obdélníkové překážky do obstaclePolygons
		private void AddRectObstacle(int x, int y, int width, int height)
		{
			Path64 rect = new Path64
	{
		new Point64(x, y),
		new Point64(x + width, y),
		new Point64(x + width, y + height),
		new Point64(x, y + height)
	};
			obstaclePolygons.Add(rect);
		}

		public interface ICharacterPlugin
		{
			Character GetCharacter();
		}
		private void LoadCharacterPlugins()
		{
			string pluginsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins");

			if (!Directory.Exists(pluginsPath))
				Directory.CreateDirectory(pluginsPath);

			var pluginFiles = Directory.GetFiles(pluginsPath, "*.dll");

			foreach (var file in pluginFiles)
			{
				try
				{
					// Načtení sestavení (pluginu)
					var assembly = Assembly.LoadFrom(file);

					// Nalezení tříd implementujících ICharacterPlugin
					var types = assembly.GetTypes()
						.Where(t => typeof(ICharacterPlugin).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

					foreach (var type in types)
					{
						ICharacterPlugin plugin = (ICharacterPlugin)Activator.CreateInstance(type);
						Character character = plugin.GetCharacter();

						// Přidání do seznamu
						allCharacters.Add(character);

						MessageBox.Show($"Načten plugin: {character.Name}");
					}
				}
				catch (ReflectionTypeLoadException rex)
				{
					// Zobrazí podrobné chyby z DLL (např. špatné resource jméno apod.)
					string loaderErrors = string.Join("\n", rex.LoaderExceptions.Select(ex => ex.Message));
					MessageBox.Show($"Chyba při načítání pluginu {file}:\n{loaderErrors}");
				}
				catch (Exception ex)
				{
					MessageBox.Show($"Chyba při načítání pluginu {file}:\n{ex.Message}");
				}
			}
		}


		private void LoadCharacters()
		{
			allCharacters.Clear();

			// === Vestavěné postavy ===
				var matej = new Character(
				"Matěj",                   // Name
				1000,                      // MaxHealth
				1000,                      // Health
				90,                        // Width
				180,                       // Height
				120,                       // Weight
				1,                         // Firerate
				22,                        // Damage
				40,                        // Precision
				28,                        // MagazineSize
				1200,                      // ReloadTime
				25,                        // BulletSpeed
				10,                        // BulletSize
				true,                      // isExplosive
				true,                      // isShotgun
				false,                     // isFullAuto
				0,                         // PelletCount
				50,                        // ExplosiveRange
				20,                        // ExplosiveDamage
				550,                       // DashDamage
				"Weapon:Extreme rapid-fire SMG,\r\nSpeed: High\r\nSize: Medium\r\nHP: low",
				B1_Apps.Apps.Game.Properties.GameResources.Matěj);
				matej.SuperPowerFactory = (form) => new DashPower(matej, form);
				allCharacters.Add(matej);

				var hlousek = new Character(
				"Vousek",                  // Name
				5500,                      // MaxHealth
				5500,                      // Health
				150,                       // Width
				160,                       // Height
				290,                       // Weight
				45,                        // Firerate
				55,                        // Damage
				20,                        // Precision
				180,                       // MagazineSize
				6000,                      // ReloadTime
				33,                        // BulletSpeed
				20,                        // BulletSize
				false,                     // isExplosive
				true,                      // isShotgun
				false,                     // isFullAuto
				0,                         // PelletCount
				300,                       // ExplosiveRange
				25,                        // ExplosiveDamage
				0,                         // DashDamage
				"Weapon: LMG\r\nSpeed: Very low\r\nSize: Chunky\r\nHP: Very Very High",
				B1_Apps.Apps.Game.Properties.GameResources.Hlousek);
				hlousek.SuperPowerFactory = (form) => new WallPierce(hlousek, form);
				allCharacters.Add(hlousek);

			var matejus = new Character(
				"Matějská",                // Name
				1200,                      // MaxHealth
				1200,                      // Health
				90,                        // Width
				170,                       // Height
				150,                       // Weight
				1500,                      // Firerate
				10,                        // Damage
				20,                        // Precision
				12,                        // MagazineSize
				2500,                      // ReloadTime
				30,                        // BulletSpeed
				15,                        // BulletSize
				false,                     // isExplosive
				false,                     // isShotgun
				true,                      // isFullAuto
				80,                        // PelletCount
				100,                       // ExplosiveRange
				30,                        // ExplosiveDamage
				350,                       // DashDamage
				"Weapon: Quick short barrel Shotgun\r\nSpeed: Medium\r\nSize: Medium\r\nHP: Medium",
				B1_Apps.Apps.Game.Properties.GameResources.Matejus);
				matejus.SuperPowerFactory = (form) => new DashPower(matejus, form);
				allCharacters.Add(matejus);

			var zelba = new Character(
				"Zelda",                   // Name
				500,                       // MaxHealth
				500,                       // Health
				110,                       // Width
				175,                       // Height
				100,                       // Weight
				1,                         // Firerate
				400,                       // Damage
				95,                        // Precision
				1,                         // MagazineSize
				4000,                      // ReloadTime
				40,                        // BulletSpeed
				35,                        // BulletSize
				true,                      // isExplosive
				false,                     // isShotgun
				false,                     // isFullAuto
				0,                         // PelletCount
				400,                       // ExplosiveRange
				400,                       // ExplosiveDamage
				0,                         // DashDamage
				"Weapon: ROCKET LAUNCHER!!\r\nSpeed: High\r\nSize: Medium\r\nHP: Very Low",
				B1_Apps.Apps.Game.Properties.GameResources.Zelba);
				zelba.SuperPowerFactory = (form) => new WallPierce(zelba, form);
				allCharacters.Add(zelba);

				var podzimek = new Character(
				"Podzim",                  // Name
				1300,                      // MaxHealth
				1300,                      // Health
				120,                       // Width
				180,                       // Height
				110,                       // Weight
				0,                         // Firerate
				50,                        // Damage
				90,                        // Precision
				20,                        // MagazineSize
				3000,                      // ReloadTime
				40,                        // BulletSpeed
				25,                        // BulletSize
				true,                      // isExplosive
				false,                     // isShotgun
				false,                     // isFullAuto
				0,                         // PelletCount
				50,                        // ExplosiveRange
				50,                        // ExplosiveDamage
				400,                       // DashDamage
				"Weapon: Explosive Pistol\r\nSpeed: Medium\r\nSize: Medium\r\nHP: Medium",
				B1_Apps.Apps.Game.Properties.GameResources.podzimek);
				podzimek.SuperPowerFactory = (form) => new DashPower(podzimek, form);
				allCharacters.Add(podzimek);

				var ondraa = new Character(
				"Ondra",                   // Name
				700,                       // MaxHealth
				1200,                      // Health
				125,                       // Width
				180,                       // Height
				120,                       // Weight
				25,                        // Firerate
				35,                        // Damage
				60,                        // Precision
				60,                        // MagazineSize
				3200,                      // ReloadTime
				35,                        // BulletSpeed
				10,                        // BulletSize
				false,                     // isExplosive
				true,                      // isShotgun
				false,                     // isFullAuto
				0,                         // PelletCount
				50,                        // ExplosiveRange
				20,                        // ExplosiveDamage
				400,                       // DashDamage
				"Weapon: Magazine SMG\r\nSpeed: High\r\nSize: Medium\r\nHP: Low",
				B1_Apps.Apps.Game.Properties.GameResources.Ondraaa);
				ondraa.SuperPowerFactory = (form) => new noWallPower(ondraa, form);
				allCharacters.Add(ondraa);

				var penc = new Character(
				"Pec",                     // Name
				1000,                      // MaxHealth
				1000,                      // Health
				90,                        // Width
				180,                       // Height
				120,                       // Weight
				1,                         // Firerate
				7,                         // Damage
				20,                        // Precision
				2,                         // MagazineSize
				2000,                      // ReloadTime
				25,                        // BulletSpeed
				10,                        // BulletSize
				false,                     // isExplosive
				false,                     // isShotgun
				true,                      // isFullAuto
				100,                       // PelletCount
				50,                        // ExplosiveRange
				20,                        // ExplosiveDamage
				550,                       // DashDamage
				"Weapon: Double barrel Shotgun\r\nSpeed: High\r\nSize: Medium\r\nHP: Low",
				B1_Apps.Apps.Game.Properties.GameResources.penc);
				penc.SuperPowerFactory = (form) => new ShieldPower(penc, form);
				allCharacters.Add(penc);


			// === Načtení postav z pluginů ===
			LoadCharacterPlugins();

			// === Aktualizace UI ===
			listBoxCharacters.DataSource = null;
			listBoxCharacters.DataSource = allCharacters;
			
		}


		private void ListBoxCharacters_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (listBoxCharacters.SelectedItem is Character selected)
			{
				pictureBoxCharacter.Image = selected.Portrait;
				lblDescription.Text = selected.Description;

			}
			else
			{

			}
		}

		private void BtnSelectPlayer1_Click(object sender, EventArgs e)
		{
			if (listBoxCharacters.SelectedItem is Character selected)
			{
				string baseName = selected.Name;

				player1 = selected.Clone();
				player1.Name = baseName + "_P1";
				player1CurrentAmmo = player1.MagazineSize;
				lblPlayer1.Text = $"Player 1: {player1.Name}";
				player1.AssignPower(this);

				var shootingSound = GetShootingSound(baseName);
				if (shootingSound != null)
				{
					player1.ShootingSound = shootingSound;
					player1.ShootingSoundBytes = ToByteArray(shootingSound);
				}
				else
				{
					MessageBox.Show($"Zvuk pro {baseName} nebyl nalezen!", "Chybějící zvuk", MessageBoxButtons.OK, MessageBoxIcon.Warning);
					player1.ShootingSoundBytes = Array.Empty<byte>(); // nebo nastav defaultní zvuk
				}

				UpdateMagazineBars();
			}
		}

		private void BtnSelectPlayer2_Click(object sender, EventArgs e)
		{
			if (listBoxCharacters.SelectedItem is Character selected)
			{
				string baseName = selected.Name;

				player2 = selected.Clone();
				player2.Name = baseName + "_P2";
				player2CurrentAmmo = player2.MagazineSize;
				lblPlayer2.Text = $"Player 2: {player2.Name}";
				player2.AssignPower(this);

				var shootingSound = GetShootingSound(baseName);
				if (shootingSound != null)
				{
					player2.ShootingSound = shootingSound;
					player2.ShootingSoundBytes = ToByteArray(shootingSound);
				}
				else
				{
					MessageBox.Show($"Zvuk pro {baseName} nebyl nalezen!", "Chybějící zvuk", MessageBoxButtons.OK, MessageBoxIcon.Warning);
					player2.ShootingSoundBytes = Array.Empty<byte>();
				}

				UpdateMagazineBars();
			}
		}


		private byte[] ToByteArray(UnmanagedMemoryStream stream)
		{
			using var ms = new MemoryStream();
			stream.CopyTo(ms);
			return ms.ToArray();
		}

		// Pomocná metoda na přiřazení správného zvuku
		private UnmanagedMemoryStream GetShootingSound(string characterName)
		{
			return characterName switch
			{
				"Pec" => Properties.GameResources.Pec_shootingsound,
				"Matěj" => Properties.GameResources.Matěj_shootingsound,
				"Matějská" => Properties.GameResources.Matějská_shootingsound,
				"Zelda" => Properties.GameResources.Zelda_shootingsound,
				"Vousek" => Properties.GameResources.Vousek_shootingsound,
				"Podzim" => Properties.GameResources.Podzim_shootingsound,
				"Ondra" => Properties.GameResources.Ondra_shootingsound,
				_ => null
			};
		}



		private void BtnStart_Click(object sender, EventArgs e)
		{
			if (player1 == null || player2 == null)
			{
				MessageBox.Show("Please select characters for both players before starting.", "Selection Missing", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}

			// Schovej výběr postav a připrav UI
			listBoxCharacters.Visible = false;
			btnSelectPlayer1.Visible = false;
			btnSelectPlayer2.Visible = false;
			pictureBoxCharacter.Visible = false;
			lblPlayer1.Visible = false;
			lblPlayer2.Visible = false;
			btnStart.Visible = false;
			lblDescription.Visible = false;
			btnShowResults.Visible = false;

			Player_1.Visible = true;
			Player_2.Visible = true;

			Player_1.Image = player1.Portrait;
			Player_2.Image = player2.Portrait;

			Player_1.Size = new Size(player1.Width, player1.Height);
			Player_2.Size = new Size(player2.Width, player2.Height);

			lblPlayer1Name.Visible = true;
			lblPlayer2Name.Visible = true;

			// Přiřaď superpowers znovu (pro jistotu až po UI inicializaci)
			player1.AssignPower(this);
			player2.AssignPower(this);

			// Spusť cooldown timery, pokud je daná superpower podporuje
			if (player1.SuperPower is SuperPower sp1)
				sp1.StartCooldownTimer();

			if (player2.SuperPower is SuperPower sp2)
				sp2.StartCooldownTimer();

			UpdatePlayerLabelPositions();

			portrait1.Image = player1.Portrait;
			lblP1Name.Text = player1.Name;
			lblP1HP.Text = $"HP: {player1.Health}";

			portrait2.Image = player2.Portrait;
			lblP2Name.Text = player2.Name;
			lblP2HP.Text = $"HP: {player2.Health}";

			lblP1Ammo.Text = $"{player1CurrentAmmo} / {player1.MagazineSize}";
			lblP2Ammo.Text = $"{player2CurrentAmmo} / {player2.MagazineSize}";

			shootingTimerP1.Interval = Math.Max(5, player1.Firerate);
			shootingTimerP2.Interval = Math.Max(5, player2.Firerate);
		
			UpdateMagazineBars();

			InitObstacles();
		}

		private void MovementTimer_Tick(object sender, EventArgs e)
		{
			if (!Player_1.Visible || !Player_2.Visible)
				return;

			float baseSpeed = 5f;

			float player1Speed = baseSpeed * (100f / player1.Weight);
			float player2Speed = baseSpeed * (100f / player2.Weight);

			int p1Step = Math.Max(1, (int)Math.Round(player1Speed));
			int p2Step = Math.Max(1, (int)Math.Round(player2Speed));

			// Move Player 1 (WASD)
			if (keysPressed.Contains(Keys.W))
				MovePlayer(Player_1, 0, -p1Step);
			if (keysPressed.Contains(Keys.S))
				MovePlayer(Player_1, 0, p1Step);
			if (keysPressed.Contains(Keys.A))
				MovePlayer(Player_1, -p1Step, 0);
			if (keysPressed.Contains(Keys.D))
				MovePlayer(Player_1, p1Step, 0);

			// Move Player 2 (Arrow keys)
			if (keysPressed.Contains(Keys.Up))
				MovePlayer(Player_2, 0, -p2Step);
			if (keysPressed.Contains(Keys.Down))
				MovePlayer(Player_2, 0, p2Step);
			if (keysPressed.Contains(Keys.Left))
				MovePlayer(Player_2, -p2Step, 0);
			if (keysPressed.Contains(Keys.Right))
				MovePlayer(Player_2, p2Step, 0);


			UpdatePlayerLabelPositions();
		}
		
		private byte[] wallPierceSound;
		private byte[] shieldStartSound;
		private byte[] shieldEndSound;
		private byte[] dashSound;
		public void PlayDashSound()
		{
			SoundHelper.PlayOnce(dashSound, 0.5f); // nastav si hlasitost dle libosti
		}
		public class DashPower : SuperPower
		{
			private Form1 gameForm;
			private bool isDashing = false;
			private int dashSpeed = 400;
			private int dashDurationMs = 100;
			private int cooldownMs = 5000;
			private Timer cooldownTimer;
			private bool hasHitDuringDash = false;

			public bool IsReady { get; private set; } = true;
			public DateTime LastUsed { get; private set; } = DateTime.MinValue;
			public int CooldownMs => cooldownMs;

			public DashPower(Character owner, Form1 form) : base(owner)
			{
				gameForm = form;

				// Timer běží pořád a pravidelně kontroluje cooldown stav
				cooldownTimer = new Timer();
				cooldownTimer.Interval = 100;
				cooldownTimer.Tick += CooldownTimer_Tick;
				cooldownTimer.Start();
			}

			private DateTime? cooldownReadySince = null;

			private void CooldownTimer_Tick(object sender, EventArgs e)
			{
				if (Owner == null || gameForm == null || Owner.SuperPower is not DashPower)
					return;

				TimeSpan elapsed = DateTime.Now - LastUsed;
				double progress = Math.Min(1.0, elapsed.TotalMilliseconds / cooldownMs);
				gameForm.Text = $"Dash Power Cooldown: {progress * 100:F1}%";

				// Aktualizuj progress bar
				if (Owner == gameForm.player1 && gameForm.cooldownBarP1 != null)
					gameForm.cooldownBarP1.Value = (int)(progress * 100);
				else if (Owner == gameForm.player2 && gameForm.cooldownBarP2 != null)
					gameForm.cooldownBarP2.Value = (int)(progress * 100);

				// Pokud cooldown doběhl
				if (elapsed.TotalMilliseconds >= cooldownMs)
				{
					// Zaznamenej čas, kdy cooldown skončil
					if (cooldownReadySince == null)
						cooldownReadySince = DateTime.Now;

					// Po 2 vteřinách od konce cooldownu nastav IsReady = true
					if (!IsReady && (DateTime.Now - cooldownReadySince.Value).TotalMilliseconds >= 2000)
						IsReady = true;
				}
				else
				{
					// Cooldown ještě není dokončen, resetuj čekání
					cooldownReadySince = null;
					IsReady = false;
				}
			}
			public override bool CanActivate()
			{
				return IsReady;
			}
			bool IntersectsPolygon(Path64 polygon, Rectangle rect)
			{
				// 1. Převod Rectangle na Path64
				Path64 rectPath = new Path64
			{
				new Point64(rect.Left, rect.Top),
				new Point64(rect.Right, rect.Top),
				new Point64(rect.Right, rect.Bottom),
				new Point64(rect.Left, rect.Bottom)
			};

				// 2. Použití Clipperu pro zjištění průniku
				Clipper64 clipper = new Clipper64();
				clipper.AddSubject(new Paths64 { polygon });
				clipper.AddClip(new Paths64 { rectPath });

				Paths64 result = new Paths64();
				clipper.Execute(ClipType.Intersection, FillRule.NonZero, result);

				// 3. Pokud výsledek obsahuje aspoň 1 polygon → kolize
				return result.Count > 0;
			}

			public override void Activate()
			{
				
				if (!CanActivate() ) return;

				LastUsed = DateTime.Now;
				IsReady = false;
				isDashing = true;
				gameForm.PlayDashSound();

				// Aktualizace progress baru okamžitě
				if (Owner == gameForm.player1 && Owner.SuperPower is DashPower)
					gameForm.UpdateCooldownBar(Owner, gameForm.cooldownBarP1);
				else if (Owner == gameForm.player2 && Owner.SuperPower is DashPower)
					gameForm.UpdateCooldownBar(Owner, gameForm.cooldownBarP2);

				PictureBox playerBox = (Owner == gameForm.player1) ? gameForm.Player_1 : gameForm.Player_2;
				HashSet<Keys> pressed = gameForm.keysPressed;

				Keys up = (Owner == gameForm.player1) ? Keys.W : Keys.Up;
				Keys down = (Owner == gameForm.player1) ? Keys.S : Keys.Down;
				Keys left = (Owner == gameForm.player1) ? Keys.A : Keys.Left;
				Keys right = (Owner == gameForm.player1) ? Keys.D : Keys.Right;

				Point dir = Point.Empty;
				if (pressed.Contains(up)) dir.Y = -1;
				if (pressed.Contains(down)) dir.Y = 1;
				if (pressed.Contains(left)) dir.X = -1;
				if (pressed.Contains(right)) dir.X = 1;

				if (dir == Point.Empty) return; // žádný směr => neprováděj dash

				// Normalizace směru
				float length = (float)Math.Sqrt(dir.X * dir.X + dir.Y * dir.Y);
				dir.X = (int)(dir.X / length * dashSpeed);
				dir.Y = (int)(dir.Y / length * dashSpeed);

				Task.Run(() =>
				{
					Point startPos = playerBox.Location;
					Stopwatch sw = Stopwatch.StartNew();

					while (sw.ElapsedMilliseconds < dashDurationMs)
					{
						gameForm.Invoke((Action)(() =>
						{
							Point nextPos = new Point(playerBox.Left + dir.X / 5, playerBox.Top + dir.Y / 5);
							Rectangle futureBounds = new Rectangle(nextPos, playerBox.Size);

							// Kolize s překážkami
							if (gameForm.obstaclePolygons.Any(ob => IntersectsPolygon(ob, futureBounds)))

							{
								isDashing = false;
								return;
							}

							// Zásah druhého hráče
							PictureBox otherPlayer = (Owner == gameForm.player1) ? gameForm.Player_2 : gameForm.Player_1;
							if (futureBounds.IntersectsWith(otherPlayer.Bounds))
							{
								if (!hasHitDuringDash)
								{
									Character attacker = Owner;
									Character targetChar = (attacker == gameForm.player1) ? gameForm.player2 : gameForm.player1;
									int dashDamage = attacker.DashDamage;

									gameForm.OnBulletHit(targetChar, attacker, dashDamage);
									hasHitDuringDash = true;
								}
								isDashing = false;
								return;
							}

							playerBox.Location = nextPos;
						}));

						Thread.Sleep(15);
					}

					isDashing = false;
					hasHitDuringDash = false;
				});
			}
		}

		



		public class WallPierce : SuperPower
		{
			private Form1 gameForm;
			private DateTime lastUsed = DateTime.MinValue;
			private int cooldownMs = 20000;
			private Timer cooldownTimer;

			public bool IsReady { get; private set; } = true;
			public DateTime LastUsed => lastUsed;
			public int CooldownMs => cooldownMs;

			public WallPierce(Character owner, Form1 form) : base(owner)
			{
				gameForm = form;

				cooldownTimer = new Timer();
				cooldownTimer.Interval = 100; // každých 100 ms
				cooldownTimer.Tick += CooldownTimer_Tick;
			}

			private void CooldownTimer_Tick(object sender, EventArgs e)
			{
				if (Owner == null || gameForm == null)
					return;

				TimeSpan elapsed = DateTime.Now - lastUsed;
				double progress = Math.Min(1.0, elapsed.TotalMilliseconds / cooldownMs);

				// Update UI
				if (Owner == gameForm.player1 && gameForm.cooldownBarP1 != null)
					gameForm.cooldownBarP1.Value = (int)(progress * 100);
				else if (Owner == gameForm.player2 && gameForm.cooldownBarP2 != null)
					gameForm.cooldownBarP2.Value = (int)(progress * 100);

				// Cooldown complete
				if (!IsReady && elapsed.TotalMilliseconds >= cooldownMs)
				{
					IsReady = true;
					cooldownTimer.Stop();
				}
			}

			public override bool CanActivate()
			{
				return IsReady;
			}

			public override void Activate()
			{
				if (!CanActivate()) return;

				lastUsed = DateTime.Now;
				IsReady = false;

				// Start cooldown tracking
				cooldownTimer.Start();

				// Reset progress bar
				if (Owner == gameForm.player1 && gameForm.cooldownBarP1 != null)
					gameForm.cooldownBarP1.Value = 0;
				else if (Owner == gameForm.player2 && gameForm.cooldownBarP2 != null)
					gameForm.cooldownBarP2.Value = 0;

				// Výpočet směru střely
				PictureBox shooterBox = (Owner == gameForm.player1) ? gameForm.Player_1 : gameForm.Player_2;
				Point shooterCenter = new Point(shooterBox.Left + shooterBox.Width / 2, shooterBox.Top + shooterBox.Height / 2);

				Point targetPos = (Owner == gameForm.player1)
					? new Point(gameForm.Player_2.Left + gameForm.Player_2.Width / 2, gameForm.Player_2.Top + gameForm.Player_2.Height / 2)
					: new Point(gameForm.Player_1.Left + gameForm.Player_1.Width / 2, gameForm.Player_1.Top + gameForm.Player_1.Height / 2);

				float dx = targetPos.X - shooterCenter.X;
				float dy = targetPos.Y - shooterCenter.Y;
				float length = (float)Math.Sqrt(dx * dx + dy * dy);
				float vx = dx / length * Owner.BulletSpeed;
				float vy = dy / length * Owner.BulletSpeed;

				Bullet wallPiercingBullet = new Bullet()
				{
					Position = new PointF(shooterCenter.X, shooterCenter.Y),
					Velocity = new PointF(vx, vy),
					Lifetime = Form1.bulletMaxLifetime,
					Owner = Owner,
					IsWallExplosive = true,
					IsWallPiercing = true
				};

				gameForm.bullets.Add(wallPiercingBullet);
			}
		}
		public void PlayShieldStartSound()
		{
			SoundHelper.PlayOnce(shieldStartSound, 1.0f); // nastav hlasitost dle potřeby
		}

		public void PlayShieldEndSound()
		{
			SoundHelper.PlayOnce(shieldEndSound, 1.0f);
		}

		public class ShieldPower : SuperPower
		{
			private Form1 gameForm;
			private const int ShieldDurationMs = 5000;
			
			private const int DrainPerSecond = 200;

			private Timer shieldDrainTimer;
			private DateTime shieldStartTime;

			public DateTime LastUsed { get; private set; } = DateTime.MinValue;
			public int CooldownMs { get; } = 18000;
			public bool IsReady => (DateTime.Now - LastUsed).TotalMilliseconds >= CooldownMs;
			public bool IsActive => Owner.IsShieldActive;

			public ShieldPower(Character owner, Form1 form) : base(owner)
			{
				gameForm = form;
				if (form == null) return;
				shieldDrainTimer = new Timer();
				shieldDrainTimer.Interval = 100; // každých 100 ms
				shieldDrainTimer.Tick += ShieldDrainTimer_Tick;
				
			}

			public override void Activate()
			{
				if (!IsReady || Owner.IsShieldActive)
					return;
				gameForm.PlayShieldStartSound();


				Owner.IsShieldActive = true;
				Owner.ShieldHealth = Owner.MaxHealth;
				LastUsed = DateTime.Now;
				shieldStartTime = DateTime.Now;

				shieldDrainTimer.Start();
			}

			private void ShieldDrainTimer_Tick(object sender, EventArgs e)
			{
				if (!Owner.IsShieldActive)
				{
					shieldDrainTimer.Stop();
					return;
				}

				// Uplynulý čas od aktivace
				double elapsedTime = (DateTime.Now - shieldStartTime).TotalSeconds;
				if (elapsedTime >= ShieldDurationMs / 1000.0)
				{
					Owner.IsShieldActive = false;
					Owner.ShieldHealth = 0;
					shieldDrainTimer.Stop();
					return;
				}

				// Snížení durability (200 HP za sekundu)
				int drainAmount = (int)(DrainPerSecond * (shieldDrainTimer.Interval / 1000.0));
				Owner.ShieldHealth = Math.Max(0, Owner.ShieldHealth - drainAmount);

				if (Owner.ShieldHealth <= 0)
				{
					gameForm.PlayShieldEndSound();

					Owner.IsShieldActive = false;
					shieldDrainTimer.Stop();
				}
			}

			public override void Update()
			{
				// už nevyužíváno, protože používáme Timer
			}

			public override bool CanActivate()
			{
				return IsReady && !Owner.IsShieldActive;
			}
		}
		public class noWallPower : SuperPower
		{
			private Form1 gameForm;
			private const int NoWallDurationMs = 5000;
			private int NoWallHealth = 5000; // počáteční zdraví NoWall
			private const int DrainPerSecond = 500;

			private Timer NoWallDrainTimer;
			private DateTime NoWallStartTime;

			public DateTime LastUsed { get; private set; } = DateTime.MinValue;
			public int CooldownMs { get; } = 18000;
			public bool IsReady => (DateTime.Now - LastUsed).TotalMilliseconds >= CooldownMs;
			public bool IsActive => Owner.IsNoWallActive;

			public noWallPower(Character owner, Form1 form) : base(owner)
			{
				
				gameForm = form;
				if (form == null) return;
				NoWallDrainTimer = new Timer();
				NoWallDrainTimer.Interval = 100; // každých 100 ms
				NoWallDrainTimer.Tick += NoWallDrainTimer_Tick;

			}

			public override void Activate()
			{
				if (!IsReady || Owner.IsNoWallActive)
					return;
				gameForm.PlayShieldStartSound();


				Owner.IsNoWallActive = true;
				Owner.NoWallHealth = NoWallHealth; // nastav počáteční zdraví
				LastUsed = DateTime.Now;
				NoWallStartTime = DateTime.Now;

				NoWallDrainTimer.Start();
				
			}

			private void NoWallDrainTimer_Tick(object sender, EventArgs e)
			{
				if (!Owner.IsNoWallActive)
				{
					NoWallDrainTimer.Stop();
					return;
				}

				// Uplynulý čas od aktivace
				double elapsedTime = (DateTime.Now - NoWallStartTime).TotalSeconds;
				if (elapsedTime >= NoWallDurationMs / 1000.0)
				{
					Owner.IsNoWallActive = false;
					
					NoWallDrainTimer.Stop();
					return;
				}

				// Snížení durability (200 HP za sekundu)
				int drainAmount = (int)(DrainPerSecond * (NoWallDrainTimer.Interval / 1000.0));
				Owner.NoWallHealth = Math.Max(0, Owner.NoWallHealth - drainAmount);

				if (Owner.NoWallHealth <= 0)
				{
					gameForm.PlayShieldEndSound();

					Owner.IsNoWallActive = false;
					NoWallDrainTimer.Stop();
				}
			}
			

			public override void Update()
			{
				// už nevyužíváno, protože používáme Timer
			}

			public override bool CanActivate()
			{
				return IsReady && !Owner.IsNoWallActive;
			}
		}





		private void DestroyPartOfWall(Path64 wallPolygon, Point explosionCenter)
		{
			const int gapSize = 200;

			// Přehraj zvuk
			SoundHelper.PlayOnce(explosionWallPierceSound, 1.0f);

			// Najdi bounding box polygonu
			long minX = wallPolygon.Min(p => p.X);
			long maxX = wallPolygon.Max(p => p.X);
			long minY = wallPolygon.Min(p => p.Y);
			long maxY = wallPolygon.Max(p => p.Y);

			int wallWidth = (int)(maxX - minX);
			int wallHeight = (int)(maxY - minY);

			// Odeber původní polygon
			obstaclePolygons.Remove(wallPolygon);

			// Urči orientaci (vodorovná nebo svislá zeď)
			if (wallWidth > wallHeight)
			{
				// Vodorovná: rozdělit zleva a zprava od výbuchu
				int left = explosionCenter.X - gapSize / 2;
				int right = explosionCenter.X + gapSize / 2;

				Path64 leftRect = new Path64 {
			new Point64(minX, minY),
			new Point64(Math.Min(left, maxX), minY),
			new Point64(Math.Min(left, maxX), maxY),
			new Point64(minX, maxY)
		};

				Path64 rightRect = new Path64 {
			new Point64(Math.Max(right, minX), minY),
			new Point64(maxX, minY),
			new Point64(maxX, maxY),
			new Point64(Math.Max(right, minX), maxY)
		};

				if (Area64(leftRect) > 0) obstaclePolygons.Add(leftRect);
				if (Area64(rightRect) > 0) obstaclePolygons.Add(rightRect);
			}
			else
			{
				// Svislá: rozdělit nahoře a dole od výbuchu
				int top = explosionCenter.Y - gapSize / 2;
				int bottom = explosionCenter.Y + gapSize / 2;

				Path64 topRect = new Path64 {
			new Point64(minX, minY),
			new Point64(maxX, minY),
			new Point64(maxX, Math.Min(top, maxY)),
			new Point64(minX, Math.Min(top, maxY))
		};

				Path64 bottomRect = new Path64 {
			new Point64(minX, Math.Max(bottom, minY)),
			new Point64(maxX, Math.Max(bottom, minY)),
			new Point64(maxX, maxY),
			new Point64(minX, maxY)
		};

				if (Area64(topRect) > 0) obstaclePolygons.Add(topRect);
				if (Area64(bottomRect) > 0) obstaclePolygons.Add(bottomRect);
			}

			Invalidate();
		}
		private long Area64(Path64 path)
		{
			long area = 0;
			int j = path.Count - 1;
			for (int i = 0; i < path.Count; i++)
			{
				area += (path[j].X + path[i].X) * (path[j].Y - path[i].Y);
				j = i;
			}
			return Math.Abs(area);
		}

		private void DestroyPartOfWallTiny(Path64 wallPolygon, Point impactPoint, int bulletSize, PointF velocity)
		{
			// Vytvoř polygon díry
			float angle = (float)Math.Atan2(velocity.Y, velocity.X);
			int gapLength = bulletSize * 4;
			float dx = (float)Math.Cos(angle) * gapLength / 2f;
			float dy = (float)Math.Sin(angle) * gapLength / 2f;

			PointF p1 = new PointF(impactPoint.X - dx, impactPoint.Y - dy);
			PointF p2 = new PointF(impactPoint.X + dx, impactPoint.Y + dy);

			float vx = p2.X - p1.X;
			float vy = p2.Y - p1.Y;
			float len = (float)Math.Sqrt(vx * vx + vy * vy);
			if (len == 0) return;
			vx /= len;
			vy /= len;

			float px = -vy;
			float py = vx;
			float halfThick = bulletSize * 4;

			PointF[] cutPolygon = new PointF[] {
			new PointF(p1.X + px * halfThick, p1.Y + py * halfThick),
			new PointF(p1.X - px * halfThick, p1.Y - py * halfThick),
			new PointF(p2.X - px * halfThick, p2.Y - py * halfThick),
			new PointF(p2.X + px * halfThick, p2.Y + py * halfThick)};

			Path64 cutPath = new Path64();
			foreach (var pt in cutPolygon)
				cutPath.Add(new Point64((long)pt.X, (long)pt.Y));

			// Clipping pomocí Clipper64
			Clipper64 clipper = new Clipper64();
			clipper.AddSubject(new Paths64 { wallPolygon });
			clipper.AddClip(new Paths64 { cutPath });

			Paths64 result = new Paths64();
			clipper.Execute(ClipType.Difference, FillRule.NonZero, result);

			// Najdi odpovídající polygon a odeber ho
			for (int i = 0; i < obstaclePolygons.Count; i++)
			{
				if (ArePolygonsEquivalent(obstaclePolygons[i], wallPolygon))
				{
					obstaclePolygons.RemoveAt(i);
					break;
				}
			}

			// Přidej nové fragmenty
			foreach (var path in result)
			{
				if (path.Count >= 3)
					obstaclePolygons.Add(path);
			}

			Invalidate();
		}
		private bool ArePolygonsEquivalent(Path64 a, Path64 b)
		{
			if (a.Count != b.Count)
				return false;

			for (int i = 0; i < a.Count; i++)
			{
				if (!b.Contains(a[i]))
					return false;
			}
			return true;
		}


		private List<Path64> SplitWallWithCutLinePolygon(Path64 originalWall, PointF cutStart, PointF cutEnd, int bulletSize)
		{
			// Vytvoř polygon pro díru
			float vx = cutEnd.X - cutStart.X;
			float vy = cutEnd.Y - cutStart.Y;
			float length = (float)Math.Sqrt(vx * vx + vy * vy);
			if (length == 0) return new List<Path64> { originalWall };

			vx /= length;
			vy /= length;

			float perpendicularX = -vy;
			float perpendicularY = vx;
			float halfThickness = bulletSize * 4;

			PointF[] cutPolygon = new PointF[]
			{
			new PointF(cutStart.X + perpendicularX * halfThickness, cutStart.Y + perpendicularY * halfThickness),
			new PointF(cutStart.X - perpendicularX * halfThickness, cutStart.Y - perpendicularY * halfThickness),
			new PointF(cutEnd.X - perpendicularX * halfThickness, cutEnd.Y - perpendicularY * halfThickness),
			new PointF(cutEnd.X + perpendicularX * halfThickness, cutEnd.Y + perpendicularY * halfThickness)
			};

			Path64 cutPath = new Path64();
			foreach (var pt in cutPolygon)
				cutPath.Add(new Point64((long)pt.X, (long)pt.Y));

			// Clipping
			Clipper64 clipper = new Clipper64();
			clipper.AddSubject(new Paths64 { originalWall });
			clipper.AddClip(new Paths64 { cutPath });

			Paths64 result = new Paths64();
			clipper.Execute(ClipType.Difference, FillRule.NonZero, result);

			// Výstup: seznam polygonových fragmentů
			List<Path64> remainingPolygons = new List<Path64>();
			foreach (var path in result)
			{
				if (path.Count >= 3)
					remainingPolygons.Add(path);
			}

			return remainingPolygons;
		}





		private void UpdatePlayerLabelPositions()
		{
			// Position label centered horizontally above Player_1 PictureBox
			lblPlayer1Name.Location = new Point(
				Player_1.Location.X + (Player_1.Width - lblPlayer1Name.Width) / 2,
				Player_1.Location.Y - lblPlayer1Name.Height - 5);

			// Position label centered horizontally above Player_2 PictureBox
			lblPlayer2Name.Location = new Point(
				Player_2.Location.X + (Player_2.Width - lblPlayer2Name.Width) / 2,
				Player_2.Location.Y - lblPlayer2Name.Height - 5);
		}
		private void UpdateHealthDisplay(Label hpLabel, int Health, int MaxHealth)
		{
			float percentage = (float)Health / MaxHealth;

			// Update text
			hpLabel.Text = $"HP: {Health} / {MaxHealth}";

			// Update color based on percentage
			if (percentage >= 0.75f)
				hpLabel.ForeColor = Color.LightGreen;
			else if (percentage >= 0.5f)
				hpLabel.ForeColor = Color.Yellow;
			else if (percentage >= 0.25f)
				hpLabel.ForeColor = Color.Orange;
			else
				hpLabel.ForeColor = Color.Red;
		}
		public void UpdateCooldownBar(Character player, ProgressBar bar)
		{
			if (player?.SuperPower is null)
			{
				bar.Value = 0;
				bar.ForeColor = Color.Gray;
				return;
			}

			DateTime lastUsed;
			int cooldown;
			bool isReady;

			if (player.SuperPower is DashPower dashPower)
			{
				lastUsed = dashPower.LastUsed;
				cooldown = dashPower.CooldownMs;
				isReady = dashPower.IsReady;
			}
			else if (player.SuperPower is WallPierce wallPierce)
			{
				lastUsed = wallPierce.LastUsed;
				cooldown = wallPierce.CooldownMs;
				isReady = wallPierce.IsReady;
			}
			else if (player.SuperPower is ShieldPower shieldPower)
			{
				lastUsed = shieldPower.LastUsed;
				cooldown = shieldPower.CooldownMs;
				isReady = shieldPower.IsReady;
			}
			else if (player.SuperPower is noWallPower noWallPower)
			{
				lastUsed = noWallPower.LastUsed;
				cooldown = noWallPower.CooldownMs;
				isReady = noWallPower.IsReady;
			}
			else
			{
				bar.Value = 0;
				bar.ForeColor = Color.Gray;
				return;
			}

			double elapsedMs = (DateTime.Now - lastUsed).TotalMilliseconds;
			double ratio = Math.Min(1.0, elapsedMs / cooldown);
			bar.Value = (int)(ratio * bar.Maximum);
			bar.ForeColor = isReady ? Color.LimeGreen : Color.OrangeRed;
		}




	}


	class DamagePopup
	{
		public PointF Position;
		public string Text;
		public DateTime StartTime;
		public float Lifetime = 1.0f; // seconds
		public Color Color = Color.Red;

		// Nové vlastnosti navíc – volitelné:
		public float StartFontSize = 12f;
		public bool IsCritical = false;  // např. pro větší font/barvu
		public bool IsHealing = false;   // např. zelený popup
	}

	public abstract class SuperPower
	{
		public Character Owner { get; }
		public virtual void StartCooldownTimer()
		{
			// Defaultní implementace, může být prázdná
		}

		public SuperPower(Character owner)
		{
			Owner = owner;
		}
		public abstract void Activate();

		// Optional for powers with effects over time (e.g., cooldowns, durations)
		public virtual void Update() { }
		public virtual bool CanActivate() => true;

	}
	public class NoKeyPressButton : Button
	{
		protected override bool IsInputKey(Keys keyData)
		{
			// Prevent arrow keys and space/enter from triggering the button
			if (keyData == Keys.Space || keyData == Keys.Enter ||
				keyData == Keys.Left || keyData == Keys.Right ||
				keyData == Keys.Up || keyData == Keys.Down)
			{
				return false;
			}
			return base.IsInputKey(keyData);
		}

		protected override void OnKeyDown(KeyEventArgs kevent)
		{
			// Do nothing on keydown
		}
	}
	public class NoFocusButton : Button
	{
		protected override bool ShowFocusCues => false;

		protected override void OnGotFocus(EventArgs e)
		{
			// Prevent base.OnGotFocus to suppress all keyboard focus
		}

		public NoFocusButton()
		{
			SetStyle(ControlStyles.Selectable, false);
			TabStop = false;
		}
	}


}