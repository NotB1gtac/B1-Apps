using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static B1_Apps.Apps.Game.Form1;

namespace B1_Apps.Apps.Game
{
	public class Character
	{
		public string Name { get; set; }
		public int MaxHealth { get; set; }
		public int Health { get; set; }
		public int Width { get; set; }
		public int Height { get; set; }
		public int Weight { get; set; }
		public int Firerate { get; set; }
		public int Damage { get; set; }
		public int Precision { get; set; }
		public int MagazineSize { get; set; }
		public int ReloadTime { get; set; }
		public int BulletSpeed { get; set; }
		public int BulletSize { get; set; } = 10;
		public bool isExplosive { get; set; } = false;
		public bool isShotgun { get; set; } = false;
		public bool isFullAuto { get; set; } = true;
		public int PelletCount { get; set; } = 10; // počet projektilů při výstřelu z brokovnice, default 10
		public int ExplosiveRange { get; set; } = 100;
		public int ExplosiveDamage { get; set; } = 100;
		public int DashDamage { get; set; } = 550;
		public string Description { get; set; }
		public Image Portrait { get; set; }
		public bool IsShieldActive { get; set; } = false;
		public int ShieldHealth { get; set; } = 0;
		public int NoWallHealth { get; set; } = 0; // pro noWallPower, kolik má postava zdraví, když je aktivní noWallPower
		public DateTime ShieldEndTime { get; set; }
		public bool IsNoWallActive { get; set; } = false;

		public Func<Form1, SuperPower> SuperPowerFactory { get; set; }
		public SuperPower SuperPower { get; set; }
		public UnmanagedMemoryStream ShootingSound { get; set; }

		public bool IsShooting { get; set; }
		public byte[] ShootingSoundBytes { get; set; } // předpřipravený zvuk

		public void AssignPower(Form1 form)
		{
			if (SuperPowerFactory != null)
				SuperPower = SuperPowerFactory(form);
		}

		public void Reset()
		{
			Health = MaxHealth;
			// případně další věci: cooldowny, efekty, stav postavy
		}

		public Character Clone()
		{
			var clone = new Character(Name, MaxHealth, MaxHealth, Width, Height, Weight, Firerate, Damage, Precision,
									  MagazineSize, ReloadTime, BulletSpeed, BulletSize, isExplosive,
									  isFullAuto, isShotgun,PelletCount, ExplosiveRange, ExplosiveDamage, DashDamage,
									  Description, Portrait);
			ShootingSound = this.ShootingSound;
			// Klonujeme i SuperPowerFactory, ale s přepojením na nový klon
			if (this.SuperPowerFactory != null)
			{
				var type = this.SuperPowerFactory(null).GetType();

				clone.SuperPowerFactory = form =>
				{
					return type == typeof(DashPower)
						? new DashPower(clone, form)
						: type == typeof(WallPierce)
							? new WallPierce(clone, form)
							: type == typeof(ShieldPower)
								? new ShieldPower(clone, form)
								: type == typeof (noWallPower)
								? new noWallPower(clone, form)
								
							: throw new InvalidOperationException("Unknown SuperPower type.");
				};
			}

			return clone;
		}

		public Character(string name, int maxhealth, int health, int width, int height, int weight,
						 int firerate, int damage, int precision, int magazinesize, int reloadtime,
						 int bulletspeed, int bulletsize, bool isexplosive, bool isfullauto, bool isshotgun, int pelletcount,
						 int explosiverange, int explosivedamage, int dashDamage, string description, Image portrait)
		{
			Name = name;
			MaxHealth = maxhealth;
			Health = health;
			Width = width;
			Height = height;
			Weight = weight;
			Firerate = firerate;
			Damage = damage;
			Precision = precision;
			MagazineSize = magazinesize;
			ReloadTime = reloadtime;
			BulletSpeed = bulletspeed;
			BulletSize = bulletsize;
			isExplosive = isexplosive;
			isFullAuto = isfullauto;
			isShotgun = isshotgun;
			PelletCount = pelletcount;
			ExplosiveRange = explosiverange;
			ExplosiveDamage = explosivedamage;
			DashDamage = dashDamage;
			Description = description;
			Portrait = portrait;
			
		}

		public class Explosion
		{
			public PointF Center;
			public int Radius;
			public DateTime StartTime;
			public int DurationMs = 500;
		}

		public override string ToString() => Name;
	}
}
