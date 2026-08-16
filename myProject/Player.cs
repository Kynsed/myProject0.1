using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;

namespace myProject
{
	[Tracked(false)]
	public class Player : Actor
	{
		public bool Dead { get; private set; }

		public Player(Vector2 position, PlayerSpriteMode spriteMode) : base(new Vector2((float)((int)position.X), (float)((int)position.Y)))
		{
			Input.ResetGrab();
			this.DefaultSpriteMode = spriteMode;
			base.Depth = 0;
			base.Tag = Tags.Persistent;
			if (SaveData.Instance != null && SaveData.Instance.Assists.PlayAsBadeline)
			{
				spriteMode = PlayerSpriteMode.MadelineAsBadeline;
			}
			this.Sprite = new PlayerSprite(spriteMode);
			base.Add(this.Hair = new PlayerHair(this.Sprite));
			base.Add(this.Sprite);
			if (spriteMode == PlayerSpriteMode.MadelineAsBadeline)
			{
				this.Hair.Color = Player.NormalBadelineHairColor;
			}
			else
			{
				this.Hair.Color = Player.NormalHairColor;
			}
			this.startHairCount = this.Sprite.HairCount;
			this.sweatSprite = GFX.SpriteBank.Create("player_sweat");
			base.Add(this.sweatSprite);
			base.Collider = this.normalHitbox;
			this.hurtbox = this.normalHurtbox;
			this.onCollideH = new Collision(this.OnCollideH);
			this.onCollideV = new Collision(this.OnCollideV);
			this.StateMachine = new StateMachine(26);
			this.StateMachine.SetCallbacks(0, new Func<int>(this.NormalUpdate), null, new Action(this.NormalBegin), new Action(this.NormalEnd));
			this.StateMachine.SetCallbacks(1, new Func<int>(this.ClimbUpdate), null, new Action(this.ClimbBegin), new Action(this.ClimbEnd));
			this.StateMachine.SetCallbacks(2, new Func<int>(this.DashUpdate), new Func<IEnumerator>(this.DashCoroutine), new Action(this.DashBegin), new Action(this.DashEnd));
			this.StateMachine.SetCallbacks(3, new Func<int>(this.SwimUpdate), null, new Action(this.SwimBegin), null);
			this.StateMachine.SetCallbacks(4, new Func<int>(this.BoostUpdate), new Func<IEnumerator>(this.BoostCoroutine), new Action(this.BoostBegin), new Action(this.BoostEnd));
			this.StateMachine.SetCallbacks(5, new Func<int>(this.RedDashUpdate), new Func<IEnumerator>(this.RedDashCoroutine), new Action(this.RedDashBegin), new Action(this.RedDashEnd));
			this.StateMachine.SetCallbacks(6, new Func<int>(this.HitSquashUpdate), null, new Action(this.HitSquashBegin), null);
			this.StateMachine.SetCallbacks(7, new Func<int>(this.LaunchUpdate), null, new Action(this.LaunchBegin), null);
			this.StateMachine.SetCallbacks(8, null, new Func<IEnumerator>(this.PickupCoroutine), null, null);
			this.StateMachine.SetCallbacks(9, new Func<int>(this.DreamDashUpdate), null, new Action(this.DreamDashBegin), new Action(this.DreamDashEnd));
			this.StateMachine.SetCallbacks(10, new Func<int>(this.SummitLaunchUpdate), null, new Action(this.SummitLaunchBegin), null);
			this.StateMachine.SetCallbacks(11, new Func<int>(this.DummyUpdate), null, new Action(this.DummyBegin), null);
			this.StateMachine.SetCallbacks(12, null, new Func<IEnumerator>(this.IntroWalkCoroutine), null, null);
			this.StateMachine.SetCallbacks(13, null, new Func<IEnumerator>(this.IntroJumpCoroutine), null, null);
			this.StateMachine.SetCallbacks(14, null, null, new Action(this.IntroRespawnBegin), new Action(this.IntroRespawnEnd));
			this.StateMachine.SetCallbacks(15, null, new Func<IEnumerator>(this.IntroWakeUpCoroutine), null, null);
			this.StateMachine.SetCallbacks(20, new Func<int>(this.TempleFallUpdate), new Func<IEnumerator>(this.TempleFallCoroutine), null, null);
			this.StateMachine.SetCallbacks(18, new Func<int>(this.ReflectionFallUpdate), new Func<IEnumerator>(this.ReflectionFallCoroutine), new Action(this.ReflectionFallBegin), new Action(this.ReflectionFallEnd));
			this.StateMachine.SetCallbacks(16, new Func<int>(this.BirdDashTutorialUpdate), new Func<IEnumerator>(this.BirdDashTutorialCoroutine), new Action(this.BirdDashTutorialBegin), null);
			this.StateMachine.SetCallbacks(17, new Func<int>(this.FrozenUpdate), null, null, null);
			this.StateMachine.SetCallbacks(19, new Func<int>(this.StarFlyUpdate), new Func<IEnumerator>(this.StarFlyCoroutine), new Action(this.StarFlyBegin), new Action(this.StarFlyEnd));
			this.StateMachine.SetCallbacks(21, new Func<int>(this.CassetteFlyUpdate), new Func<IEnumerator>(this.CassetteFlyCoroutine), new Action(this.CassetteFlyBegin), new Action(this.CassetteFlyEnd));
			this.StateMachine.SetCallbacks(22, new Func<int>(this.AttractUpdate), null, new Action(this.AttractBegin), new Action(this.AttractEnd));
			this.StateMachine.SetCallbacks(23, null, new Func<IEnumerator>(this.IntroMoonJumpCoroutine), null, null);
			this.StateMachine.SetCallbacks(24, new Func<int>(this.FlingBirdUpdate), new Func<IEnumerator>(this.FlingBirdCoroutine), new Action(this.FlingBirdBegin), new Action(this.FlingBirdEnd));
			this.StateMachine.SetCallbacks(25, null, new Func<IEnumerator>(this.IntroThinkForABitCoroutine), null, null);
			base.Add(this.StateMachine);
			base.Add(this.Leader = new Leader(new Vector2(0f, -8f)));
			this.lastAim = Vector2.UnitX;
			this.Facing = Facings.Right;
			this.ChaserStates = new List<Player.ChaserState>();
			this.triggersInside = new HashSet<Trigger>();
			base.Add(this.Light = new VertexLight(this.normalLightOffset, Color.White, 1f, 32, 64));
			base.Add(new WaterInteraction(() => this.StateMachine.State == 2 || this.StateMachine.State == 18));
			base.Add(new WindMover(new Action<Vector2>(this.WindMove)));
			base.Add(this.wallSlideSfx = new SoundSource());
			base.Add(this.swimSurfaceLoopSfx = new SoundSource());
			this.Sprite.OnFrameChange = delegate(string anim)
			{
				if (base.Scene != null && !this.Dead && this.Sprite.Visible)
				{
					int currentAnimationFrame = this.Sprite.CurrentAnimationFrame;
					if ((anim.Equals("runSlow_carry") && (currentAnimationFrame == 0 || currentAnimationFrame == 6)) || (anim.Equals("runFast") && (currentAnimationFrame == 0 || currentAnimationFrame == 6)) || (anim.Equals("runSlow") && (currentAnimationFrame == 0 || currentAnimationFrame == 6)) || (anim.Equals("walk") && (currentAnimationFrame == 0 || currentAnimationFrame == 6)) || (anim.Equals("runStumble") && currentAnimationFrame == 6) || (anim.Equals("flip") && currentAnimationFrame == 4) || (anim.Equals("runWind") && (currentAnimationFrame == 0 || currentAnimationFrame == 6)) || (anim.Equals("idleC") && this.Sprite.Mode == PlayerSpriteMode.MadelineNoBackpack && (currentAnimationFrame == 3 || currentAnimationFrame == 6 || currentAnimationFrame == 8 || currentAnimationFrame == 11)) || (anim.Equals("carryTheoWalk") && (currentAnimationFrame == 0 || currentAnimationFrame == 6)) || (anim.Equals("push") && (currentAnimationFrame == 8 || currentAnimationFrame == 15)))
					{
						Platform platformByPriority = SurfaceIndex.GetPlatformByPriority(base.CollideAll<Platform>(this.Position + Vector2.UnitY, this.temp));
						if (platformByPriority != null)
						{
							this.Play("event:/char/madeline/footstep", "surface_index", (float)platformByPriority.GetStepSoundIndex(this));
						}
					}
					else if ((anim.Equals("climbUp") && currentAnimationFrame == 5) || (anim.Equals("climbDown") && currentAnimationFrame == 5))
					{
						Platform platformByPriority2 = SurfaceIndex.GetPlatformByPriority(base.CollideAll<Solid>(base.Center + Vector2.UnitX * (float)this.Facing, this.temp));
						if (platformByPriority2 != null)
						{
							this.Play("event:/char/madeline/handhold", "surface_index", (float)platformByPriority2.GetWallSoundIndex(this, (int)this.Facing));
						}
					}
					else if (anim.Equals("wakeUp") && currentAnimationFrame == 19)
					{
						this.Play("event:/char/madeline/campfire_stand", null, 0f);
					}
					else if (anim.Equals("sitDown") && currentAnimationFrame == 12)
					{
						this.Play("event:/char/madeline/summit_sit", null, 0f);
					}
					if (anim.Equals("push") && (currentAnimationFrame == 8 || currentAnimationFrame == 15))
					{
						Dust.BurstFG(this.Position + new Vector2((float)(-(int)this.Facing *5), -1f), new Vector2((float)(-(float)this.Facing), -0.5f).Angle(), 1, 0f, null);
					}
				}
			};
			this.Sprite.OnLastFrame = delegate(string anim)
			{
				if (base.Scene != null && !this.Dead && this.Sprite.CurrentAnimationID == "idle" && !this.level.InCutscene && this.idleTimer > 3f && Calc.Random.Chance(0.2f))
				{
					string text;
					if (this.Sprite.Mode == PlayerSpriteMode.Madeline)
					{
						text = ((this.level.CoreMode == Session.CoreModes.Hot) ? Player.idleWarmOptions : Player.idleColdOptions).Choose();
					}
					else
					{
						text = Player.idleNoBackpackOptions.Choose();
					}
					if (!string.IsNullOrEmpty(text) && this.Sprite.Has(text))
					{
						this.Sprite.Play(text, false, false);
						if (this.Sprite.Mode == PlayerSpriteMode.Madeline)
						{
							if (text == "idleB")
							{
								this.idleSfx = this.Play("event:/char/madeline/idle_scratch", null, 0f);
								return;
							}
							if (text == "idleC")
							{
								this.idleSfx = this.Play("event:/char/madeline/idle_sneeze", null, 0f);
								return;
							}
						}
						else if (text == "idleA")
						{
							this.idleSfx = this.Play("event:/char/madeline/idle_crackknuckles", null, 0f);
						}
					}
				}
			};
			this.Sprite.OnChange = delegate(string last, string next)
			{
				if ((last == "idleB" || last == "idleC") && next != null && !next.StartsWith("idle") && this.idleSfx != null)
				{
					Audio.Stop(this.idleSfx, true);
				}
			};
			base.Add(this.reflection = new MirrorReflection());
		}

		public void ResetSpriteNextFrame(PlayerSpriteMode mode)
		{
			this.nextSpriteMode = new PlayerSpriteMode?(mode);
		}

		public void ResetSprite(PlayerSpriteMode mode)
		{
			string currentAnimationID = this.Sprite.CurrentAnimationID;
			int currentAnimationFrame = this.Sprite.CurrentAnimationFrame;
			this.Sprite.RemoveSelf();
			base.Add(this.Sprite = new PlayerSprite(mode));
			if (this.Sprite.Has(currentAnimationID))
			{
				this.Sprite.Play(currentAnimationID, false, false);
				if (currentAnimationFrame < this.Sprite.CurrentAnimationTotalFrames)
				{
					this.Sprite.SetAnimationFrame(currentAnimationFrame);
				}
			}
			this.Hair.Sprite = this.Sprite;
		}

		public override void Added(Scene scene)
		{
			base.Added(scene);
			this.level = base.SceneAs<Level>();
			this.lastDashes = (this.Dashes = this.MaxDashes);
			SpawnFacingTrigger spawnFacingTrigger = base.CollideFirst<SpawnFacingTrigger>();
			if (spawnFacingTrigger != null)
			{
				this.Facing = spawnFacingTrigger.Facing;
			}
			else if (base.X > (float)this.level.Bounds.Center.X && this.IntroType != Player.IntroTypes.None)
			{
				this.Facing = Facings.Left;
			}
			switch (this.IntroType)
			{
			case Player.IntroTypes.Respawn:
				this.StateMachine.State = 14;
				this.JustRespawned = true;
				break;
			case Player.IntroTypes.WalkInRight:
				this.IntroWalkDirection = Facings.Right;
				this.StateMachine.State = 12;
				break;
			case Player.IntroTypes.WalkInLeft:
				this.IntroWalkDirection = Facings.Left;
				this.StateMachine.State = 12;
				break;
			case Player.IntroTypes.Jump:
				this.StateMachine.State = 13;
				break;
			case Player.IntroTypes.WakeUp:
				this.Sprite.Play("asleep", false, false);
				this.Facing = Facings.Right;
				this.StateMachine.State = 15;
				break;
			case Player.IntroTypes.Fall:
				this.StateMachine.State = 18;
				break;
			case Player.IntroTypes.TempleMirrorVoid:
				this.StartTempleMirrorVoidSleep();
				break;
			case Player.IntroTypes.None:
				this.StateMachine.State = 0;
				break;
			case Player.IntroTypes.ThinkForABit:
				this.StateMachine.State = 25;
				break;
			}
			this.IntroType = Player.IntroTypes.Transition;
			this.StartHair();
			this.PreviousPosition = this.Position;
		}

		public void StartTempleMirrorVoidSleep()
		{
			this.Sprite.Play("asleep", false, false);
			this.Facing = Facings.Right;
			this.StateMachine.State = 11;
			this.StateMachine.Locked = true;
			this.DummyAutoAnimate = false;
			this.DummyGravity = false;
		}

		public override void Removed(Scene scene)
		{
			base.Removed(scene);
			this.level = null;
			Audio.Stop(this.conveyorLoopSfx, true);
			foreach (Trigger trigger in this.triggersInside)
			{
				trigger.Triggered = false;
				trigger.OnLeave(this);
			}
			this.triggersInside.Clear();
		}

		public override void SceneEnd(Scene scene)
		{
			base.SceneEnd(scene);
			Audio.Stop(this.conveyorLoopSfx, true);
		}

		public override void Render()
		{
			if (SaveData.Instance.Assists.InvisibleMotion && this.InControl)
			{
				if (!this.onGround && this.StateMachine.State != 1 && this.StateMachine.State != 3)
				{
					return;
				}
				if (this.Speed.LengthSquared() > 800f)
				{
					return;
				}
			}
			Vector2 renderPosition = this.Sprite.RenderPosition;
			this.Sprite.RenderPosition = this.Sprite.RenderPosition.Floored();
			if (this.StateMachine.State == 14)
			{
				DeathEffect.Draw(base.Center + this.deadOffset, this.Hair.Color, this.introEase);
			}
			else
			{
				if (this.StateMachine.State != 19)
				{
					if (this.IsTired && this.flash)
					{
						this.Sprite.Color = Color.Red;
					}
					else
					{
						this.Sprite.Color = Color.White;
					}
				}
				if (this.reflection.IsRendering && this.FlipInReflection)
				{
					this.Facing = (Facings)(-(int)this.Facing);
					this.Hair.Facing = this.Facing;
				}
				PlayerSprite sprite = this.Sprite;
				sprite.Scale.X = sprite.Scale.X * (float)this.Facing;
				if (this.sweatSprite.LastAnimationID == "idle")
				{
					this.sweatSprite.Scale = this.Sprite.Scale;
				}
				else
				{
					this.sweatSprite.Scale.Y = this.Sprite.Scale.Y;
					this.sweatSprite.Scale.X = Math.Abs(this.Sprite.Scale.X) * (float)Math.Sign(this.sweatSprite.Scale.X);
				}
				base.Render();
				if (this.Sprite.CurrentAnimationID == "startStarFly")
				{
					float scale = (float)this.Sprite.CurrentAnimationFrame / (float)this.Sprite.CurrentAnimationTotalFrames;
					GFX.Game.GetAtlasSubtexturesAt("characters/player/startStarFlyWhite", this.Sprite.CurrentAnimationFrame).Draw(this.Sprite.RenderPosition, this.Sprite.Origin, this.starFlyColor * scale, this.Sprite.Scale, this.Sprite.Rotation, SpriteEffects.None);
				}
				PlayerSprite sprite2 = this.Sprite;
				sprite2.Scale.X = sprite2.Scale.X * (float)this.Facing;
				if (this.reflection.IsRendering && this.FlipInReflection)
				{
					this.Facing = (Facings)(-(int)this.Facing);
					this.Hair.Facing = this.Facing;
				}
			}
			this.Sprite.RenderPosition = renderPosition;
		}

		public override void DebugRender(Camera camera)
		{
			base.DebugRender(camera);
			Collider collider = base.Collider;
			base.Collider = this.hurtbox;
			Draw.HollowRect(base.Collider, Color.Lime);
			base.Collider = collider;
		}

		public override void Update()
		{
			if (SaveData.Instance.Assists.InfiniteStamina)
			{
				this.Stamina = 110f;
			}
			this.PreviousPosition = this.Position;
			if (this.nextSpriteMode != null)
			{
				this.ResetSprite(this.nextSpriteMode.Value);
				this.nextSpriteMode = null;
			}
			this.climbTriggerDir = 0;
			if (SaveData.Instance.Assists.Hiccups)
			{
				if (this.hiccupTimer <= 0f)
				{
					this.hiccupTimer = this.level.HiccupRandom.Range(1.2f, 1.8f);
				}
				if (this.Ducking)
				{
					this.hiccupTimer -= Engine.DeltaTime * 0.5f;
				}
				else
				{
					this.hiccupTimer -= Engine.DeltaTime;
				}
				if (this.hiccupTimer <= 0f)
				{
					this.HiccupJump();
				}
			}
			if (this.gliderBoostTimer > 0f)
			{
				this.gliderBoostTimer -= Engine.DeltaTime;
			}
			if (this.lowFrictionStopTimer > 0f)
			{
				this.lowFrictionStopTimer -= Engine.DeltaTime;
			}
			if (this.explodeLaunchBoostTimer > 0f)
			{
				if (Input.MoveX.Value == Math.Sign(this.explodeLaunchBoostSpeed))
				{
					this.Speed.X = this.explodeLaunchBoostSpeed;
					this.explodeLaunchBoostTimer = 0f;
				}
				else
				{
					this.explodeLaunchBoostTimer -= Engine.DeltaTime;
				}
			}
			this.StrawberryCollectResetTimer -= Engine.DeltaTime;
			if (this.StrawberryCollectResetTimer <= 0f)
			{
				this.StrawberryCollectIndex = 0;
			}
			this.idleTimer += Engine.DeltaTime;
			if (this.level != null && this.level.InCutscene)
			{
				this.idleTimer = -5f;
			}
			else if (this.Speed.X != 0f || this.Speed.Y != 0f)
			{
				this.idleTimer = 0f;
			}
			if (!this.Dead)
			{
				Audio.MusicUnderwater = this.UnderwaterMusicCheck();
			}
			if (this.JustRespawned && this.Speed != Vector2.Zero)
			{
				this.JustRespawned = false;
			}
			if (this.StateMachine.State == 9)
			{
				this.onGround = (this.OnSafeGround = false);
			}
			else if (this.Speed.Y >= 0f)
			{
				Platform platform = base.CollideFirst<Solid>(this.Position + Vector2.UnitY);
				if (platform == null)
				{
					platform = base.CollideFirstOutside<JumpThru>(this.Position + Vector2.UnitY);
				}
				if (platform != null)
				{
					this.onGround = true;
					this.OnSafeGround = platform.Safe;
				}
				else
				{
					this.onGround = (this.OnSafeGround = false);
				}
			}
			else
			{
				this.onGround = (this.OnSafeGround = false);
			}
			if (this.StateMachine.State == 3)
			{
				this.OnSafeGround = true;
			}
			if (this.OnSafeGround)
			{
				using (List<Component>.Enumerator enumerator = base.Scene.Tracker.GetComponents<SafeGroundBlocker>().GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						if (((SafeGroundBlocker)enumerator.Current).Check(this))
						{
							this.OnSafeGround = false;
							break;
						}
					}
				}
			}
			this.playFootstepOnLand -= Engine.DeltaTime;
			if (this.onGround)
			{
				this.highestAirY = base.Y;
			}
			else
			{
				this.highestAirY = Math.Min(base.Y, this.highestAirY);
			}
			if (base.Scene.OnInterval(0.05f))
			{
				this.flash = !this.flash;
			}
			if (this.wallSlideDir != 0)
			{
				this.wallSlideTimer = Math.Max(this.wallSlideTimer - Engine.DeltaTime, 0f);
				this.wallSlideDir = 0;
			}
			if (this.wallBoostTimer > 0f)
			{
				this.wallBoostTimer -= Engine.DeltaTime;
				if (this.moveX == this.wallBoostDir)
				{
					this.Speed.X = 130f * (float)this.moveX;
					this.Stamina += 27.5f;
					this.wallBoostTimer = 0f;
					this.sweatSprite.Play("idle", false, false);
				}
			}
			if (this.onGround && this.StateMachine.State != 1)
			{
				this.AutoJump = false;
				this.Stamina = 110f;
				this.wallSlideTimer = 1.2f;
			}
			if (this.dashAttackTimer > 0f)
			{
				this.dashAttackTimer -= Engine.DeltaTime;
			}
			if (this.onGround)
			{
				this.dreamJump = false;
				this.jumpGraceTimer = 0.1f;
			}
			else if (this.jumpGraceTimer > 0f)
			{
				this.jumpGraceTimer -= Engine.DeltaTime;
			}
			if (this.dashCooldownTimer > 0f)
			{
				this.dashCooldownTimer -= Engine.DeltaTime;
			}
			if (this.dashRefillCooldownTimer > 0f)
			{
				this.dashRefillCooldownTimer -= Engine.DeltaTime;
			}
			else if (SaveData.Instance.Assists.DashMode == Assists.DashModes.Infinite && !this.level.InCutscene)
			{
				this.RefillDash();
			}
			else if (!this.Inventory.NoRefills)
			{
				if (this.StateMachine.State == 3)
				{
					this.RefillDash();
				}
				else if (this.onGround && (base.CollideCheck<Solid, NegaBlock>(this.Position + Vector2.UnitY) || base.CollideCheckOutside<JumpThru>(this.Position + Vector2.UnitY)) && (!base.CollideCheck<Spikes>(this.Position) || SaveData.Instance.Assists.Invincible))
				{
					this.RefillDash();
				}
			}
			if (this.varJumpTimer > 0f)
			{
				this.varJumpTimer -= Engine.DeltaTime;
			}
			if (this.AutoJumpTimer > 0f)
			{
				if (this.AutoJump)
				{
					this.AutoJumpTimer -= Engine.DeltaTime;
					if (this.AutoJumpTimer <= 0f)
					{
						this.AutoJump = false;
					}
				}
				else
				{
					this.AutoJumpTimer = 0f;
				}
			}
			if (this.forceMoveXTimer > 0f)
			{
				this.forceMoveXTimer -= Engine.DeltaTime;
				this.moveX = this.forceMoveX;
			}
			else
			{
				this.moveX = Input.MoveX.Value;
				this.climbHopSolid = null;
			}
			if (this.climbHopSolid != null && !this.climbHopSolid.Collidable)
			{
				this.climbHopSolid = null;
			}
			else if (this.climbHopSolid != null && this.climbHopSolid.Position != this.climbHopSolidPosition)
			{
				Vector2 vector = this.climbHopSolid.Position - this.climbHopSolidPosition;
				this.climbHopSolidPosition = this.climbHopSolid.Position;
				base.MoveHExact((int)vector.X, null, null);
				base.MoveVExact((int)vector.Y, null, null);
			}
			if (this.noWindTimer > 0f)
			{
				this.noWindTimer -= Engine.DeltaTime;
			}
			if (this.moveX != 0 && this.InControl && this.StateMachine.State != 1 && this.StateMachine.State != 8 && this.StateMachine.State != 5 && this.StateMachine.State != 6)
			{
				Facings facings = (Facings)this.moveX;
				if (facings != this.Facing && this.Ducking)
				{
					this.Sprite.Scale = new Vector2(0.8f, 1.2f);
				}
				this.Facing = facings;
			}
			this.lastAim = Input.GetAimVector(this.Facing);
			if (this.wallSpeedRetentionTimer > 0f)
			{
				if (Math.Sign(this.Speed.X) == -Math.Sign(this.wallSpeedRetained))
				{
					this.wallSpeedRetentionTimer = 0f;
				}
				else if (!base.CollideCheck<Solid>(this.Position + Vector2.UnitX * (float)Math.Sign(this.wallSpeedRetained)))
				{
					this.Speed.X = this.wallSpeedRetained;
					this.wallSpeedRetentionTimer = 0f;
				}
				else
				{
					this.wallSpeedRetentionTimer -= Engine.DeltaTime;
				}
			}
			if (this.hopWaitX != 0)
			{
				if (Math.Sign(this.Speed.X) == -this.hopWaitX || this.Speed.Y > 0f)
				{
					this.hopWaitX = 0;
				}
				else if (!base.CollideCheck<Solid>(this.Position + Vector2.UnitX * (float)this.hopWaitX))
				{
					this.lowFrictionStopTimer = 0.15f;
					this.Speed.X = this.hopWaitXSpeed;
					this.hopWaitX = 0;
				}
			}
			if (this.windTimeout > 0f)
			{
				this.windTimeout -= Engine.DeltaTime;
			}
			Vector2 forceStrongWindHair = this.windDirection;
			if (this.ForceStrongWindHair.Length() > 0f)
			{
				forceStrongWindHair = this.ForceStrongWindHair;
			}
			if (this.windTimeout > 0f && forceStrongWindHair.X != 0f)
			{
				this.windHairTimer += Engine.DeltaTime * 8f;
				this.Hair.StepPerSegment = new Vector2(forceStrongWindHair.X * 5f, (float)Math.Sin((double)this.windHairTimer));
				this.Hair.StepInFacingPerSegment = 0f;
				this.Hair.StepApproach = 128f;
				this.Hair.StepYSinePerSegment = 0f;
			}
			else if (this.Dashes > 1)
			{
				this.Hair.StepPerSegment = new Vector2((float)Math.Sin((double)(base.Scene.TimeActive * 2f)) * 0.7f - (float)((int)this.Facing *3), (float)Math.Sin((double)(base.Scene.TimeActive * 1f)));
				this.Hair.StepInFacingPerSegment = 0f;
				this.Hair.StepApproach = 90f;
				this.Hair.StepYSinePerSegment = 1f;
				PlayerHair hair = this.Hair;
				hair.StepPerSegment.Y = hair.StepPerSegment.Y + forceStrongWindHair.Y * 2f;
			}
			else
			{
				this.Hair.StepPerSegment = new Vector2(0f, 2f);
				this.Hair.StepInFacingPerSegment = 0.5f;
				this.Hair.StepApproach = 64f;
				this.Hair.StepYSinePerSegment = 0f;
				PlayerHair hair2 = this.Hair;
				hair2.StepPerSegment.Y = hair2.StepPerSegment.Y + forceStrongWindHair.Y * 0.5f;
			}
			if (this.StateMachine.State == 5)
			{
				this.Sprite.HairCount = 1;
			}
			else if (this.StateMachine.State != 19)
			{
				this.Sprite.HairCount = ((this.Dashes > 1) ? 5 : this.startHairCount);
			}
			if (this.minHoldTimer > 0f)
			{
				this.minHoldTimer -= Engine.DeltaTime;
			}
			if (this.launched)
			{
				if (this.Speed.LengthSquared() < 19600f)
				{
					this.launched = false;
				}
				else
				{
					float prevVal = this.launchedTimer;
					this.launchedTimer += Engine.DeltaTime;
					if (this.launchedTimer >= 0.5f)
					{
						this.launched = false;
						this.launchedTimer = 0f;
					}
					else if (Calc.OnInterval(this.launchedTimer, prevVal, 0.15f))
					{
						this.level.Add(Engine.Pooler.Create<SpeedRing>().Init(base.Center, this.Speed.Angle(), Color.White));
					}
				}
			}
			else
			{
				this.launchedTimer = 0f;
			}
			if (this.IsTired)
			{
				Input.Rumble(RumbleStrength.Light, RumbleLength.Short);
				if (!this.wasTired)
				{
					this.wasTired = true;
				}
			}
			else
			{
				this.wasTired = false;
			}
			base.Update();
			if (this.Ducking)
			{
				this.Light.Position = this.duckingLightOffset;
			}
			else
			{
				this.Light.Position = this.normalLightOffset;
			}
			if (!this.onGround && this.Speed.Y <= 0f && (this.StateMachine.State != 1 || this.lastClimbMove == -1) && base.CollideCheck<JumpThru>() && !this.JumpThruBoostBlockedCheck())
			{
				base.MoveV(-40f * Engine.DeltaTime, null, null);
			}
			if (!this.onGround && this.DashAttacking && this.DashDir.Y == 0f && (base.CollideCheck<Solid>(this.Position + Vector2.UnitY * 3f) || base.CollideCheckOutside<JumpThru>(this.Position + Vector2.UnitY * 3f)) && !this.DashCorrectCheck(Vector2.UnitY * 3f))
			{
				base.MoveVExact(3, null, null);
			}
			if (this.Speed.Y > 0f && this.CanUnDuck && base.Collider != this.starFlyHitbox && !this.onGround && this.jumpGraceTimer <= 0f)
			{
				this.Ducking = false;
			}
			if (this.StateMachine.State != 9 && this.StateMachine.State != 22)
			{
				base.MoveH(this.Speed.X * Engine.DeltaTime, this.onCollideH, null);
			}
			if (this.StateMachine.State != 9 && this.StateMachine.State != 22)
			{
				base.MoveV(this.Speed.Y * Engine.DeltaTime, this.onCollideV, null);
			}
			if (this.StateMachine.State == 3)
			{
				if (this.Speed.Y < 0f && this.Speed.Y >= -60f)
				{
					while (!this.SwimCheck())
					{
						this.Speed.Y = 0f;
						if (base.MoveVExact(1, null, null))
						{
							break;
						}
					}
				}
			}
			else if (this.StateMachine.State == 0 && this.SwimCheck())
			{
				this.StateMachine.State = 3;
			}
			else if (this.StateMachine.State == 1 && this.SwimCheck())
			{
				Water water = base.CollideFirst<Water>(this.Position);
				if (water != null && base.Center.Y < water.Center.Y)
				{
					while (this.SwimCheck() && !base.MoveVExact(-1, null, null))
					{
					}
					if (this.SwimCheck())
					{
						this.StateMachine.State = 3;
					}
				}
				else
				{
					this.StateMachine.State = 3;
				}
			}
			if (this.Sprite.CurrentAnimationID != null && this.Sprite.CurrentAnimationID.Equals("wallslide") && this.Speed.Y > 0f)
			{
				if (!this.wallSlideSfx.Playing)
				{
					this.Loop(this.wallSlideSfx, "event:/char/madeline/wallslide");
				}
				Platform platformByPriority = SurfaceIndex.GetPlatformByPriority(base.CollideAll<Solid>(base.Center + Vector2.UnitX * (float)this.Facing, this.temp));
				if (platformByPriority != null)
				{
					this.wallSlideSfx.Param("surface_index", (float)platformByPriority.GetWallSoundIndex(this, (int)this.Facing));
				}
			}
			else
			{
				this.Stop(this.wallSlideSfx);
			}
			this.UpdateSprite();
			this.UpdateCarry();
			if (this.StateMachine.State != 18)
			{
				foreach (Entity entity in base.Scene.Tracker.GetEntities<Trigger>())
				{
					Trigger trigger = (Trigger)entity;
					if (base.CollideCheck(trigger))
					{
						if (!trigger.Triggered)
						{
							trigger.Triggered = true;
							this.triggersInside.Add(trigger);
							trigger.OnEnter(this);
						}
						trigger.OnStay(this);
					}
					else if (trigger.Triggered)
					{
						this.triggersInside.Remove(trigger);
						trigger.Triggered = false;
						trigger.OnLeave(this);
					}
				}
			}
			this.StrawberriesBlocked = base.CollideCheck<BlockField>();
			// NOTE (jogo proprio): com uma GameCamera na cena quem manda na camera e ela;
			// este follow fiel do Celeste segue valendo quando ela nao existe (--parity).
			if ((this.InControl || this.ForceCameraUpdate) && this.level.FollowCamera == null)
			{
				if (this.StateMachine.State == 18)
				{
					this.level.Camera.Position = this.CameraTarget;
				}
				else
				{
					Vector2 position = this.level.Camera.Position;
					Vector2 cameraTarget = this.CameraTarget;
					float num = (this.StateMachine.State == 20) ? 8f : 1f;
					this.level.Camera.Position = position + (cameraTarget - position) * (1f - (float)Math.Pow((double)(0.01f / num), (double)Engine.DeltaTime));
				}
			}
			if (!this.Dead && this.StateMachine.State != 21)
			{
				Collider collider = base.Collider;
				base.Collider = this.hurtbox;
				using (List<Component>.Enumerator enumerator = base.Scene.Tracker.GetComponents<PlayerCollider>().GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						if (((PlayerCollider)enumerator.Current).Check(this) && this.Dead)
						{
							base.Collider = collider;
							return;
						}
					}
				}
				if (base.Collider == this.hurtbox)
				{
					base.Collider = collider;
				}
			}
			if (this.InControl && !this.Dead && this.StateMachine.State != 9 && this.EnforceLevelBounds)
			{
				this.level.EnforceBounds(this);
			}
			this.UpdateChaserStates();
			this.UpdateHair(true);
			if (this.wasDucking != this.Ducking)
			{
				this.wasDucking = this.Ducking;
				if (this.wasDucking)
				{
					this.Play("event:/char/madeline/duck", null, 0f);
				}
				else if (this.onGround)
				{
					this.Play("event:/char/madeline/stand", null, 0f);
				}
			}
			if (this.Speed.X != 0f && ((this.StateMachine.State == 3 && !this.SwimUnderwaterCheck()) || (this.StateMachine.State == 0 && base.CollideCheck<Water>(this.Position))))
			{
				if (!this.swimSurfaceLoopSfx.Playing)
				{
					this.swimSurfaceLoopSfx.Play("event:/char/madeline/water_move_shallow", null, 0f);
				}
			}
			else
			{
				this.swimSurfaceLoopSfx.Stop(true);
			}
			this.wasOnGround = this.onGround;
			this.windMovedUp = false;
		}

		private void CreateTrail()
		{
			Vector2 scale = new Vector2(Math.Abs(this.Sprite.Scale.X) * (float)this.Facing, this.Sprite.Scale.Y);
			if (this.Sprite.Mode == PlayerSpriteMode.MadelineAsBadeline)
			{
				TrailManager.Add(this, scale, this.wasDashB ? Player.NormalBadelineHairColor : Player.UsedBadelineHairColor, 1f);
				return;
			}
			TrailManager.Add(this, scale, this.wasDashB ? Player.NormalHairColor : Player.UsedHairColor, 1f);
		}

		public void CleanUpTriggers()
		{
			if (this.triggersInside.Count > 0)
			{
				foreach (Trigger trigger in this.triggersInside)
				{
					trigger.OnLeave(this);
					trigger.Triggered = false;
				}
				this.triggersInside.Clear();
			}
		}

		private void UpdateChaserStates()
		{
			while (this.ChaserStates.Count > 0 && base.Scene.TimeActive - this.ChaserStates[0].TimeStamp > 4f)
			{
				this.ChaserStates.RemoveAt(0);
			}
			this.ChaserStates.Add(new Player.ChaserState(this));
			this.activeSounds.Clear();
		}

		private void StartHair()
		{
			if (this.startHairCalled)
			{
				return;
			}
			this.startHairCalled = true;
			this.Hair.Facing = this.Facing;
			this.Hair.Start();
			this.UpdateHair(true);
		}

		public void UpdateHair(bool applyGravity)
		{
			if (this.StateMachine.State == 19)
			{
				this.Hair.Color = this.Sprite.Color;
				applyGravity = false;
			}
			else if (this.Dashes == 0 && this.Dashes < this.MaxDashes)
			{
				if (this.Sprite.Mode == PlayerSpriteMode.MadelineAsBadeline)
				{
					this.Hair.Color = Color.Lerp(this.Hair.Color, Player.UsedBadelineHairColor, 6f * Engine.DeltaTime);
				}
				else
				{
					this.Hair.Color = Color.Lerp(this.Hair.Color, Player.UsedHairColor, 6f * Engine.DeltaTime);
				}
			}
			else
			{
				Color color;
				if (this.lastDashes != this.Dashes)
				{
					color = Player.FlashHairColor;
					this.hairFlashTimer = 0.12f;
				}
				else if (this.hairFlashTimer > 0f)
				{
					color = Player.FlashHairColor;
					this.hairFlashTimer -= Engine.DeltaTime;
				}
				else if (this.Sprite.Mode == PlayerSpriteMode.MadelineAsBadeline)
				{
					if (this.Dashes == 2)
					{
						color = Player.TwoDashesBadelineHairColor;
					}
					else
					{
						color = Player.NormalBadelineHairColor;
					}
				}
				else if (this.Dashes == 2)
				{
					color = Player.TwoDashesHairColor;
				}
				else
				{
					color = Player.NormalHairColor;
				}
				this.Hair.Color = color;
			}
			if (this.OverrideHairColor != null)
			{
				this.Hair.Color = this.OverrideHairColor.Value;
			}
			this.Hair.Facing = this.Facing;
			this.Hair.SimulateMotion = applyGravity;
			this.lastDashes = this.Dashes;
		}

		private void UpdateSprite()
		{
			this.Sprite.Scale.X = Calc.Approach(this.Sprite.Scale.X, 1f, 1.75f * Engine.DeltaTime);
			this.Sprite.Scale.Y = Calc.Approach(this.Sprite.Scale.Y, 1f, 1.75f * Engine.DeltaTime);
			if (this.InControl && this.Sprite.CurrentAnimationID != "throw" && this.StateMachine.State != 20 && this.StateMachine.State != 18 && this.StateMachine.State != 19 && this.StateMachine.State != 21)
			{
				if (this.StateMachine.State == 22)
				{
					this.Sprite.Play("fallFast", false, false);
				}
				else if (this.StateMachine.State == 10)
				{
					this.Sprite.Play("launch", false, false);
				}
				else if (this.StateMachine.State == 8)
				{
					this.Sprite.Play("pickup", false, false);
				}
				else if (this.StateMachine.State == 3)
				{
					if (Input.MoveY.Value > 0)
					{
						this.Sprite.Play("swimDown", false, false);
					}
					else if (Input.MoveY.Value < 0)
					{
						this.Sprite.Play("swimUp", false, false);
					}
					else
					{
						this.Sprite.Play("swimIdle", false, false);
					}
				}
				else if (this.StateMachine.State == 9)
				{
					if (this.Sprite.CurrentAnimationID != "dreamDashIn" && this.Sprite.CurrentAnimationID != "dreamDashLoop")
					{
						this.Sprite.Play("dreamDashIn", false, false);
					}
				}
				else if (this.Sprite.DreamDashing && this.Sprite.LastAnimationID != "dreamDashOut")
				{
					this.Sprite.Play("dreamDashOut", false, false);
				}
				else if (this.Sprite.CurrentAnimationID != "dreamDashOut")
				{
					if (this.DashAttacking)
					{
						if (this.onGround && this.DashDir.Y == 0f && !this.Ducking && this.Speed.X != 0f && this.moveX == -Math.Sign(this.Speed.X))
						{
							if (base.Scene.OnInterval(0.02f))
							{
								Dust.Burst(this.Position, -1.5707964f, 1, null);
							}
							this.Sprite.Play("skid", false, false);
						}
						else if (this.Ducking)
						{
							this.Sprite.Play("duck", false, false);
						}
						else
						{
							this.Sprite.Play("dash", false, false);
						}
					}
					else if (this.StateMachine.State == 1)
					{
						if (this.lastClimbMove < 0)
						{
							this.Sprite.Play("climbUp", false, false);
						}
						else if (this.lastClimbMove > 0)
						{
							this.Sprite.Play("wallslide", false, false);
						}
						else if (!base.CollideCheck<Solid>(this.Position + new Vector2((float)this.Facing, 6f)))
						{
							this.Sprite.Play("dangling", false, false);
						}
						else if (Input.MoveX == (float)(-(float)this.Facing))
						{
							if (this.Sprite.CurrentAnimationID != "climbLookBack")
							{
								this.Sprite.Play("climbLookBackStart", false, false);
							}
						}
						else
						{
							this.Sprite.Play("wallslide", false, false);
						}
					}
					else if (this.Ducking && this.StateMachine.State == 0)
					{
						this.Sprite.Play("duck", false, false);
					}
					else if (this.onGround)
					{
						this.fastJump = false;
						if (this.Holding == null && this.moveX != 0 && base.CollideCheck<Solid>(this.Position + Vector2.UnitX * (float)this.moveX) && !ClimbBlocker.EdgeCheck(this.level, this, this.moveX))
						{
							this.Sprite.Play("push", false, false);
						}
						else if (Math.Abs(this.Speed.X) <= 25f && this.moveX == 0)
						{
							if (this.Holding != null)
							{
								this.Sprite.Play("idle_carry", false, false);
							}
							else if (!base.Scene.CollideCheck<Solid>(this.Position + new Vector2((float)this.Facing, 2f)) && !base.Scene.CollideCheck<Solid>(this.Position + new Vector2((float)((int)this.Facing *4), 2f)) && !base.CollideCheck<JumpThru>(this.Position + new Vector2((float)((int)this.Facing *4), 2f)))
							{
								this.Sprite.Play("edge", false, false);
							}
							else if (!base.Scene.CollideCheck<Solid>(this.Position + new Vector2((float)(-(float)this.Facing), 2f)) && !base.Scene.CollideCheck<Solid>(this.Position + new Vector2((float)(-(int)this.Facing *4), 2f)) && !base.CollideCheck<JumpThru>(this.Position + new Vector2((float)(-(int)this.Facing *4), 2f)))
							{
								this.Sprite.Play("edgeBack", false, false);
							}
							else if (Input.MoveY.Value == -1)
							{
								if (this.Sprite.LastAnimationID != "lookUp")
								{
									this.Sprite.Play("lookUp", false, false);
								}
							}
							else if (this.Sprite.CurrentAnimationID != null && (!this.Sprite.CurrentAnimationID.Contains("idle") || (this.Sprite.CurrentAnimationID == "idle_carry" && this.Holding == null)))
							{
								this.Sprite.Play("idle", false, false);
							}
						}
						else if (this.Holding != null)
						{
							this.Sprite.Play("runSlow_carry", false, false);
						}
						else if (Math.Sign(this.Speed.X) == -this.moveX && this.moveX != 0)
						{
							if (Math.Abs(this.Speed.X) > 90f)
							{
								this.Sprite.Play("skid", false, false);
							}
							else if (this.Sprite.CurrentAnimationID != "skid")
							{
								this.Sprite.Play("flip", false, false);
							}
						}
						else if (this.windDirection.X != 0f && this.windTimeout > 0f && this.Facing == (Facings)(-Math.Sign(this.windDirection.X)))
						{
							this.Sprite.Play("runWind", false, false);
						}
						else if (!this.Sprite.Running || this.Sprite.CurrentAnimationID == "runWind" || (this.Sprite.CurrentAnimationID == "runSlow_carry" && this.Holding == null))
						{
							if (Math.Abs(this.Speed.X) < 45f)
							{
								this.Sprite.Play("runSlow", false, false);
							}
							else
							{
								this.Sprite.Play("runFast", false, false);
							}
						}
					}
					else if (this.wallSlideDir != 0 && this.Holding == null)
					{
						this.Sprite.Play("wallslide", false, false);
					}
					else if (this.Speed.Y < 0f)
					{
						if (this.Holding != null)
						{
							this.Sprite.Play("jumpSlow_carry", false, false);
						}
						else if (this.fastJump || Math.Abs(this.Speed.X) > 90f)
						{
							this.fastJump = true;
							this.Sprite.Play("jumpFast", false, false);
						}
						else
						{
							this.Sprite.Play("jumpSlow", false, false);
						}
					}
					else if (this.Holding != null)
					{
						this.Sprite.Play("fallSlow_carry", false, false);
					}
					else if (this.fastJump || this.Speed.Y >= 160f || this.level.InSpace)
					{
						this.fastJump = true;
						if (this.Sprite.LastAnimationID != "fallFast")
						{
							this.Sprite.Play("fallFast", false, false);
						}
					}
					else
					{
						this.Sprite.Play("fallSlow", false, false);
					}
				}
			}
			if (this.StateMachine.State != 11)
			{
				if (this.level.InSpace)
				{
					this.Sprite.Rate = 0.5f;
					return;
				}
				this.Sprite.Rate = 1f;
			}
		}

		public void CreateSplitParticles()
		{
			this.level.Particles.Emit(Player.P_Split, 16, base.Center, Vector2.One * 6f);
		}

		public Vector2 CameraTarget
		{
			get
			{
				Vector2 vector = default(Vector2);
				Vector2 vector2 = new Vector2(base.X - 160f, base.Y - 90f);
				if (this.StateMachine.State != 18)
				{
					vector2 += new Vector2(this.level.CameraOffset.X, this.level.CameraOffset.Y);
				}
				if (this.StateMachine.State == 19)
				{
					vector2.X += 0.2f * this.Speed.X;
					vector2.Y += 0.2f * this.Speed.Y;
				}
				else if (this.StateMachine.State == 5)
				{
					vector2.X += (float)(48 * Math.Sign(this.Speed.X));
					vector2.Y += (float)(48 * Math.Sign(this.Speed.Y));
				}
				else if (this.StateMachine.State == 10)
				{
					vector2.Y -= 64f;
				}
				else if (this.StateMachine.State == 18)
				{
					vector2.Y += 32f;
				}
				if (this.CameraAnchorLerp.Length() > 0f)
				{
					if (this.CameraAnchorIgnoreX && !this.CameraAnchorIgnoreY)
					{
						vector2.Y = MathHelper.Lerp(vector2.Y, this.CameraAnchor.Y, this.CameraAnchorLerp.Y);
					}
					else if (!this.CameraAnchorIgnoreX && this.CameraAnchorIgnoreY)
					{
						vector2.X = MathHelper.Lerp(vector2.X, this.CameraAnchor.X, this.CameraAnchorLerp.X);
					}
					else if (this.CameraAnchorLerp.X == this.CameraAnchorLerp.Y)
					{
						vector2 = Vector2.Lerp(vector2, this.CameraAnchor, this.CameraAnchorLerp.X);
					}
					else
					{
						vector2.X = MathHelper.Lerp(vector2.X, this.CameraAnchor.X, this.CameraAnchorLerp.X);
						vector2.Y = MathHelper.Lerp(vector2.Y, this.CameraAnchor.Y, this.CameraAnchorLerp.Y);
					}
				}
				if (this.EnforceLevelBounds)
				{
					vector.X = MathHelper.Clamp(vector2.X, (float)this.level.Bounds.Left, (float)(this.level.Bounds.Right - 320));
					vector.Y = MathHelper.Clamp(vector2.Y, (float)this.level.Bounds.Top, (float)(this.level.Bounds.Bottom - 180));
				}
				else
				{
					vector = vector2;
				}
				if (this.level.CameraLockMode != Level.CameraLockModes.None)
				{
					CameraLocker component = base.Scene.Tracker.GetComponent<CameraLocker>();
					if (this.level.CameraLockMode != Level.CameraLockModes.BoostSequence)
					{
						vector.X = Math.Max(vector.X, this.level.Camera.X);
						if (component != null)
						{
							vector.X = Math.Min(vector.X, Math.Max((float)this.level.Bounds.Left, component.Entity.X - component.MaxXOffset));
						}
					}
					if (this.level.CameraLockMode == Level.CameraLockModes.FinalBoss)
					{
						vector.Y = Math.Max(vector.Y, this.level.Camera.Y);
						if (component != null)
						{
							vector.Y = Math.Min(vector.Y, Math.Max((float)this.level.Bounds.Top, component.Entity.Y - component.MaxYOffset));
						}
					}
					else if (this.level.CameraLockMode == Level.CameraLockModes.BoostSequence)
					{
						this.level.CameraUpwardMaxY = Math.Min(this.level.Camera.Y + 180f, this.level.CameraUpwardMaxY);
						vector.Y = Math.Min(vector.Y, this.level.CameraUpwardMaxY);
						if (component != null)
						{
							vector.Y = Math.Max(vector.Y, Math.Min((float)(this.level.Bounds.Bottom - 180), component.Entity.Y - component.MaxYOffset));
						}
					}
				}
				foreach (Entity entity in base.Scene.Tracker.GetEntities<Killbox>())
				{
					if (entity.Collidable && base.Top < entity.Bottom && base.Right > entity.Left && base.Left < entity.Right)
					{
						vector.Y = Math.Min(vector.Y, entity.Top - 180f);
					}
				}
				return vector;
			}
		}

		public bool GetChasePosition(float sceneTime, float timeAgo, out Player.ChaserState chaseState)
		{
			if (!this.Dead)
			{
				bool flag = false;
				foreach (Player.ChaserState chaserState in this.ChaserStates)
				{
					float num = sceneTime - chaserState.TimeStamp;
					if (num <= timeAgo)
					{
						if (flag || timeAgo - num < 0.02f)
						{
							chaseState = chaserState;
							return true;
						}
						chaseState = default(Player.ChaserState);
						return false;
					}
					else
					{
						flag = true;
					}
				}
			}
			chaseState = default(Player.ChaserState);
			return false;
		}

		public bool CanRetry
		{
			get
			{
				int state = this.StateMachine.State;
				return state - 12 > 3 && state != 18 && state != 25;
			}
		}

		public bool TimePaused
		{
			get
			{
				if (this.Dead)
				{
					return true;
				}
				int state = this.StateMachine.State;
				return state == 10 || state - 12 <= 3 || state == 25;
			}
		}

		public bool InControl
		{
			get
			{
				switch (this.StateMachine.State)
				{
				case 11:
				case 12:
				case 13:
				case 14:
				case 15:
				case 16:
				case 17:
				case 23:
				case 25:
					return false;
				default:
					return true;
				}
			}
		}

		public PlayerInventory Inventory
		{
			get
			{
				if (this.level != null && this.level.Session != null)
				{
					return this.level.Session.Inventory;
				}
				return PlayerInventory.Default;
			}
		}

		public void OnTransition()
		{
			this.wallSlideTimer = 1.2f;
			this.jumpGraceTimer = 0f;
			this.forceMoveXTimer = 0f;
			this.ChaserStates.Clear();
			this.RefillDash();
			this.RefillStamina();
			this.Leader.TransferFollowers();
		}

		public bool TransitionTo(Vector2 target, Vector2 direction)
		{
			base.MoveTowardsX(target.X, 60f * Engine.DeltaTime, null);
			base.MoveTowardsY(target.Y, 60f * Engine.DeltaTime, null);
			this.UpdateHair(false);
			this.UpdateCarry();
			if (this.Position == target)
			{
				base.ZeroRemainderX();
				base.ZeroRemainderY();
				this.Speed.X = (float)((int)Math.Round((double)this.Speed.X));
				this.Speed.Y = (float)((int)Math.Round((double)this.Speed.Y));
				return true;
			}
			return false;
		}

		public void BeforeSideTransition()
		{
		}

		public void BeforeDownTransition()
		{
			if (this.StateMachine.State != 5 && this.StateMachine.State != 18 && this.StateMachine.State != 19)
			{
				this.StateMachine.State = 0;
				this.Speed.Y = Math.Max(0f, this.Speed.Y);
				this.AutoJump = false;
				this.varJumpTimer = 0f;
			}
			foreach (Entity entity in base.Scene.Tracker.GetEntities<Platform>())
			{
				if (!(entity is SolidTiles) && base.CollideCheckOutside(entity, this.Position + Vector2.UnitY * base.Height))
				{
					entity.Collidable = false;
				}
			}
		}

		public void BeforeUpTransition()
		{
			this.Speed.X = 0f;
			if (this.StateMachine.State != 5 && this.StateMachine.State != 18 && this.StateMachine.State != 19)
			{
				this.varJumpSpeed = (this.Speed.Y = -105f);
				if (this.StateMachine.State == 10)
				{
					this.StateMachine.State = 13;
				}
				else
				{
					this.StateMachine.State = 0;
				}
				this.AutoJump = true;
				this.AutoJumpTimer = 0f;
				this.varJumpTimer = 0.2f;
			}
			this.dashCooldownTimer = 0.2f;
		}

		public bool OnSafeGround { get; private set; }

		public bool LoseShards
		{
			get
			{
				return this.onGround;
			}
		}

		private bool LaunchedBoostCheck()
		{
			if (this.LiftBoost.LengthSquared() >= 10000f && this.Speed.LengthSquared() >= 48400f)
			{
				this.launched = true;
				return true;
			}
			this.launched = false;
			return false;
		}

		public void HiccupJump()
		{
			switch (this.StateMachine.State)
			{
			default:
				this.StateMachine.State = 0;
				this.Speed.X = Calc.Approach(this.Speed.X, 0f, 40f);
				if (this.Speed.Y > -60f)
				{
					this.varJumpSpeed = (this.Speed.Y = -60f);
					this.varJumpTimer = 0.15f;
					this.AutoJump = true;
					this.AutoJumpTimer = 0f;
					if (this.jumpGraceTimer > 0f)
					{
						this.jumpGraceTimer = 0.6f;
					}
				}
				this.sweatSprite.Play("jump", true, false);
				break;
			case 1:
				this.StateMachine.State = 0;
				this.varJumpSpeed = (this.Speed.Y = -60f);
				this.varJumpTimer = 0.15f;
				this.Speed.X = 130f * (float)(-(float)this.Facing);
				this.AutoJump = true;
				this.AutoJumpTimer = 0f;
				this.sweatSprite.Play("jump", true, false);
				break;
			case 4:
			case 7:
			case 22:
				this.sweatSprite.Play("jump", true, false);
				break;
			case 5:
			case 9:
				if (this.Speed.X < 0f || (this.Speed.X == 0f && this.Speed.Y < 0f))
				{
					this.Speed = Calc.Rotate(this.Speed, 0.17453292f);
				}
				else
				{
					this.Speed = Calc.Rotate(this.Speed, -0.17453292f);
				}
				break;
			case 10:
			case 11:
			case 12:
			case 13:
			case 14:
			case 15:
			case 16:
			case 17:
			case 18:
			case 21:
			case 24:
				return;
			case 19:
				if (this.Speed.X > 0f)
				{
					this.Speed = Calc.Rotate(this.Speed, 0.6981317f);
				}
				else
				{
					this.Speed = Calc.Rotate(this.Speed, -0.6981317f);
				}
				break;
			}
			Input.Rumble(RumbleStrength.Strong, RumbleLength.Short);
			this.Play(this.Ducking ? "event:/new_content/char/madeline/hiccup_ducking" : "event:/new_content/char/madeline/hiccup_standing", null, 0f);
		}

		public void Jump(bool particles = true, bool playSfx = true)
		{
			Input.Jump.ConsumeBuffer();
			this.jumpGraceTimer = 0f;
			this.varJumpTimer = 0.2f;
			this.AutoJump = false;
			this.dashAttackTimer = 0f;
			this.gliderBoostTimer = 0f;
			this.wallSlideTimer = 1.2f;
			this.wallBoostTimer = 0f;
			this.Speed.X = this.Speed.X + 40f * (float)this.moveX;
			this.Speed.Y = -105f;
			this.Speed += this.LiftBoost;
			this.varJumpSpeed = this.Speed.Y;
			this.LaunchedBoostCheck();
			if (playSfx)
			{
				if (this.launched)
				{
					this.Play("event:/char/madeline/jump_assisted", null, 0f);
				}
				if (this.dreamJump)
				{
					this.Play("event:/char/madeline/jump_dreamblock", null, 0f);
				}
				else
				{
					this.Play("event:/char/madeline/jump", null, 0f);
				}
			}
			this.Sprite.Scale = new Vector2(0.6f, 1.4f);
			if (particles)
			{
				int index = -1;
				Platform platformByPriority = SurfaceIndex.GetPlatformByPriority(base.CollideAll<Platform>(this.Position + Vector2.UnitY, this.temp));
				if (platformByPriority != null)
				{
					index = platformByPriority.GetLandSoundIndex(this);
				}
				Dust.Burst(base.BottomCenter, -1.5707964f, 4, this.DustParticleFromSurfaceIndex(index));
			}
			SaveData.Instance.TotalJumps++;
		}

		private void SuperJump()
		{
			Input.Jump.ConsumeBuffer();
			this.jumpGraceTimer = 0f;
			this.varJumpTimer = 0.2f;
			this.AutoJump = false;
			this.dashAttackTimer = 0f;
			this.gliderBoostTimer = 0f;
			this.wallSlideTimer = 1.2f;
			this.wallBoostTimer = 0f;
			this.Speed.X = 260f * (float)this.Facing;
			this.Speed.Y = -105f;
			this.Speed += this.LiftBoost;
			this.gliderBoostTimer = 0.55f;
			this.Play("event:/char/madeline/jump", null, 0f);
			if (this.Ducking)
			{
				this.Ducking = false;
				this.Speed.X = this.Speed.X * 1.25f;
				this.Speed.Y = this.Speed.Y * 0.5f;
				this.Play("event:/char/madeline/jump_superslide", null, 0f);
				this.gliderBoostDir = Calc.AngleToVector(-0.5890486f, 1f);
			}
			else
			{
				this.gliderBoostDir = Calc.AngleToVector(-0.7853982f, 1f);
				this.Play("event:/char/madeline/jump_super", null, 0f);
			}
			this.varJumpSpeed = this.Speed.Y;
			this.launched = true;
			this.Sprite.Scale = new Vector2(0.6f, 1.4f);
			int index = -1;
			Platform platformByPriority = SurfaceIndex.GetPlatformByPriority(base.CollideAll<Platform>(this.Position + Vector2.UnitY, this.temp));
			if (platformByPriority != null)
			{
				index = platformByPriority.GetLandSoundIndex(this);
			}
			Dust.Burst(base.BottomCenter, -1.5707964f, 4, this.DustParticleFromSurfaceIndex(index));
			SaveData.Instance.TotalJumps++;
		}

		private bool WallJumpCheck(int dir)
		{
			int num = 3;
			bool flag = this.DashAttacking && this.DashDir.X == 0f && this.DashDir.Y == -1f;
			if (flag)
			{
				Spikes.Directions directions;
				if (dir > 0)
				{
					directions = Spikes.Directions.Left;
				}
				else
				{
					directions = Spikes.Directions.Right;
				}
				foreach (Entity entity in this.level.Tracker.GetEntities<Spikes>())
				{
					Spikes spikes = (Spikes)entity;
					if (spikes.Direction == directions && base.CollideCheck(spikes, this.Position + Vector2.UnitX * (float)dir * 5f))
					{
						flag = false;
						break;
					}
				}
			}
			if (flag)
			{
				num = 5;
			}
			return this.ClimbBoundsCheck(dir) && !ClimbBlocker.EdgeCheck(this.level, this, dir * num) && base.CollideCheck<Solid>(this.Position + Vector2.UnitX * (float)dir * (float)num);
		}

		private void WallJump(int dir)
		{
			this.Ducking = false;
			Input.Jump.ConsumeBuffer();
			this.jumpGraceTimer = 0f;
			this.varJumpTimer = 0.2f;
			this.AutoJump = false;
			this.dashAttackTimer = 0f;
			this.gliderBoostTimer = 0f;
			this.wallSlideTimer = 1.2f;
			this.wallBoostTimer = 0f;
			this.lowFrictionStopTimer = 0.15f;
			if (this.Holding != null && this.Holding.SlowFall)
			{
				this.forceMoveX = dir;
				this.forceMoveXTimer = 0.26f;
			}
			else if (this.moveX != 0)
			{
				this.forceMoveX = dir;
				this.forceMoveXTimer = 0.16f;
			}
			if (base.LiftSpeed == Vector2.Zero)
			{
				Solid solid = base.CollideFirst<Solid>(this.Position + Vector2.UnitX * 3f * (float)(-(float)dir));
				if (solid != null)
				{
					base.LiftSpeed = solid.LiftSpeed;
				}
			}
			this.Speed.X = 130f * (float)dir;
			this.Speed.Y = -105f;
			this.Speed += this.LiftBoost;
			this.varJumpSpeed = this.Speed.Y;
			this.LaunchedBoostCheck();
			int num = -1;
			Platform platformByPriority = SurfaceIndex.GetPlatformByPriority(base.CollideAll<Platform>(this.Position - Vector2.UnitX * (float)dir * 4f, this.temp));
			if (platformByPriority != null)
			{
				num = platformByPriority.GetWallSoundIndex(this, -dir);
				this.Play("event:/char/madeline/landing", "surface_index", (float)num);
				if (platformByPriority is DreamBlock)
				{
					(platformByPriority as DreamBlock).FootstepRipple(this.Position + new Vector2((float)(dir * 3), -4f));
				}
			}
			this.Play((dir < 0) ? "event:/char/madeline/jump_wall_right" : "event:/char/madeline/jump_wall_left", null, 0f);
			this.Sprite.Scale = new Vector2(0.6f, 1.4f);
			if (dir == -1)
			{
				Dust.Burst(base.Center + Vector2.UnitX * 2f, -2.3561945f, 4, this.DustParticleFromSurfaceIndex(num));
			}
			else
			{
				Dust.Burst(base.Center + Vector2.UnitX * -2f, -0.7853982f, 4, this.DustParticleFromSurfaceIndex(num));
			}
			SaveData.Instance.TotalWallJumps++;
		}

		private void SuperWallJump(int dir)
		{
			this.Ducking = false;
			Input.Jump.ConsumeBuffer();
			this.jumpGraceTimer = 0f;
			this.varJumpTimer = 0.25f;
			this.AutoJump = false;
			this.dashAttackTimer = 0f;
			this.gliderBoostTimer = 0.55f;
			this.gliderBoostDir = -Vector2.UnitY;
			this.wallSlideTimer = 1.2f;
			this.wallBoostTimer = 0f;
			this.Speed.X = 170f * (float)dir;
			this.Speed.Y = -160f;
			this.Speed += this.LiftBoost;
			this.varJumpSpeed = this.Speed.Y;
			this.launched = true;
			this.Play((dir < 0) ? "event:/char/madeline/jump_wall_right" : "event:/char/madeline/jump_wall_left", null, 0f);
			this.Play("event:/char/madeline/jump_superwall", null, 0f);
			this.Sprite.Scale = new Vector2(0.6f, 1.4f);
			int index = -1;
			Platform platformByPriority = SurfaceIndex.GetPlatformByPriority(base.CollideAll<Platform>(this.Position - Vector2.UnitX * (float)dir * 4f, this.temp));
			if (platformByPriority != null)
			{
				index = platformByPriority.GetWallSoundIndex(this, dir);
			}
			if (dir == -1)
			{
				Dust.Burst(base.Center + Vector2.UnitX * 2f, -2.3561945f, 4, this.DustParticleFromSurfaceIndex(index));
			}
			else
			{
				Dust.Burst(base.Center + Vector2.UnitX * -2f, -0.7853982f, 4, this.DustParticleFromSurfaceIndex(index));
			}
			SaveData.Instance.TotalWallJumps++;
		}

		private void ClimbJump()
		{
			if (!this.onGround)
			{
				this.Stamina -= 27.5f;
				this.sweatSprite.Play("jump", true, false);
				Input.Rumble(RumbleStrength.Light, RumbleLength.Medium);
			}
			this.dreamJump = false;
			this.Jump(false, false);
			if (this.moveX == 0)
			{
				this.wallBoostDir = (int)(-(int)this.Facing);
				this.wallBoostTimer = 0.2f;
			}
			int index = -1;
			Platform platformByPriority = SurfaceIndex.GetPlatformByPriority(base.CollideAll<Platform>(this.Position - Vector2.UnitX * (float)this.Facing * 4f, this.temp));
			if (platformByPriority != null)
			{
				index = platformByPriority.GetWallSoundIndex(this, (int)this.Facing);
			}
			if (this.Facing == Facings.Right)
			{
				this.Play("event:/char/madeline/jump_climb_right", null, 0f);
				Dust.Burst(base.Center + Vector2.UnitX * 2f, -2.3561945f, 4, this.DustParticleFromSurfaceIndex(index));
				return;
			}
			this.Play("event:/char/madeline/jump_climb_left", null, 0f);
			Dust.Burst(base.Center + Vector2.UnitX * -2f, -0.7853982f, 4, this.DustParticleFromSurfaceIndex(index));
		}

		public void Bounce(float fromY)
		{
			if (this.StateMachine.State == 4 && this.CurrentBooster != null)
			{
				this.CurrentBooster.PlayerReleased();
				this.CurrentBooster = null;
			}
			Collider collider = base.Collider;
			base.Collider = this.normalHitbox;
			base.MoveVExact((int)(fromY - base.Bottom), null, null);
			if (!this.Inventory.NoRefills)
			{
				this.RefillDash();
			}
			this.RefillStamina();
			this.StateMachine.State = 0;
			this.jumpGraceTimer = 0f;
			this.varJumpTimer = 0.2f;
			this.AutoJump = true;
			this.AutoJumpTimer = 0.1f;
			this.dashAttackTimer = 0f;
			this.gliderBoostTimer = 0f;
			this.wallSlideTimer = 1.2f;
			this.wallBoostTimer = 0f;
			this.varJumpSpeed = (this.Speed.Y = -140f);
			this.launched = false;
			Input.Rumble(RumbleStrength.Light, RumbleLength.Medium);
			this.Sprite.Scale = new Vector2(0.6f, 1.4f);
			base.Collider = collider;
		}

		public void SuperBounce(float fromY)
		{
			if (this.StateMachine.State == 4 && this.CurrentBooster != null)
			{
				this.CurrentBooster.PlayerReleased();
				this.CurrentBooster = null;
			}
			Collider collider = base.Collider;
			base.Collider = this.normalHitbox;
			base.MoveV(fromY - base.Bottom, null, null);
			if (!this.Inventory.NoRefills)
			{
				this.RefillDash();
			}
			this.RefillStamina();
			this.StateMachine.State = 0;
			this.jumpGraceTimer = 0f;
			this.varJumpTimer = 0.2f;
			this.AutoJump = true;
			this.AutoJumpTimer = 0f;
			this.dashAttackTimer = 0f;
			this.gliderBoostTimer = 0f;
			this.wallSlideTimer = 1.2f;
			this.wallBoostTimer = 0f;
			this.Speed.X = 0f;
			this.varJumpSpeed = (this.Speed.Y = -185f);
			this.launched = false;
			this.level.DirectionalShake(-Vector2.UnitY, 0.1f);
			Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
			this.Sprite.Scale = new Vector2(0.5f, 1.5f);
			base.Collider = collider;
		}

		public bool SideBounce(int dir, float fromX, float fromY)
		{
			if (Math.Abs(this.Speed.X) > 240f && Math.Sign(this.Speed.X) == dir)
			{
				return false;
			}
			Collider collider = base.Collider;
			base.Collider = this.normalHitbox;
			base.MoveV(Calc.Clamp(fromY - base.Bottom, -4f, 4f), null, null);
			if (dir > 0)
			{
				base.MoveH(fromX - base.Left, null, null);
			}
			else if (dir < 0)
			{
				base.MoveH(fromX - base.Right, null, null);
			}
			if (!this.Inventory.NoRefills)
			{
				this.RefillDash();
			}
			this.RefillStamina();
			this.StateMachine.State = 0;
			this.jumpGraceTimer = 0f;
			this.varJumpTimer = 0.2f;
			this.AutoJump = true;
			this.AutoJumpTimer = 0f;
			this.dashAttackTimer = 0f;
			this.gliderBoostTimer = 0f;
			this.wallSlideTimer = 1.2f;
			this.forceMoveX = dir;
			this.forceMoveXTimer = 0.3f;
			this.wallBoostTimer = 0f;
			this.launched = false;
			this.Speed.X = 240f * (float)dir;
			this.varJumpSpeed = (this.Speed.Y = -140f);
			this.level.DirectionalShake(Vector2.UnitX * (float)dir, 0.1f);
			Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
			this.Sprite.Scale = new Vector2(1.5f, 0.5f);
			base.Collider = collider;
			return true;
		}

		public void Rebound(int direction = 0)
		{
			this.Speed.X = (float)direction * 120f;
			this.Speed.Y = -120f;
			this.varJumpSpeed = this.Speed.Y;
			this.varJumpTimer = 0.15f;
			this.AutoJump = true;
			this.AutoJumpTimer = 0f;
			this.dashAttackTimer = 0f;
			this.gliderBoostTimer = 0f;
			this.wallSlideTimer = 1.2f;
			this.wallBoostTimer = 0f;
			this.launched = false;
			this.lowFrictionStopTimer = 0.15f;
			this.forceMoveXTimer = 0f;
			this.StateMachine.State = 0;
		}

		public void ReflectBounce(Vector2 direction)
		{
			if (direction.X != 0f)
			{
				this.Speed.X = direction.X * 220f;
			}
			if (direction.Y != 0f)
			{
				this.Speed.Y = direction.Y * 220f;
			}
			this.AutoJumpTimer = 0f;
			this.dashAttackTimer = 0f;
			this.gliderBoostTimer = 0f;
			this.wallSlideTimer = 1.2f;
			this.wallBoostTimer = 0f;
			this.launched = false;
			this.dashAttackTimer = 0f;
			this.gliderBoostTimer = 0f;
			this.forceMoveXTimer = 0f;
			this.StateMachine.State = 0;
		}

		public int MaxDashes
		{
			get
			{
				if (SaveData.Instance.Assists.DashMode != Assists.DashModes.Normal && !this.level.InCutscene)
				{
					return 2;
				}
				return this.Inventory.Dashes;
			}
		}

		public bool RefillDash()
		{
			if (this.Dashes < this.MaxDashes)
			{
				this.Dashes = this.MaxDashes;
				return true;
			}
			return false;
		}

		public bool UseRefill(bool twoDashes)
		{
			int num = this.MaxDashes;
			if (twoDashes)
			{
				num = 2;
			}
			if (this.Dashes < num || this.Stamina < 20f)
			{
				this.Dashes = num;
				this.RefillStamina();
				return true;
			}
			return false;
		}

		public void RefillStamina()
		{
			this.Stamina = 110f;
		}

		public PlayerDeadBody Die(Vector2 direction, bool evenIfInvincible = false, bool registerDeathInStats = true)
		{
			Session session = this.level.Session;
			bool flag = !evenIfInvincible && SaveData.Instance.Assists.Invincible;
			if (!this.Dead && !flag && this.StateMachine.State != 18)
			{
				this.Stop(this.wallSlideSfx);
				if (registerDeathInStats)
				{
					session.Deaths++;
					session.DeathsInCurrentLevel++;
					SaveData.Instance.AddDeath(session.Area);
				}
				Strawberry goldenStrawb = null;
				foreach (Follower follower in this.Leader.Followers)
				{
					if (follower.Entity is Strawberry && (follower.Entity as Strawberry).Golden && !(follower.Entity as Strawberry).Winged)
					{
						goldenStrawb = (follower.Entity as Strawberry);
					}
				}
				this.Dead = true;
				this.Leader.LoseFollowers();
				base.Depth = -1000000;
				this.Speed = Vector2.Zero;
				this.StateMachine.Locked = true;
				this.Collidable = false;
				this.Drop();
				if (this.LastBooster != null)
				{
					this.LastBooster.PlayerDied();
				}
				this.level.InCutscene = false;
				this.level.Shake(0.3f);
				Input.Rumble(RumbleStrength.Light, RumbleLength.Medium);
				PlayerDeadBody playerDeadBody = new PlayerDeadBody(this, direction);
				if (goldenStrawb != null)
				{
					playerDeadBody.HasGolden = true;
					playerDeadBody.DeathAction = delegate()
					{
						Engine.Scene = new LevelExit(LevelExit.Mode.GoldenBerryRestart, session, null)
						{
							GoldenStrawberryEntryLevel = goldenStrawb.ID.Level
						};
					};
				}
				base.Scene.Add(playerDeadBody);
				base.Scene.Remove(this);
				Lookout entity = base.Scene.Tracker.GetEntity<Lookout>();
				if (entity != null)
				{
					entity.StopInteracting();
				}
				return playerDeadBody;
			}
			return null;
		}

		private Vector2 LiftBoost
		{
			get
			{
				Vector2 liftSpeed = base.LiftSpeed;
				if (Math.Abs(liftSpeed.X) > 250f)
				{
					liftSpeed.X = 250f * (float)Math.Sign(liftSpeed.X);
				}
				if (liftSpeed.Y > 0f)
				{
					liftSpeed.Y = 0f;
				}
				else if (liftSpeed.Y < -130f)
				{
					liftSpeed.Y = -130f;
				}
				return liftSpeed;
			}
		}

		public bool Ducking
		{
			get
			{
				return base.Collider == this.duckHitbox || base.Collider == this.duckHurtbox;
			}
			set
			{
				if (value)
				{
					base.Collider = this.duckHitbox;
					this.hurtbox = this.duckHurtbox;
					return;
				}
				base.Collider = this.normalHitbox;
				this.hurtbox = this.normalHurtbox;
			}
		}

		public bool CanUnDuck
		{
			get
			{
				if (!this.Ducking)
				{
					return true;
				}
				Collider collider = base.Collider;
				base.Collider = this.normalHitbox;
				bool result = !base.CollideCheck<Solid>();
				base.Collider = collider;
				return result;
			}
		}

		public bool CanUnDuckAt(Vector2 at)
		{
			Vector2 position = this.Position;
			this.Position = at;
			bool canUnDuck = this.CanUnDuck;
			this.Position = position;
			return canUnDuck;
		}

		public bool DuckFreeAt(Vector2 at)
		{
			Vector2 position = this.Position;
			Collider collider = base.Collider;
			this.Position = at;
			base.Collider = this.duckHitbox;
			bool result = !base.CollideCheck<Solid>();
			this.Position = position;
			base.Collider = collider;
			return result;
		}

		private void Duck()
		{
			base.Collider = this.duckHitbox;
		}

		private void UnDuck()
		{
			base.Collider = this.normalHitbox;
		}

		public Holdable Holding { get; set; }

		public void UpdateCarry()
		{
			if (this.Holding != null)
			{
				if (this.Holding.Scene == null)
				{
					this.Holding = null;
					return;
				}
				this.Holding.Carry(this.Position + this.carryOffset + Vector2.UnitY * this.Sprite.CarryYOffset);
			}
		}

		public void Swat(int dir)
		{
			if (this.Holding != null)
			{
				this.Holding.Release(new Vector2(0.8f * (float)dir, -0.25f));
				this.Holding = null;
			}
		}

		private bool Pickup(Holdable pickup)
		{
			if (pickup.Pickup(this))
			{
				this.Ducking = false;
				this.Holding = pickup;
				this.minHoldTimer = 0.35f;
				return true;
			}
			return false;
		}

		public void Throw()
		{
			if (this.Holding != null)
			{
				if (Input.MoveY.Value == 1)
				{
					this.Drop();
				}
				else
				{
					Input.Rumble(RumbleStrength.Strong, RumbleLength.Short);
					this.Holding.Release(Vector2.UnitX * (float)this.Facing);
					this.Speed.X = this.Speed.X + 80f * (float)(-(float)this.Facing);
					this.Play("event:/char/madeline/crystaltheo_throw", null, 0f);
					this.Sprite.Play("throw", false, false);
				}
				this.Holding = null;
			}
		}

		public void Drop()
		{
			if (this.Holding != null)
			{
				Input.Rumble(RumbleStrength.Light, RumbleLength.Short);
				this.Holding.Release(Vector2.Zero);
				this.Holding = null;
			}
		}

		public void StartJumpGraceTime()
		{
			this.jumpGraceTimer = 0.1f;
		}

		public override bool IsRiding(Solid solid)
		{
			if (this.StateMachine.State == 23)
			{
				return false;
			}
			if (this.StateMachine.State == 9)
			{
				return base.CollideCheck(solid);
			}
			if (this.StateMachine.State == 1 || this.StateMachine.State == 6)
			{
				return base.CollideCheck(solid, this.Position + Vector2.UnitX * (float)this.Facing);
			}
			if (this.climbTriggerDir != 0)
			{
				return base.CollideCheck(solid, this.Position + Vector2.UnitX * (float)this.climbTriggerDir);
			}
			return base.IsRiding(solid);
		}

		public override bool IsRiding(JumpThru jumpThru)
		{
			return this.StateMachine.State != 9 && (this.StateMachine.State != 1 && this.Speed.Y >= 0f) && base.IsRiding(jumpThru);
		}

		public bool BounceCheck(float y)
		{
			return base.Bottom <= y + 3f;
		}

		public void PointBounce(Vector2 from)
		{
			if (this.StateMachine.State == 2)
			{
				this.StateMachine.State = 0;
			}
			if (this.StateMachine.State == 4 && this.CurrentBooster != null)
			{
				this.CurrentBooster.PlayerReleased();
			}
			this.RefillDash();
			this.RefillStamina();
			Vector2 vector = (base.Center - from).SafeNormalize();
			if (vector.Y > -0.2f && vector.Y <= 0.4f)
			{
				vector.Y = -0.2f;
			}
			this.Speed = vector * 220f;
			this.Speed.X = this.Speed.X * 1.5f;
			if (Math.Abs(this.Speed.X) < 100f)
			{
				if (this.Speed.X == 0f)
				{
					this.Speed.X = (float)(-(float)this.Facing) * 100f;
					return;
				}
				this.Speed.X = (float)Math.Sign(this.Speed.X) * 100f;
			}
		}

		private void WindMove(Vector2 move)
		{
			if (!this.JustRespawned && this.noWindTimer <= 0f && this.InControl && this.StateMachine.State != 4 && this.StateMachine.State != 2 && this.StateMachine.State != 10)
			{
				if (move.X != 0f && this.StateMachine.State != 1)
				{
					this.windTimeout = 0.2f;
					this.windDirection.X = (float)Math.Sign(move.X);
					if (!base.CollideCheck<Solid>(this.Position + Vector2.UnitX * (float)(-(float)Math.Sign(move.X)) * 3f))
					{
						if (this.Ducking && this.onGround)
						{
							move.X *= 0f;
						}
						if (move.X < 0f)
						{
							move.X = Math.Max(move.X, (float)this.level.Bounds.Left - (base.ExactPosition.X + base.Collider.Left));
						}
						else
						{
							move.X = Math.Min(move.X, (float)this.level.Bounds.Right - (base.ExactPosition.X + base.Collider.Right));
						}
						base.MoveH(move.X, null, null);
					}
				}
				if (move.Y != 0f)
				{
					this.windTimeout = 0.2f;
					this.windDirection.Y = (float)Math.Sign(move.Y);
					if (base.Bottom > (float)this.level.Bounds.Top && (this.Speed.Y < 0f || !base.OnGround(1)))
					{
						if (this.StateMachine.State == 1)
						{
							if (move.Y <= 0f || this.climbNoMoveTimer > 0f)
							{
								return;
							}
							move.Y *= 0.4f;
						}
						if (move.Y < 0f)
						{
							this.windMovedUp = true;
						}
						base.MoveV(move.Y, null, null);
					}
				}
			}
		}

		private void OnCollideH(CollisionData data)
		{
			this.canCurveDash = false;
			if (this.StateMachine.State == 19)
			{
				if (this.starFlyTimer < 0.2f)
				{
					this.Speed.X = 0f;
					return;
				}
				this.Play("event:/game/06_reflection/feather_state_bump", null, 0f);
				Input.Rumble(RumbleStrength.Light, RumbleLength.Medium);
				this.Speed.X = this.Speed.X * -0.5f;
				return;
			}
			else
			{
				if (this.StateMachine.State == 9)
				{
					return;
				}
				if (this.DashAttacking && data.Hit != null && data.Hit.OnDashCollide != null && data.Direction.X == (float)Math.Sign(this.DashDir.X))
				{
					DashCollisionResults dashCollisionResults = data.Hit.OnDashCollide(this, data.Direction);
					if (dashCollisionResults == DashCollisionResults.NormalOverride)
					{
						dashCollisionResults = DashCollisionResults.NormalCollision;
					}
					else if (this.StateMachine.State == 5)
					{
						dashCollisionResults = DashCollisionResults.Ignore;
					}
					if (dashCollisionResults == DashCollisionResults.Rebound)
					{
						this.Rebound(-Math.Sign(this.Speed.X));
						return;
					}
					if (dashCollisionResults == DashCollisionResults.Bounce)
					{
						this.ReflectBounce(new Vector2((float)(-(float)Math.Sign(this.Speed.X)), 0f));
						return;
					}
					if (dashCollisionResults == DashCollisionResults.Ignore)
					{
						return;
					}
				}
				if (this.StateMachine.State == 2 || this.StateMachine.State == 5)
				{
					if (this.onGround && this.DuckFreeAt(this.Position + Vector2.UnitX * (float)Math.Sign(this.Speed.X)))
					{
						this.Ducking = true;
						return;
					}
					if (this.Speed.Y == 0f && this.Speed.X != 0f)
					{
						for (int i = 1; i <= 4; i++)
						{
							for (int j = 1; j >= -1; j -= 2)
							{
								Vector2 vector = new Vector2((float)Math.Sign(this.Speed.X), (float)(i * j));
								Vector2 vector2 = this.Position + vector;
								if (!base.CollideCheck<Solid>(vector2) && base.CollideCheck<Solid>(vector2 - Vector2.UnitY * (float)j) && !this.DashCorrectCheck(vector))
								{
									base.MoveVExact(i * j, null, null);
									base.MoveHExact(Math.Sign(this.Speed.X), null, null);
									return;
								}
							}
						}
					}
				}
				if (this.DreamDashCheck(Vector2.UnitX * (float)Math.Sign(this.Speed.X)))
				{
					this.StateMachine.State = 9;
					this.dashAttackTimer = 0f;
					this.gliderBoostTimer = 0f;
					return;
				}
				if (this.wallSpeedRetentionTimer <= 0f)
				{
					this.wallSpeedRetained = this.Speed.X;
					this.wallSpeedRetentionTimer = 0.06f;
				}
				if (data.Hit != null && data.Hit.OnCollide != null)
				{
					data.Hit.OnCollide(data.Direction);
				}
				this.Speed.X = 0f;
				this.dashAttackTimer = 0f;
				this.gliderBoostTimer = 0f;
				if (this.StateMachine.State == 5)
				{
					Input.Rumble(RumbleStrength.Medium, RumbleLength.Short);
					this.level.Displacement.AddBurst(base.Center, 0.5f, 8f, 48f, 0.4f, Ease.QuadOut, Ease.QuadOut);
					this.StateMachine.State = 6;
				}
				return;
			}
		}

		private void OnCollideV(CollisionData data)
		{
			this.canCurveDash = false;
			if (this.StateMachine.State == 19)
			{
				if (this.starFlyTimer < 0.2f)
				{
					this.Speed.Y = 0f;
					return;
				}
				this.Play("event:/game/06_reflection/feather_state_bump", null, 0f);
				Input.Rumble(RumbleStrength.Light, RumbleLength.Medium);
				this.Speed.Y = this.Speed.Y * -0.5f;
				return;
			}
			else
			{
				if (this.StateMachine.State == 3)
				{
					this.Speed.Y = 0f;
					return;
				}
				if (this.StateMachine.State == 9)
				{
					return;
				}
				if (data.Hit != null && data.Hit.OnDashCollide != null)
				{
					if (this.DashAttacking && data.Direction.Y == (float)Math.Sign(this.DashDir.Y))
					{
						DashCollisionResults dashCollisionResults = data.Hit.OnDashCollide(this, data.Direction);
						if (this.StateMachine.State == 5)
						{
							dashCollisionResults = DashCollisionResults.Ignore;
						}
						if (dashCollisionResults == DashCollisionResults.Rebound)
						{
							this.Rebound(0);
							return;
						}
						if (dashCollisionResults == DashCollisionResults.Bounce)
						{
							this.ReflectBounce(new Vector2(0f, (float)(-(float)Math.Sign(this.Speed.Y))));
							return;
						}
						if (dashCollisionResults == DashCollisionResults.Ignore)
						{
							return;
						}
					}
					else if (this.StateMachine.State == 10)
					{
						data.Hit.OnDashCollide(this, data.Direction);
						return;
					}
				}
				if (this.Speed.Y > 0f)
				{
					if ((this.StateMachine.State == 2 || this.StateMachine.State == 5) && !this.dashStartedOnGround)
					{
						if (this.Speed.X <= 0.01f)
						{
							for (int i = -1; i >= -4; i--)
							{
								if (!base.OnGround(this.Position + new Vector2((float)i, 0f), 1))
								{
									base.MoveHExact(i, null, null);
									base.MoveVExact(1, null, null);
									return;
								}
							}
						}
						if (this.Speed.X >= -0.01f)
						{
							for (int j = 1; j <= 4; j++)
							{
								if (!base.OnGround(this.Position + new Vector2((float)j, 0f), 1))
								{
									base.MoveHExact(j, null, null);
									base.MoveVExact(1, null, null);
									return;
								}
							}
						}
					}
					if (this.DreamDashCheck(Vector2.UnitY * (float)Math.Sign(this.Speed.Y)))
					{
						this.StateMachine.State = 9;
						this.dashAttackTimer = 0f;
						this.gliderBoostTimer = 0f;
						return;
					}
					if (this.DashDir.X != 0f && this.DashDir.Y > 0f && this.Speed.Y > 0f)
					{
						this.DashDir.X = (float)Math.Sign(this.DashDir.X);
						this.DashDir.Y = 0f;
						this.Speed.Y = 0f;
						this.Speed.X = this.Speed.X * 1.2f;
						this.Ducking = true;
					}
					if (this.StateMachine.State != 1)
					{
						float amount = Math.Min(this.Speed.Y / 240f, 1f);
						this.Sprite.Scale.X = MathHelper.Lerp(1f, 1.6f, amount);
						this.Sprite.Scale.Y = MathHelper.Lerp(1f, 0.4f, amount);
						if (this.highestAirY < base.Y - 50f && this.Speed.Y >= 160f && Math.Abs(this.Speed.X) >= 90f)
						{
							this.Sprite.Play("runStumble", false, false);
						}
						Input.Rumble(RumbleStrength.Light, RumbleLength.Short);
						Platform platformByPriority = SurfaceIndex.GetPlatformByPriority(base.CollideAll<Platform>(this.Position + new Vector2(0f, 1f), this.temp));
						int num = -1;
						if (platformByPriority != null)
						{
							num = platformByPriority.GetLandSoundIndex(this);
							if (num >= 0 && !this.MuffleLanding)
							{
								this.Play((this.playFootstepOnLand > 0f) ? "event:/char/madeline/footstep" : "event:/char/madeline/landing", "surface_index", (float)num);
							}
							if (platformByPriority is DreamBlock)
							{
								(platformByPriority as DreamBlock).FootstepRipple(this.Position);
							}
							this.MuffleLanding = false;
						}
						if (this.Speed.Y >= 80f)
						{
							Dust.Burst(this.Position, new Vector2(0f, -1f).Angle(), 8, this.DustParticleFromSurfaceIndex(num));
						}
						this.playFootstepOnLand = 0f;
					}
				}
				else
				{
					if (this.Speed.Y < 0f)
					{
						int num2 = 4;
						if (this.DashAttacking && Math.Abs(this.Speed.X) < 0.01f)
						{
							num2 = 5;
						}
						if (this.Speed.X <= 0.01f)
						{
							for (int k = 1; k <= num2; k++)
							{
								if (!base.CollideCheck<Solid>(this.Position + new Vector2((float)(-(float)k), -1f)))
								{
									this.Position += new Vector2((float)(-(float)k), -1f);
									return;
								}
							}
						}
						if (this.Speed.X >= -0.01f)
						{
							for (int l = 1; l <= num2; l++)
							{
								if (!base.CollideCheck<Solid>(this.Position + new Vector2((float)l, -1f)))
								{
									this.Position += new Vector2((float)l, -1f);
									return;
								}
							}
						}
						if (this.varJumpTimer < 0.15f)
						{
							this.varJumpTimer = 0f;
						}
					}
					if (this.DreamDashCheck(Vector2.UnitY * (float)Math.Sign(this.Speed.Y)))
					{
						this.StateMachine.State = 9;
						this.dashAttackTimer = 0f;
						this.gliderBoostTimer = 0f;
						return;
					}
				}
				if (data.Hit != null && data.Hit.OnCollide != null)
				{
					data.Hit.OnCollide(data.Direction);
				}
				this.dashAttackTimer = 0f;
				this.gliderBoostTimer = 0f;
				this.Speed.Y = 0f;
				if (this.StateMachine.State == 5)
				{
					Input.Rumble(RumbleStrength.Medium, RumbleLength.Short);
					this.level.Displacement.AddBurst(base.Center, 0.5f, 8f, 48f, 0.4f, Ease.QuadOut, Ease.QuadOut);
					this.StateMachine.State = 6;
				}
				return;
			}
		}

		private bool DreamDashCheck(Vector2 dir)
		{
			if (this.Inventory.DreamDash && this.DashAttacking && (dir.X == (float)Math.Sign(this.DashDir.X) || dir.Y == (float)Math.Sign(this.DashDir.Y)))
			{
				DreamBlock dreamBlock = base.CollideFirst<DreamBlock>(this.Position + dir);
				if (dreamBlock != null)
				{
					if (base.CollideCheck<Solid, DreamBlock>(this.Position + dir))
					{
						Vector2 value = new Vector2(Math.Abs(dir.Y), Math.Abs(dir.X));
						bool flag;
						bool flag2;
						if (dir.X != 0f)
						{
							flag = (this.Speed.Y <= 0f);
							flag2 = (this.Speed.Y >= 0f);
						}
						else
						{
							flag = (this.Speed.X <= 0f);
							flag2 = (this.Speed.X >= 0f);
						}
						if (flag)
						{
							for (int i = -1; i >= -4; i--)
							{
								Vector2 at = this.Position + dir + value * (float)i;
								if (!base.CollideCheck<Solid, DreamBlock>(at))
								{
									this.Position += value * (float)i;
									this.dreamBlock = dreamBlock;
									return true;
								}
							}
						}
						if (flag2)
						{
							for (int j = 1; j <= 4; j++)
							{
								Vector2 at2 = this.Position + dir + value * (float)j;
								if (!base.CollideCheck<Solid, DreamBlock>(at2))
								{
									this.Position += value * (float)j;
									this.dreamBlock = dreamBlock;
									return true;
								}
							}
						}
						return false;
					}
					this.dreamBlock = dreamBlock;
					return true;
				}
			}
			return false;
		}

		public void OnBoundsH()
		{
			this.Speed.X = 0f;
			if (this.StateMachine.State == 5)
			{
				this.StateMachine.State = 0;
			}
		}

		public void OnBoundsV()
		{
			this.Speed.Y = 0f;
			if (this.StateMachine.State == 5)
			{
				this.StateMachine.State = 0;
			}
		}

		protected override void OnSquish(CollisionData data)
		{
			bool flag = false;
			if (!this.Ducking && this.StateMachine.State != 1)
			{
				flag = true;
				this.Ducking = true;
				data.Pusher.Collidable = true;
				if (!base.CollideCheck<Solid>())
				{
					data.Pusher.Collidable = false;
					return;
				}
				Vector2 position = this.Position;
				this.Position = data.TargetPosition;
				if (!base.CollideCheck<Solid>())
				{
					data.Pusher.Collidable = false;
					return;
				}
				this.Position = position;
				data.Pusher.Collidable = false;
			}
			if (!base.TrySquishWiggle(data, 3, 5))
			{
				bool evenIfInvincible = false;
				if (data.Pusher != null && data.Pusher.SquishEvenInAssistMode)
				{
					evenIfInvincible = true;
				}
				this.Die(Vector2.Zero, evenIfInvincible, true);
				return;
			}
			if (flag && this.CanUnDuck)
			{
				this.Ducking = false;
			}
		}

		private void NormalBegin()
		{
			this.maxFall = 160f;
		}

		private void NormalEnd()
		{
			this.wallBoostTimer = 0f;
			this.wallSpeedRetentionTimer = 0f;
			this.hopWaitX = 0;
		}

		public bool ClimbBoundsCheck(int dir)
		{
			return base.Left + (float)(dir * 2) >= (float)this.level.Bounds.Left && base.Right + (float)(dir * 2) < (float)this.level.Bounds.Right;
		}

		public void ClimbTrigger(int dir)
		{
			this.climbTriggerDir = dir;
		}

		public bool ClimbCheck(int dir, int yAdd = 0)
		{
			// NOTE (poda de movimento): agarrar parede virou upgrade (Abilities.WallClimb).
			// Todo caminho p/ o estado Climb (1) passa por aqui, entao um portao so basta.
			// Wall slide (ClimbBoundsCheck), wall jump (WallJumpCheck) e pegar Holdable
			// nao passam por aqui e continuam valendo.
			if (!Abilities.WallClimb)
			{
				return false;
			}
			return this.ClimbBoundsCheck(dir) && !ClimbBlocker.Check(base.Scene, this, this.Position + Vector2.UnitY * (float)yAdd + Vector2.UnitX * 2f * (float)this.Facing) && base.CollideCheck<Solid>(this.Position + new Vector2((float)(dir * 2), (float)yAdd));
		}

		private int NormalUpdate()
		{
			if (this.LiftBoost.Y < 0f && this.wasOnGround && !this.onGround && this.Speed.Y >= 0f)
			{
				this.Speed.Y = this.LiftBoost.Y;
			}
			if (this.Holding == null)
			{
				if (Input.GrabCheck && !this.IsTired && !this.Ducking)
				{
					foreach (Component component in base.Scene.Tracker.GetComponents<Holdable>())
					{
						Holdable holdable = (Holdable)component;
						if (holdable.Check(this) && this.Pickup(holdable))
						{
							return 8;
						}
					}
					if (this.Speed.Y < 0f || Math.Sign(this.Speed.X) == (int)(-(int)this.Facing))
					{
						goto IL_1BD;
					}
					if (this.ClimbCheck((int)this.Facing, 0))
					{
						this.Ducking = false;
						if (!SaveData.Instance.Assists.NoGrabbing)
						{
							return 1;
						}
						this.ClimbTrigger((int)this.Facing);
					}
					if (!SaveData.Instance.Assists.NoGrabbing && Input.MoveY < 1f && this.level.Wind.Y <= 0f)
					{
						for (int i = 1; i <= 2; i++)
						{
							if (!base.CollideCheck<Solid>(this.Position + Vector2.UnitY * (float)(-(float)i)) && this.ClimbCheck((int)this.Facing, -i))
							{
								base.MoveVExact(-i, null, null);
								this.Ducking = false;
								return 1;
							}
						}
					}
				}
				IL_1BD:
				if (this.CanDash)
				{
					this.Speed += this.LiftBoost;
					return this.StartDash();
				}
				if (this.Ducking)
				{
					if (this.onGround && Input.MoveY != 1f)
					{
						if (this.CanUnDuck)
						{
							this.Ducking = false;
							this.Sprite.Scale = new Vector2(0.8f, 1.2f);
						}
						else if (this.Speed.X == 0f)
						{
							for (int j = 4; j > 0; j--)
							{
								if (this.CanUnDuckAt(this.Position + Vector2.UnitX * (float)j))
								{
									base.MoveH(50f * Engine.DeltaTime, null, null);
									break;
								}
								if (this.CanUnDuckAt(this.Position - Vector2.UnitX * (float)j))
								{
									base.MoveH(-50f * Engine.DeltaTime, null, null);
									break;
								}
							}
						}
					}
				}
				else if (this.onGround && Input.MoveY == 1f && this.Speed.Y >= 0f)
				{
					this.Ducking = true;
					this.Sprite.Scale = new Vector2(1.4f, 0.6f);
				}
			}
			else
			{
				if (!Input.GrabCheck && this.minHoldTimer <= 0f)
				{
					this.Throw();
				}
				if (!this.Ducking && this.onGround && Input.MoveY == 1f && this.Speed.Y >= 0f && !this.holdCannotDuck)
				{
					this.Drop();
					this.Ducking = true;
					this.Sprite.Scale = new Vector2(1.4f, 0.6f);
				}
				else if (this.onGround && this.Ducking && this.Speed.Y >= 0f)
				{
					if (this.CanUnDuck)
					{
						this.Ducking = false;
					}
					else
					{
						this.Drop();
					}
				}
				else if (this.onGround && Input.MoveY != 1f && this.holdCannotDuck)
				{
					this.holdCannotDuck = false;
				}
			}
			if (this.Ducking && this.onGround)
			{
				this.Speed.X = Calc.Approach(this.Speed.X, 0f, 500f * Engine.DeltaTime);
			}
			else
			{
				float num = this.onGround ? 1f : 0.65f;
				if (this.onGround && this.level.CoreMode == Session.CoreModes.Cold)
				{
					num *= 0.3f;
				}
				if (SaveData.Instance.Assists.LowFriction && this.lowFrictionStopTimer <= 0f)
				{
					num *= (this.onGround ? 0.35f : 0.5f);
				}
				float num2;
				if (this.Holding != null && this.Holding.SlowRun)
				{
					num2 = 70f;
				}
				else if (this.Holding != null && this.Holding.SlowFall && !this.onGround)
				{
					num2 = 108.00001f;
					num *= 0.5f;
				}
				else
				{
					num2 = 90f;
				}
				if (this.level.InSpace)
				{
					num2 *= 0.6f;
				}
				if (Math.Abs(this.Speed.X) > num2 && Math.Sign(this.Speed.X) == this.moveX)
				{
					this.Speed.X = Calc.Approach(this.Speed.X, num2 * (float)this.moveX, 400f * num * Engine.DeltaTime);
				}
				else
				{
					this.Speed.X = Calc.Approach(this.Speed.X, num2 * (float)this.moveX, 1000f * num * Engine.DeltaTime);
				}
			}
			float num3 = 160f;
			float num4 = 240f;
			if (this.level.InSpace)
			{
				num3 *= 0.6f;
				num4 *= 0.6f;
			}
			if (this.Holding != null && this.Holding.SlowFall && this.forceMoveXTimer <= 0f)
			{
				if (Input.GliderMoveY == 1f)
				{
					num3 = 120f;
				}
				else if (this.windMovedUp && Input.GliderMoveY == -1f)
				{
					num3 = -32f;
				}
				else if (Input.GliderMoveY == -1f)
				{
					num3 = 24f;
				}
				else if (this.windMovedUp)
				{
					num3 = 0f;
				}
				else
				{
					num3 = 40f;
				}
				this.maxFall = Calc.Approach(this.maxFall, num3, 300f * Engine.DeltaTime);
			}
			else if (Input.MoveY == 1f && this.Speed.Y >= num3)
			{
				this.maxFall = Calc.Approach(this.maxFall, num4, 300f * Engine.DeltaTime);
				float num5 = num3 + (num4 - num3) * 0.5f;
				if (this.Speed.Y >= num5)
				{
					float amount = Math.Min(1f, (this.Speed.Y - num5) / (num4 - num5));
					this.Sprite.Scale.X = MathHelper.Lerp(1f, 0.5f, amount);
					this.Sprite.Scale.Y = MathHelper.Lerp(1f, 1.5f, amount);
				}
			}
			else
			{
				this.maxFall = Calc.Approach(this.maxFall, num3, 300f * Engine.DeltaTime);
			}
			if (!this.onGround)
			{
				float target = this.maxFall;
				if (this.Holding != null && this.Holding.SlowFall)
				{
					this.holdCannotDuck = (Input.MoveY == 1f);
				}
				if ((this.moveX == (int)this.Facing || (this.moveX == 0 && Input.GrabCheck)) && Input.MoveY.Value != 1)
				{
					if (this.Speed.Y >= 0f && this.wallSlideTimer > 0f && this.Holding == null && this.ClimbBoundsCheck((int)this.Facing) && base.CollideCheck<Solid>(this.Position + Vector2.UnitX * (float)this.Facing) && !ClimbBlocker.EdgeCheck(this.level, this, (int)this.Facing) && this.CanUnDuck)
					{
						this.Ducking = false;
						this.wallSlideDir = (int)this.Facing;
					}
					if (this.wallSlideDir != 0)
					{
						if (Input.GrabCheck)
						{
							this.ClimbTrigger(this.wallSlideDir);
						}
						if (this.wallSlideTimer > 0.6f && ClimbBlocker.Check(this.level, this, this.Position + Vector2.UnitX * (float)this.wallSlideDir))
						{
							this.wallSlideTimer = 0.6f;
						}
						target = MathHelper.Lerp(160f, 20f, this.wallSlideTimer / 1.2f);
						if (this.wallSlideTimer / 1.2f > 0.65f)
						{
							this.CreateWallSlideParticles(this.wallSlideDir);
						}
					}
				}
				float num6 = (Math.Abs(this.Speed.Y) < 40f && (Input.Jump.Check || this.AutoJump)) ? 0.5f : 1f;
				if (this.Holding != null && this.Holding.SlowFall && this.forceMoveXTimer <= 0f)
				{
					num6 *= 0.5f;
				}
				if (this.level.InSpace)
				{
					num6 *= 0.6f;
				}
				this.Speed.Y = Calc.Approach(this.Speed.Y, target, 900f * num6 * Engine.DeltaTime);
			}
			if (this.varJumpTimer > 0f)
			{
				if (this.AutoJump || Input.Jump.Check)
				{
					this.Speed.Y = Math.Min(this.Speed.Y, this.varJumpSpeed);
				}
				else
				{
					this.varJumpTimer = 0f;
				}
			}
			if (Input.Jump.Pressed && (TalkComponent.PlayerOver == null || !Input.Talk.Pressed))
			{
				if (this.jumpGraceTimer > 0f)
				{
					this.Jump(true, true);
				}
				else if (this.CanUnDuck)
				{
					bool canUnDuck = this.CanUnDuck;
					Water water;
					if (canUnDuck && this.WallJumpCheck(1))
					{
						// NOTE (poda de movimento): o climb jump (pulo reto colado na parede) sobe
						// parede acima sem precisar do estado Climb, entao ele tambem fica atras
						// de Abilities.WallClimb. Sem o upgrade, cai no WallJump normal.
						if (Abilities.WallClimb && this.Facing == Facings.Right && Input.GrabCheck && !SaveData.Instance.Assists.NoGrabbing && this.Stamina > 0f && this.Holding == null && !ClimbBlocker.Check(base.Scene, this, this.Position + Vector2.UnitX * 3f))
						{
							this.ClimbJump();
						}
						else if (this.DashAttacking && this.SuperWallJumpAngleCheck)
						{
							this.SuperWallJump(-1);
						}
						else
						{
							this.WallJump(-1);
						}
					}
					else if (canUnDuck && this.WallJumpCheck(-1))
					{
						if (Abilities.WallClimb && this.Facing == Facings.Left && Input.GrabCheck && !SaveData.Instance.Assists.NoGrabbing && this.Stamina > 0f && this.Holding == null && !ClimbBlocker.Check(base.Scene, this, this.Position + Vector2.UnitX * -3f))
						{
							this.ClimbJump();
						}
						else if (this.DashAttacking && this.SuperWallJumpAngleCheck)
						{
							this.SuperWallJump(1);
						}
						else
						{
							this.WallJump(1);
						}
					}
					else if ((water = base.CollideFirst<Water>(this.Position + Vector2.UnitY * 2f)) != null)
					{
						this.Jump(true, true);
						water.TopSurface.DoRipple(this.Position, 1f);
					}
				}
			}
			return 0;
		}

		public void CreateWallSlideParticles(int dir)
		{
			if (base.Scene.OnInterval(0.01f))
			{
				int index = -1;
				Platform platformByPriority = SurfaceIndex.GetPlatformByPriority(base.CollideAll<Platform>(this.Position + Vector2.UnitX * (float)dir * 4f, this.temp));
				if (platformByPriority != null)
				{
					index = platformByPriority.GetWallSoundIndex(this, dir);
				}
				ParticleType particleType = this.DustParticleFromSurfaceIndex(index);
				float num = (particleType == ParticleTypes.Dust) ? 5f : 2f;
				Vector2 vector = base.Center;
				if (dir == 1)
				{
					vector += new Vector2(num, 4f);
				}
				else
				{
					vector += new Vector2(-num, 4f);
				}
				Dust.Burst(vector, -1.5707964f, 1, particleType);
			}
		}

		private bool IsTired
		{
			get
			{
				return this.CheckStamina < 20f;
			}
		}

		private float CheckStamina
		{
			get
			{
				if (this.wallBoostTimer > 0f)
				{
					return this.Stamina + 27.5f;
				}
				return this.Stamina;
			}
		}

		private void PlaySweatEffectDangerOverride(string state)
		{
			if (this.Stamina <= 20f)
			{
				this.sweatSprite.Play("danger", false, false);
				return;
			}
			this.sweatSprite.Play(state, false, false);
		}

		private void ClimbBegin()
		{
			this.AutoJump = false;
			this.Speed.X = 0f;
			this.Speed.Y = this.Speed.Y * 0.2f;
			this.wallSlideTimer = 1.2f;
			this.climbNoMoveTimer = 0.1f;
			this.wallBoostTimer = 0f;
			this.lastClimbMove = 0;
			Input.Rumble(RumbleStrength.Medium, RumbleLength.Short);
			int num = 0;
			while (num < 2 && !base.CollideCheck<Solid>(this.Position + Vector2.UnitX * (float)this.Facing))
			{
				this.Position += Vector2.UnitX * (float)this.Facing;
				num++;
			}
			Platform platformByPriority = SurfaceIndex.GetPlatformByPriority(base.CollideAll<Solid>(this.Position + Vector2.UnitX * (float)this.Facing, this.temp));
			if (platformByPriority != null)
			{
				this.Play("event:/char/madeline/grab", "surface_index", (float)platformByPriority.GetWallSoundIndex(this, (int)this.Facing));
				if (platformByPriority is DreamBlock)
				{
					(platformByPriority as DreamBlock).FootstepRipple(this.Position + new Vector2((float)((int)this.Facing *3), -4f));
				}
			}
		}

		private void ClimbEnd()
		{
			if (this.conveyorLoopSfx != null)
			{
				this.conveyorLoopSfx.setParameterValue("end", 1f);
				this.conveyorLoopSfx.release();
				this.conveyorLoopSfx = null;
			}
			this.wallSpeedRetentionTimer = 0f;
			if (this.sweatSprite != null && this.sweatSprite.CurrentAnimationID != "jump")
			{
				this.sweatSprite.Play("idle", false, false);
			}
		}

		private int ClimbUpdate()
		{
			this.climbNoMoveTimer -= Engine.DeltaTime;
			if (this.onGround)
			{
				this.Stamina = 110f;
			}
			if (Input.Jump.Pressed && (!this.Ducking || this.CanUnDuck))
			{
				if (this.moveX == (int)(-(int)this.Facing))
				{
					this.WallJump((int)(-(int)this.Facing));
				}
				else
				{
					this.ClimbJump();
				}
				return 0;
			}
			if (this.CanDash)
			{
				this.Speed += this.LiftBoost;
				return this.StartDash();
			}
			if (!Input.GrabCheck)
			{
				this.Speed += this.LiftBoost;
				this.Play("event:/char/madeline/grab_letgo", null, 0f);
				return 0;
			}
			if (!base.CollideCheck<Solid>(this.Position + Vector2.UnitX * (float)this.Facing))
			{
				if (this.Speed.Y < 0f)
				{
					if (this.wallBoosting)
					{
						this.Speed += this.LiftBoost;
						this.Play("event:/char/madeline/grab_letgo", null, 0f);
					}
					else
					{
						this.ClimbHop();
					}
				}
				return 0;
			}
			WallBooster wallBooster = this.WallBoosterCheck();
			if (this.climbNoMoveTimer <= 0f && wallBooster != null)
			{
				this.wallBoosting = true;
				if (this.conveyorLoopSfx == null)
				{
					this.conveyorLoopSfx = Audio.Play("event:/game/09_core/conveyor_activate", this.Position, "end", 0f);
				}
				Audio.Position(this.conveyorLoopSfx, this.Position);
				this.Speed.Y = Calc.Approach(this.Speed.Y, -160f, 600f * Engine.DeltaTime);
				base.LiftSpeed = Vector2.UnitY * Math.Max(this.Speed.Y, -80f);
				Input.Rumble(RumbleStrength.Light, RumbleLength.Short);
			}
			else
			{
				this.wallBoosting = false;
				if (this.conveyorLoopSfx != null)
				{
					this.conveyorLoopSfx.setParameterValue("end", 1f);
					this.conveyorLoopSfx.release();
					this.conveyorLoopSfx = null;
				}
				float num = 0f;
				bool flag = false;
				if (this.climbNoMoveTimer <= 0f)
				{
					if (ClimbBlocker.Check(base.Scene, this, this.Position + Vector2.UnitX * (float)this.Facing))
					{
						flag = true;
					}
					else if (Input.MoveY.Value == -1)
					{
						num = -45f;
						if (base.CollideCheck<Solid>(this.Position - Vector2.UnitY) || (this.ClimbHopBlockedCheck() && this.SlipCheck(-1f)))
						{
							if (this.Speed.Y < 0f)
							{
								this.Speed.Y = 0f;
							}
							num = 0f;
							flag = true;
						}
						else if (this.SlipCheck(0f))
						{
							this.ClimbHop();
							return 0;
						}
					}
					else if (Input.MoveY.Value == 1)
					{
						num = 80f;
						if (this.onGround)
						{
							if (this.Speed.Y > 0f)
							{
								this.Speed.Y = 0f;
							}
							num = 0f;
						}
						else
						{
							this.CreateWallSlideParticles((int)this.Facing);
						}
					}
					else
					{
						flag = true;
					}
				}
				else
				{
					flag = true;
				}
				this.lastClimbMove = Math.Sign(num);
				if (flag && this.SlipCheck(0f))
				{
					num = 30f;
				}
				this.Speed.Y = Calc.Approach(this.Speed.Y, num, 900f * Engine.DeltaTime);
			}
			if (Input.MoveY.Value != 1 && this.Speed.Y > 0f && !base.CollideCheck<Solid>(this.Position + new Vector2((float)this.Facing, 1f)))
			{
				this.Speed.Y = 0f;
			}
			if (this.climbNoMoveTimer <= 0f)
			{
				if (this.lastClimbMove == -1)
				{
					this.Stamina -= 45.454544f * Engine.DeltaTime;
					if (this.Stamina <= 20f)
					{
						this.sweatSprite.Play("danger", false, false);
					}
					else if (this.sweatSprite.CurrentAnimationID != "climbLoop")
					{
						this.sweatSprite.Play("climb", false, false);
					}
					if (base.Scene.OnInterval(0.2f))
					{
						Input.Rumble(RumbleStrength.Climb, RumbleLength.Short);
					}
				}
				else
				{
					if (this.lastClimbMove == 0)
					{
						this.Stamina -= 10f * Engine.DeltaTime;
					}
					if (!this.onGround)
					{
						this.PlaySweatEffectDangerOverride("still");
						if (base.Scene.OnInterval(0.8f))
						{
							Input.Rumble(RumbleStrength.Climb, RumbleLength.Short);
						}
					}
					else
					{
						this.PlaySweatEffectDangerOverride("idle");
					}
				}
			}
			else
			{
				this.PlaySweatEffectDangerOverride("idle");
			}
			if (this.Stamina <= 0f)
			{
				this.Speed += this.LiftBoost;
				return 0;
			}
			return 1;
		}

		private WallBooster WallBoosterCheck()
		{
			if (ClimbBlocker.Check(base.Scene, this, this.Position + Vector2.UnitX * (float)this.Facing))
			{
				return null;
			}
			foreach (Entity entity in base.Scene.Tracker.GetEntities<WallBooster>())
			{
				WallBooster wallBooster = (WallBooster)entity;
				if (wallBooster.Facing == this.Facing && base.CollideCheck(wallBooster))
				{
					return wallBooster;
				}
			}
			return null;
		}

		private void ClimbHop()
		{
			this.climbHopSolid = base.CollideFirst<Solid>(this.Position + Vector2.UnitX * (float)this.Facing);
			this.playFootstepOnLand = 0.5f;
			if (this.climbHopSolid != null)
			{
				this.climbHopSolidPosition = this.climbHopSolid.Position;
				this.hopWaitX = (int)this.Facing;
				this.hopWaitXSpeed = (float)this.Facing * 100f;
			}
			else
			{
				this.hopWaitX = 0;
				this.Speed.X = (float)this.Facing * 100f;
			}
			this.lowFrictionStopTimer = 0.15f;
			this.Speed.Y = Math.Min(this.Speed.Y, -120f);
			this.forceMoveX = 0;
			this.forceMoveXTimer = 0.2f;
			this.fastJump = false;
			this.noWindTimer = 0.3f;
			this.Play("event:/char/madeline/climb_ledge", null, 0f);
		}

		private bool SlipCheck(float addY = 0f)
		{
			Vector2 vector;
			if (this.Facing == Facings.Right)
			{
				vector = base.TopRight + Vector2.UnitY * (4f + addY);
			}
			else
			{
				vector = base.TopLeft - Vector2.UnitX + Vector2.UnitY * (4f + addY);
			}
			return !base.Scene.CollideCheck<Solid>(vector) && !base.Scene.CollideCheck<Solid>(vector + Vector2.UnitY * (-4f + addY));
		}

		private bool ClimbHopBlockedCheck()
		{
			using (List<Follower>.Enumerator enumerator = this.Leader.Followers.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					if (enumerator.Current.Entity is StrawberrySeed)
					{
						return true;
					}
				}
			}
			using (List<Component>.Enumerator enumerator2 = base.Scene.Tracker.GetComponents<LedgeBlocker>().GetEnumerator())
			{
				while (enumerator2.MoveNext())
				{
					if (((LedgeBlocker)enumerator2.Current).HopBlockCheck(this))
					{
						return true;
					}
				}
			}
			return base.CollideCheck<Solid>(this.Position - Vector2.UnitY * 6f);
		}

		private bool JumpThruBoostBlockedCheck()
		{
			using (List<Component>.Enumerator enumerator = base.Scene.Tracker.GetComponents<LedgeBlocker>().GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					if (((LedgeBlocker)enumerator.Current).JumpThruBoostCheck(this))
					{
						return true;
					}
				}
			}
			return false;
		}

		private bool DashCorrectCheck(Vector2 add)
		{
			Vector2 position = this.Position;
			Collider collider = base.Collider;
			this.Position += add;
			base.Collider = this.hurtbox;
			using (List<Component>.Enumerator enumerator = base.Scene.Tracker.GetComponents<LedgeBlocker>().GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					if (((LedgeBlocker)enumerator.Current).DashCorrectCheck(this))
					{
						this.Position = position;
						base.Collider = collider;
						return true;
					}
				}
			}
			this.Position = position;
			base.Collider = collider;
			return false;
		}

		public int StartDash()
		{
			this.wasDashB = (this.Dashes == 2);
			this.Dashes = Math.Max(0, this.Dashes - 1);
			this.demoDashed = Input.CrouchDashPressed;
			Input.Dash.ConsumeBuffer();
			Input.CrouchDash.ConsumeBuffer();
			return 2;
		}

		public bool DashAttacking
		{
			get
			{
				return this.dashAttackTimer > 0f || this.StateMachine.State == 5;
			}
		}

		public bool CanDash
		{
			get
			{
				return (Input.CrouchDashPressed || Input.DashPressed) && this.dashCooldownTimer <= 0f && this.Dashes > 0 && (TalkComponent.PlayerOver == null || !Input.Talk.Pressed) && (this.LastBooster == null || !this.LastBooster.Ch9HubTransition || !this.LastBooster.BoostingPlayer);
			}
		}

		public bool StartedDashing { get; private set; }

		private void CallDashEvents()
		{
			if (!this.calledDashEvents)
			{
				this.calledDashEvents = true;
				if (this.CurrentBooster == null)
				{
					SaveData.Instance.TotalDashes++;
					this.level.Session.Dashes++;
					Stats.Increment(Stat.DASHES, 1);
					bool flag = this.DashDir.Y < 0f || (this.DashDir.Y == 0f && this.DashDir.X > 0f);
					if (this.DashDir == Vector2.Zero)
					{
						flag = (this.Facing == Facings.Right);
					}
					if (flag)
					{
						if (this.wasDashB)
						{
							this.Play("event:/char/madeline/dash_pink_right", null, 0f);
						}
						else
						{
							this.Play("event:/char/madeline/dash_red_right", null, 0f);
						}
					}
					else if (this.wasDashB)
					{
						this.Play("event:/char/madeline/dash_pink_left", null, 0f);
					}
					else
					{
						this.Play("event:/char/madeline/dash_red_left", null, 0f);
					}
					if (this.SwimCheck())
					{
						this.Play("event:/char/madeline/water_dash_gen", null, 0f);
					}
					using (List<Component>.Enumerator enumerator = base.Scene.Tracker.GetComponents<DashListener>().GetEnumerator())
					{
						while (enumerator.MoveNext())
						{
							Component component = enumerator.Current;
							DashListener dashListener = (DashListener)component;
							if (dashListener.OnDash != null)
							{
								dashListener.OnDash(this.DashDir);
							}
						}
						return;
					}
				}
				this.CurrentBooster.PlayerBoosted(this, this.DashDir);
				this.CurrentBooster = null;
			}
		}

		private void DashBegin()
		{
			this.calledDashEvents = false;
			this.dashStartedOnGround = this.onGround;
			this.launched = false;
			this.canCurveDash = true;
			if (Engine.TimeRate > 0.25f)
			{
				Celeste.Freeze(0.05f);
			}
			this.dashCooldownTimer = 0.2f;
			this.dashRefillCooldownTimer = 0.1f;
			this.StartedDashing = true;
			this.wallSlideTimer = 1.2f;
			this.dashTrailTimer = 0f;
			this.dashTrailCounter = 0;
			if (!SaveData.Instance.Assists.DashAssist)
			{
				Input.Rumble(RumbleStrength.Strong, RumbleLength.Medium);
			}
			this.dashAttackTimer = 0.3f;
			this.gliderBoostTimer = 0.55f;
			if (SaveData.Instance.Assists.SuperDashing)
			{
				this.dashAttackTimer += 0.15f;
			}
			this.beforeDashSpeed = this.Speed;
			this.Speed = Vector2.Zero;
			this.DashDir = Vector2.Zero;
			if (!this.onGround && this.Ducking && this.CanUnDuck)
			{
				this.Ducking = false;
			}
			else if (!this.Ducking && (this.demoDashed || Input.MoveY.Value == 1))
			{
				this.Ducking = true;
			}
			this.DashAssistInit();
		}

		private void DashAssistInit()
		{
			if (SaveData.Instance.Assists.DashAssist && !this.demoDashed)
			{
				Input.LastAim = Vector2.UnitX * (float)this.Facing;
				Engine.DashAssistFreeze = true;
				Engine.DashAssistFreezePress = false;
				PlayerDashAssist playerDashAssist = base.Scene.Tracker.GetEntity<PlayerDashAssist>();
				if (playerDashAssist == null)
				{
					base.Scene.Add(playerDashAssist = new PlayerDashAssist());
				}
				playerDashAssist.Direction = Input.GetAimVector(this.Facing).Angle();
				playerDashAssist.Scale = 0f;
				playerDashAssist.Offset = ((this.CurrentBooster == null && this.StateMachine.PreviousState != 5) ? Vector2.Zero : new Vector2(0f, -4f));
			}
		}

		private void DashEnd()
		{
			this.CallDashEvents();
			this.demoDashed = false;
		}

		private int DashUpdate()
		{
			this.StartedDashing = false;
			if (this.dashTrailTimer > 0f)
			{
				this.dashTrailTimer -= Engine.DeltaTime;
				if (this.dashTrailTimer <= 0f)
				{
					this.CreateTrail();
					this.dashTrailCounter--;
					if (this.dashTrailCounter > 0)
					{
						this.dashTrailTimer = 0.1f;
					}
				}
			}
			if (SaveData.Instance.Assists.SuperDashing && this.canCurveDash && Input.Aim.Value != Vector2.Zero && this.Speed != Vector2.Zero)
			{
				Vector2 vector = Input.GetAimVector(Facings.Right);
				vector = this.CorrectDashPrecision(vector);
				vector = Abilities.ConstrainDash(vector, this.Facing);   // NOTE (poda): curvar o dash tambem respeita o portao
				float num = Vector2.Dot(vector, this.Speed.SafeNormalize());
				if (num >= -0.1f && num < 0.99f)
				{
					this.Speed = this.Speed.RotateTowards(vector.Angle(), 4.1887903f * Engine.DeltaTime);
					this.DashDir = this.Speed.SafeNormalize();
					this.DashDir = this.CorrectDashPrecision(this.DashDir);
				}
			}
			if (SaveData.Instance.Assists.SuperDashing && this.CanDash)
			{
				this.StartDash();
				this.StateMachine.ForceState(2);
				return 2;
			}
			if (this.Holding == null && this.DashDir != Vector2.Zero && Input.GrabCheck && !this.IsTired && this.CanUnDuck)
			{
				foreach (Component component in base.Scene.Tracker.GetComponents<Holdable>())
				{
					Holdable holdable = (Holdable)component;
					if (holdable.Check(this) && this.Pickup(holdable))
					{
						return 8;
					}
				}
			}
			if (Math.Abs(this.DashDir.Y) < 0.1f)
			{
				foreach (Entity entity in base.Scene.Tracker.GetEntities<JumpThru>())
				{
					JumpThru jumpThru = (JumpThru)entity;
					if (base.CollideCheck(jumpThru) && base.Bottom - jumpThru.Top <= 6f && !this.DashCorrectCheck(Vector2.UnitY * (jumpThru.Top - base.Bottom)))
					{
						base.MoveVExact((int)(jumpThru.Top - base.Bottom), null, null);
					}
				}
				if (this.CanUnDuck && Input.Jump.Pressed && this.jumpGraceTimer > 0f)
				{
					this.SuperJump();
					return 0;
				}
			}
			if (this.SuperWallJumpAngleCheck)
			{
				if (Input.Jump.Pressed && this.CanUnDuck)
				{
					if (this.WallJumpCheck(1))
					{
						this.SuperWallJump(-1);
						return 0;
					}
					if (this.WallJumpCheck(-1))
					{
						this.SuperWallJump(1);
						return 0;
					}
				}
			}
			else if (Input.Jump.Pressed && this.CanUnDuck)
			{
				if (this.WallJumpCheck(1))
				{
					if (Abilities.WallClimb && this.Facing == Facings.Right && Input.GrabCheck && this.Stamina > 0f && this.Holding == null && !ClimbBlocker.Check(base.Scene, this, this.Position + Vector2.UnitX * 3f))
					{
						this.ClimbJump();
					}
					else
					{
						this.WallJump(-1);
					}
					return 0;
				}
				if (this.WallJumpCheck(-1))
				{
					if (Abilities.WallClimb && this.Facing == Facings.Left && Input.GrabCheck && this.Stamina > 0f && this.Holding == null && !ClimbBlocker.Check(base.Scene, this, this.Position + Vector2.UnitX * -3f))
					{
						this.ClimbJump();
					}
					else
					{
						this.WallJump(1);
					}
					return 0;
				}
			}
			if (this.Speed != Vector2.Zero && this.level.OnInterval(0.02f))
			{
				ParticleType type;
				if (!this.wasDashB)
				{
					type = Player.P_DashA;
				}
				else if (this.Sprite.Mode == PlayerSpriteMode.MadelineAsBadeline)
				{
					type = Player.P_DashBadB;
				}
				else
				{
					type = Player.P_DashB;
				}
				this.level.ParticlesFG.Emit(type, base.Center + Calc.Random.Range(Vector2.One * -2f, Vector2.One * 2f), this.DashDir.Angle());
			}
			return 2;
		}

		private bool SuperWallJumpAngleCheck
		{
			get
			{
				return Math.Abs(this.DashDir.X) <= 0.2f && this.DashDir.Y <= -0.75f;
			}
		}

		private Vector2 CorrectDashPrecision(Vector2 dir)
		{
			if (dir.X != 0f && Math.Abs(dir.X) < 0.001f)
			{
				dir.X = 0f;
				dir.Y = (float)Math.Sign(dir.Y);
			}
			else if (dir.Y != 0f && Math.Abs(dir.Y) < 0.001f)
			{
				dir.Y = 0f;
				dir.X = (float)Math.Sign(dir.X);
			}
			return dir;
		}

		private IEnumerator DashCoroutine()
		{
			yield return null;
			if (SaveData.Instance.Assists.DashAssist)
			{
				Input.Rumble(RumbleStrength.Strong, RumbleLength.Medium);
			}
			this.level.Displacement.AddBurst(base.Center, 0.4f, 8f, 64f, 0.5f, Ease.QuadOut, Ease.QuadOut);
			Vector2 vector = this.lastAim;
			if (this.OverrideDashDirection != null)
			{
				vector = this.OverrideDashDirection.Value;
			}
			vector = this.CorrectDashPrecision(vector);
			// NOTE (poda de movimento): so dash horizontal. Diagonal e vertical viram upgrade
			// (Abilities); com eles desligados o dash p/ cima/diagonal cai p/ o lado do input.
			// Consequencia: sem diagonal p/ baixo nao ha hyper dash, e sem dash p/ cima nao ha
			// super wall jump — os dois voltam junto com o upgrade correspondente.
			vector = Abilities.ConstrainDash(vector, this.Facing);
			Vector2 vector2 = vector * 240f;
			if (Math.Sign(this.beforeDashSpeed.X) == Math.Sign(vector2.X) && Math.Abs(this.beforeDashSpeed.X) > Math.Abs(vector2.X))
			{
				vector2.X = this.beforeDashSpeed.X;
			}
			this.Speed = vector2;
			if (base.CollideCheck<Water>())
			{
				this.Speed *= 0.75f;
			}
			this.gliderBoostDir = (this.DashDir = vector);
			base.SceneAs<Level>().DirectionalShake(this.DashDir, 0.2f);
			if (this.DashDir.X != 0f)
			{
				this.Facing = (Facings)Math.Sign(this.DashDir.X);
			}
			this.CallDashEvents();
			if (this.StateMachine.PreviousState == 19)
			{
				this.level.Particles.Emit(FlyFeather.P_Boost, 12, base.Center, Vector2.One * 4f, (-vector).Angle());
			}
			if (this.onGround && this.DashDir.X != 0f && this.DashDir.Y > 0f && this.Speed.Y > 0f && (!this.Inventory.DreamDash || !base.CollideCheck<DreamBlock>(this.Position + Vector2.UnitY)))
			{
				this.DashDir.X = (float)Math.Sign(this.DashDir.X);
				this.DashDir.Y = 0f;
				this.Speed.Y = 0f;
				this.Speed.X = this.Speed.X * 1.2f;
				this.Ducking = true;
			}
			SlashFx.Burst(base.Center, this.DashDir.Angle());
			this.CreateTrail();
			if (SaveData.Instance.Assists.SuperDashing)
			{
				this.dashTrailTimer = 0.1f;
				this.dashTrailCounter = 2;
			}
			else
			{
				this.dashTrailTimer = 0.08f;
				this.dashTrailCounter = 1;
			}
			if (this.DashDir.X != 0f && Input.GrabCheck)
			{
				SwapBlock swapBlock = base.CollideFirst<SwapBlock>(this.Position + Vector2.UnitX * (float)Math.Sign(this.DashDir.X));
				if (swapBlock != null && swapBlock.Direction.X == (float)Math.Sign(this.DashDir.X))
				{
					this.StateMachine.State = 1;
					this.Speed = Vector2.Zero;
					yield break;
				}
			}
			Vector2 swapCancel = Vector2.One;
			foreach (Entity entity in base.Scene.Tracker.GetEntities<SwapBlock>())
			{
				SwapBlock swapBlock2 = (SwapBlock)entity;
				if (base.CollideCheck(swapBlock2, this.Position + Vector2.UnitY) && swapBlock2 != null && swapBlock2.Swapping)
				{
					if (this.DashDir.X != 0f && swapBlock2.Direction.X == (float)Math.Sign(this.DashDir.X))
					{
						this.Speed.X = (swapCancel.X = 0f);
					}
					if (this.DashDir.Y != 0f && swapBlock2.Direction.Y == (float)Math.Sign(this.DashDir.Y))
					{
						this.Speed.Y = (swapCancel.Y = 0f);
					}
				}
			}
			if (SaveData.Instance.Assists.SuperDashing)
			{
				yield return 0.3f;
			}
			else
			{
				yield return 0.15f;
			}
			this.CreateTrail();
			this.AutoJump = true;
			this.AutoJumpTimer = 0f;
			if (this.DashDir.Y <= 0f)
			{
				this.Speed = this.DashDir * 160f;
				this.Speed.X = this.Speed.X * swapCancel.X;
				this.Speed.Y = this.Speed.Y * swapCancel.Y;
			}
			if (this.Speed.Y < 0f)
			{
				this.Speed.Y = this.Speed.Y * 0.75f;
			}
			this.StateMachine.State = 0;
			yield break;
		}

		private bool SwimCheck()
		{
			return base.CollideCheck<Water>(this.Position + Vector2.UnitY * -8f) && base.CollideCheck<Water>(this.Position);
		}

		private bool SwimUnderwaterCheck()
		{
			return base.CollideCheck<Water>(this.Position + Vector2.UnitY * -9f);
		}

		private bool SwimJumpCheck()
		{
			return !base.CollideCheck<Water>(this.Position + Vector2.UnitY * -14f);
		}

		private bool SwimRiseCheck()
		{
			return !base.CollideCheck<Water>(this.Position + Vector2.UnitY * -18f);
		}

		private bool UnderwaterMusicCheck()
		{
			return base.CollideCheck<Water>(this.Position) && base.CollideCheck<Water>(this.Position + Vector2.UnitY * -12f);
		}

		private void SwimBegin()
		{
			if (this.Speed.Y > 0f)
			{
				this.Speed.Y = this.Speed.Y * 0.5f;
			}
			this.Stamina = 110f;
		}

		private int SwimUpdate()
		{
			if (!this.SwimCheck())
			{
				return 0;
			}
			if (this.CanUnDuck)
			{
				this.Ducking = false;
			}
			if (this.CanDash)
			{
				this.demoDashed = Input.CrouchDashPressed;
				Input.Dash.ConsumeBuffer();
				Input.CrouchDash.ConsumeBuffer();
				return 2;
			}
			bool flag = this.SwimUnderwaterCheck();
			if (!flag && this.Speed.Y >= 0f && Input.GrabCheck && !this.IsTired && this.CanUnDuck && Math.Sign(this.Speed.X) != (int)(-(int)this.Facing) && this.ClimbCheck((int)this.Facing, 0))
			{
				if (SaveData.Instance.Assists.NoGrabbing)
				{
					this.ClimbTrigger((int)this.Facing);
				}
				else if (!base.MoveVExact(-1, null, null))
				{
					this.Ducking = false;
					return 1;
				}
			}
			Vector2 vector = Input.Feather.Value;
			vector = vector.SafeNormalize();
			float num = flag ? 60f : 80f;
			float num2 = 80f;
			if (Math.Abs(this.Speed.X) > 80f && Math.Sign(this.Speed.X) == Math.Sign(vector.X))
			{
				this.Speed.X = Calc.Approach(this.Speed.X, num * vector.X, 400f * Engine.DeltaTime);
			}
			else
			{
				this.Speed.X = Calc.Approach(this.Speed.X, num * vector.X, 600f * Engine.DeltaTime);
			}
			if (vector.Y == 0f && this.SwimRiseCheck())
			{
				this.Speed.Y = Calc.Approach(this.Speed.Y, -60f, 600f * Engine.DeltaTime);
			}
			else if (vector.Y >= 0f || this.SwimUnderwaterCheck())
			{
				if (Math.Abs(this.Speed.Y) > 80f && Math.Sign(this.Speed.Y) == Math.Sign(vector.Y))
				{
					this.Speed.Y = Calc.Approach(this.Speed.Y, num2 * vector.Y, 400f * Engine.DeltaTime);
				}
				else
				{
					this.Speed.Y = Calc.Approach(this.Speed.Y, num2 * vector.Y, 600f * Engine.DeltaTime);
				}
			}
			if (!flag && this.moveX != 0 && base.CollideCheck<Solid>(this.Position + Vector2.UnitX * (float)this.moveX) && !base.CollideCheck<Solid>(this.Position + new Vector2((float)this.moveX, -3f)))
			{
				this.ClimbHop();
			}
			if (Input.Jump.Pressed && this.SwimJumpCheck())
			{
				this.Jump(true, true);
				return 0;
			}
			return 3;
		}

		public void Boost(Booster booster)
		{
			this.StateMachine.State = 4;
			this.Speed = Vector2.Zero;
			this.boostTarget = booster.Center;
			this.boostRed = false;
			this.CurrentBooster = booster;
			this.LastBooster = booster;
		}

		public void RedBoost(Booster booster)
		{
			this.StateMachine.State = 4;
			this.Speed = Vector2.Zero;
			this.boostTarget = booster.Center;
			this.boostRed = true;
			this.CurrentBooster = booster;
			this.LastBooster = booster;
		}

		private void BoostBegin()
		{
			this.RefillDash();
			this.RefillStamina();
			if (this.Holding != null)
			{
				this.Drop();
			}
		}

		private void BoostEnd()
		{
			Vector2 vector = (this.boostTarget - base.Collider.Center).Floored();
			base.MoveToX(vector.X, null);
			base.MoveToY(vector.Y, null);
		}

		private int BoostUpdate()
		{
			Vector2 value = Input.Aim.Value * 3f;
			Vector2 vector = Calc.Approach(base.ExactPosition, this.boostTarget - base.Collider.Center + value, 80f * Engine.DeltaTime);
			base.MoveToX(vector.X, null);
			base.MoveToY(vector.Y, null);
			if (!Input.DashPressed && !Input.CrouchDashPressed)
			{
				return 4;
			}
			this.demoDashed = Input.CrouchDashPressed;
			Input.Dash.ConsumePress();
			Input.CrouchDash.ConsumeBuffer();
			if (this.boostRed)
			{
				return 5;
			}
			return 2;
		}

		private IEnumerator BoostCoroutine()
		{
			yield return 0.25f;
			if (this.boostRed)
			{
				this.StateMachine.State = 5;
			}
			else
			{
				this.StateMachine.State = 2;
			}
			yield break;
		}

		private void RedDashBegin()
		{
			this.calledDashEvents = false;
			this.dashStartedOnGround = false;
			Celeste.Freeze(0.05f);
			Dust.Burst(this.Position, (-this.DashDir).Angle(), 8, null);
			this.dashCooldownTimer = 0.2f;
			this.dashRefillCooldownTimer = 0.1f;
			this.StartedDashing = true;
			this.level.Displacement.AddBurst(base.Center, 0.5f, 0f, 80f, 0.666f, Ease.QuadOut, Ease.QuadOut);
			Input.Rumble(RumbleStrength.Strong, RumbleLength.Medium);
			this.dashAttackTimer = 0.3f;
			this.gliderBoostTimer = 0.55f;
			this.DashDir = (this.Speed = Vector2.Zero);
			if (!this.onGround && this.CanUnDuck)
			{
				this.Ducking = false;
			}
			this.DashAssistInit();
		}

		private void RedDashEnd()
		{
			this.CallDashEvents();
		}

		private int RedDashUpdate()
		{
			this.StartedDashing = false;
			bool flag = this.LastBooster != null && this.LastBooster.Ch9HubTransition;
			this.gliderBoostTimer = 0.05f;
			if (this.CanDash)
			{
				return this.StartDash();
			}
			if (this.DashDir.Y == 0f)
			{
				foreach (Entity entity in base.Scene.Tracker.GetEntities<JumpThru>())
				{
					JumpThru jumpThru = (JumpThru)entity;
					if (base.CollideCheck(jumpThru) && base.Bottom - jumpThru.Top <= 6f)
					{
						base.MoveVExact((int)(jumpThru.Top - base.Bottom), null, null);
					}
				}
				if (this.CanUnDuck && Input.Jump.Pressed && this.jumpGraceTimer > 0f && !flag)
				{
					this.SuperJump();
					return 0;
				}
			}
			if (!flag)
			{
				if (this.SuperWallJumpAngleCheck)
				{
					if (Input.Jump.Pressed && this.CanUnDuck)
					{
						if (this.WallJumpCheck(1))
						{
							this.SuperWallJump(-1);
							return 0;
						}
						if (this.WallJumpCheck(-1))
						{
							this.SuperWallJump(1);
							return 0;
						}
					}
				}
				else if (Input.Jump.Pressed && this.CanUnDuck)
				{
					if (this.WallJumpCheck(1))
					{
						if (Abilities.WallClimb && this.Facing == Facings.Right && Input.GrabCheck && this.Stamina > 0f && this.Holding == null && !ClimbBlocker.Check(base.Scene, this, this.Position + Vector2.UnitX * 3f))
						{
							this.ClimbJump();
						}
						else
						{
							this.WallJump(-1);
						}
						return 0;
					}
					if (this.WallJumpCheck(-1))
					{
						if (Abilities.WallClimb && this.Facing == Facings.Left && Input.GrabCheck && this.Stamina > 0f && this.Holding == null && !ClimbBlocker.Check(base.Scene, this, this.Position + Vector2.UnitX * -3f))
						{
							this.ClimbJump();
						}
						else
						{
							this.WallJump(1);
						}
						return 0;
					}
				}
			}
			return 5;
		}

		private IEnumerator RedDashCoroutine()
		{
			yield return null;
			// NOTE (poda de movimento): o red dash (conteudo) usa o mesmo portao do dash normal
			this.Speed = Abilities.ConstrainDash(this.CorrectDashPrecision(this.lastAim), this.Facing) * 240f;
			this.gliderBoostDir = (this.DashDir = Abilities.ConstrainDash(this.lastAim, this.Facing));
			base.SceneAs<Level>().DirectionalShake(this.DashDir, 0.2f);
			if (this.DashDir.X != 0f)
			{
				this.Facing = (Facings)Math.Sign(this.DashDir.X);
			}
			this.CallDashEvents();
			yield break;
		}

		private void HitSquashBegin()
		{
			this.hitSquashNoMoveTimer = 0.1f;
		}

		private int HitSquashUpdate()
		{
			this.Speed.X = Calc.Approach(this.Speed.X, 0f, 800f * Engine.DeltaTime);
			this.Speed.Y = Calc.Approach(this.Speed.Y, 0f, 800f * Engine.DeltaTime);
			if (Input.Jump.Pressed)
			{
				if (this.onGround)
				{
					this.Jump(true, true);
				}
				else if (this.WallJumpCheck(1))
				{
					if (Abilities.WallClimb && this.Facing == Facings.Right && Input.GrabCheck && this.Stamina > 0f && this.Holding == null && !ClimbBlocker.Check(base.Scene, this, this.Position + Vector2.UnitX * 3f))
					{
						this.ClimbJump();
					}
					else
					{
						this.WallJump(-1);
					}
				}
				else if (this.WallJumpCheck(-1))
				{
					if (Abilities.WallClimb && this.Facing == Facings.Left && Input.GrabCheck && this.Stamina > 0f && this.Holding == null && !ClimbBlocker.Check(base.Scene, this, this.Position + Vector2.UnitX * -3f))
					{
						this.ClimbJump();
					}
					else
					{
						this.WallJump(1);
					}
				}
				else
				{
					Input.Jump.ConsumeBuffer();
				}
				return 0;
			}
			if (this.CanDash)
			{
				return this.StartDash();
			}
			if (Input.GrabCheck && this.ClimbCheck((int)this.Facing, 0))
			{
				return 1;
			}
			if (this.hitSquashNoMoveTimer > 0f)
			{
				this.hitSquashNoMoveTimer -= Engine.DeltaTime;
				return 6;
			}
			return 0;
		}

		public Vector2 ExplodeLaunch(Vector2 from, bool snapUp = true, bool sidesOnly = false)
		{
			Input.Rumble(RumbleStrength.Strong, RumbleLength.Medium);
			Celeste.Freeze(0.1f);
			this.launchApproachX = null;
			Vector2 vector = (base.Center - from).SafeNormalize(-Vector2.UnitY);
			float num = Vector2.Dot(vector, Vector2.UnitY);
			if (snapUp && num <= -0.7f)
			{
				vector.X = 0f;
				vector.Y = -1f;
			}
			else if (num <= 0.65f && num >= -0.55f)
			{
				vector.Y = 0f;
				vector.X = (float)Math.Sign(vector.X);
			}
			if (sidesOnly && vector.X != 0f)
			{
				vector.Y = 0f;
				vector.X = (float)Math.Sign(vector.X);
			}
			this.Speed = 280f * vector;
			if (this.Speed.Y <= 50f)
			{
				this.Speed.Y = Math.Min(-150f, this.Speed.Y);
				this.AutoJump = true;
			}
			if (this.Speed.X != 0f)
			{
				if (Input.MoveX.Value == Math.Sign(this.Speed.X))
				{
					this.explodeLaunchBoostTimer = 0f;
					this.Speed.X = this.Speed.X * 1.2f;
				}
				else
				{
					this.explodeLaunchBoostTimer = 0.01f;
					this.explodeLaunchBoostSpeed = this.Speed.X * 1.2f;
				}
			}
			SlashFx.Burst(base.Center, this.Speed.Angle());
			if (!this.Inventory.NoRefills)
			{
				this.RefillDash();
			}
			this.RefillStamina();
			this.dashCooldownTimer = 0.2f;
			this.StateMachine.State = 7;
			return vector;
		}

		public void FinalBossPushLaunch(int dir)
		{
			this.launchApproachX = null;
			this.Speed.X = 0.9f * (float)dir * 280f;
			this.Speed.Y = -150f;
			this.AutoJump = true;
			Input.Rumble(RumbleStrength.Strong, RumbleLength.Medium);
			SlashFx.Burst(base.Center, this.Speed.Angle());
			this.RefillDash();
			this.RefillStamina();
			this.dashCooldownTimer = 0.28f;
			this.StateMachine.State = 7;
		}

		public void BadelineBoostLaunch(float atX)
		{
			this.launchApproachX = new float?(atX);
			this.Speed.X = 0f;
			this.Speed.Y = -330f;
			this.AutoJump = true;
			if (this.Holding != null)
			{
				this.Drop();
			}
			SlashFx.Burst(base.Center, this.Speed.Angle());
			this.RefillDash();
			this.RefillStamina();
			this.dashCooldownTimer = 0.2f;
			this.StateMachine.State = 7;
		}

		private void LaunchBegin()
		{
			this.launched = true;
		}

		private int LaunchUpdate()
		{
			if (this.launchApproachX != null)
			{
				base.MoveTowardsX(this.launchApproachX.Value, 60f * Engine.DeltaTime, null);
			}
			if (this.CanDash)
			{
				return this.StartDash();
			}
			if (Input.GrabCheck && !this.IsTired && !this.Ducking)
			{
				foreach (Component component in base.Scene.Tracker.GetComponents<Holdable>())
				{
					Holdable holdable = (Holdable)component;
					if (holdable.Check(this) && this.Pickup(holdable))
					{
						return 8;
					}
				}
			}
			if (this.Speed.Y < 0f)
			{
				this.Speed.Y = Calc.Approach(this.Speed.Y, 160f, 450f * Engine.DeltaTime);
			}
			else
			{
				this.Speed.Y = Calc.Approach(this.Speed.Y, 160f, 225f * Engine.DeltaTime);
			}
			this.Speed.X = Calc.Approach(this.Speed.X, 0f, 200f * Engine.DeltaTime);
			if (this.Speed.Length() < 220f)
			{
				return 0;
			}
			return 7;
		}

		public void SummitLaunch(float targetX)
		{
			this.summitLaunchTargetX = targetX;
			this.StateMachine.State = 10;
		}

		private void SummitLaunchBegin()
		{
			this.wallBoostTimer = 0f;
			this.Sprite.Play("launch", false, false);
			this.Speed = -Vector2.UnitY * 240f;
			this.summitLaunchParticleTimer = 0.4f;
		}

		private int SummitLaunchUpdate()
		{
			this.summitLaunchParticleTimer -= Engine.DeltaTime;
			if (this.summitLaunchParticleTimer > 0f && base.Scene.OnInterval(0.03f))
			{
				this.level.ParticlesFG.Emit(BadelineBoost.P_Move, 1, base.Center, Vector2.One * 4f);
			}
			this.Facing = Facings.Right;
			base.MoveTowardsX(this.summitLaunchTargetX, 20f * Engine.DeltaTime, null);
			this.Speed = -Vector2.UnitY * 240f;
			if (this.level.OnInterval(0.2f))
			{
				this.level.Add(Engine.Pooler.Create<SpeedRing>().Init(base.Center, 1.5707964f, Color.White));
			}
			CrystalStaticSpinner crystalStaticSpinner = base.Scene.CollideFirst<CrystalStaticSpinner>(new Rectangle((int)(base.X - 4f), (int)(base.Y - 40f), 8, 12));
			if (crystalStaticSpinner != null)
			{
				crystalStaticSpinner.Destroy(false);
				this.level.Shake(0.3f);
				Input.Rumble(RumbleStrength.Medium, RumbleLength.Short);
				Celeste.Freeze(0.01f);
			}
			return 10;
		}

		public void StopSummitLaunch()
		{
			this.StateMachine.State = 0;
			this.Speed.Y = -140f;
			this.AutoJump = true;
			this.varJumpSpeed = this.Speed.Y;
		}

		private IEnumerator PickupCoroutine()
		{
			this.Play("event:/char/madeline/crystaltheo_lift", null, 0f);
			Input.Rumble(RumbleStrength.Medium, RumbleLength.Short);
			if (this.Holding != null && this.Holding.SlowFall && ((this.gliderBoostTimer - 0.16f > 0f && this.gliderBoostDir.Y < 0f) || (this.Speed.Length() > 180f && this.Speed.Y <= 0f)))
			{
				Audio.Play("event:/new_content/game/10_farewell/glider_platform_dissipate", this.Position);
			}
			Vector2 oldSpeed = this.Speed;
			float varJump = this.varJumpTimer;
			this.Speed = Vector2.Zero;
			Vector2 vector = this.Holding.Entity.Position - this.Position;
			Vector2 carryOffsetTarget = Player.CarryOffsetTarget;
			Vector2 control = new Vector2(vector.X + (float)(Math.Sign(vector.X) * 2), Player.CarryOffsetTarget.Y - 2f);
			SimpleCurve curve = new SimpleCurve(vector, carryOffsetTarget, control);
			this.carryOffset = vector;
			Tween tween = Tween.Create(Tween.TweenMode.Oneshot, Ease.CubeInOut, 0.16f, true);
			tween.OnUpdate = delegate(Tween t)
			{
				this.carryOffset = curve.GetPoint(t.Eased);
			};
			base.Add(tween);
			yield return tween.Wait();
			this.Speed = oldSpeed;
			this.Speed.Y = Math.Min(this.Speed.Y, 0f);
			this.varJumpTimer = varJump;
			this.StateMachine.State = 0;
			if (this.Holding != null && this.Holding.SlowFall)
			{
				if (this.gliderBoostTimer > 0f && this.gliderBoostDir.Y < 0f)
				{
					Input.Rumble(RumbleStrength.Medium, RumbleLength.Short);
					this.gliderBoostTimer = 0f;
					this.Speed.Y = Math.Min(this.Speed.Y, -240f * Math.Abs(this.gliderBoostDir.Y));
				}
				else if (this.Speed.Y < 0f)
				{
					this.Speed.Y = Math.Min(this.Speed.Y, -105f);
				}
				if (this.onGround && Input.MoveY == 1f)
				{
					this.holdCannotDuck = true;
				}
			}
			yield break;
		}

		private void DreamDashBegin()
		{
			if (this.dreamSfxLoop == null)
			{
				base.Add(this.dreamSfxLoop = new SoundSource());
			}
			this.Speed = this.DashDir * 240f;
			this.TreatNaive = true;
			base.Depth = -12000;
			this.dreamDashCanEndTimer = 0.1f;
			this.Stamina = 110f;
			this.dreamJump = false;
			this.Play("event:/char/madeline/dreamblock_enter", null, 0f);
			this.Loop(this.dreamSfxLoop, "event:/char/madeline/dreamblock_travel");
		}

		private void DreamDashEnd()
		{
			base.Depth = 0;
			if (!this.dreamJump)
			{
				this.AutoJump = true;
				this.AutoJumpTimer = 0f;
			}
			if (!this.Inventory.NoRefills)
			{
				this.RefillDash();
			}
			this.RefillStamina();
			this.TreatNaive = false;
			if (this.dreamBlock != null)
			{
				if (this.DashDir.X != 0f)
				{
					this.jumpGraceTimer = 0.1f;
					this.dreamJump = true;
				}
				else
				{
					this.jumpGraceTimer = 0f;
				}
				this.dreamBlock.OnPlayerExit(this);
				this.dreamBlock = null;
			}
			this.Stop(this.dreamSfxLoop);
			this.Play("event:/char/madeline/dreamblock_exit", null, 0f);
			Input.Rumble(RumbleStrength.Medium, RumbleLength.Short);
		}

		private int DreamDashUpdate()
		{
			Input.Rumble(RumbleStrength.Light, RumbleLength.Medium);
			Vector2 position = this.Position;
			base.NaiveMove(this.Speed * Engine.DeltaTime);
			if (this.dreamDashCanEndTimer > 0f)
			{
				this.dreamDashCanEndTimer -= Engine.DeltaTime;
			}
			DreamBlock dreamBlock = base.CollideFirst<DreamBlock>();
			if (dreamBlock == null)
			{
				if (this.DreamDashedIntoSolid())
				{
					if (SaveData.Instance.Assists.Invincible)
					{
						this.Position = position;
						this.Speed *= -1f;
						this.Play("event:/game/general/assist_dreamblockbounce", null, 0f);
					}
					else
					{
						this.Die(Vector2.Zero, false, true);
					}
				}
				else if (this.dreamDashCanEndTimer <= 0f)
				{
					Celeste.Freeze(0.05f);
					if (Input.Jump.Pressed && this.DashDir.X != 0f)
					{
						this.dreamJump = true;
						this.Jump(true, true);
					}
					else if (this.DashDir.Y >= 0f || this.DashDir.X != 0f)
					{
						if (this.DashDir.X > 0f && base.CollideCheck<Solid>(this.Position - Vector2.UnitX * 5f))
						{
							base.MoveHExact(-5, null, null);
						}
						else if (this.DashDir.X < 0f && base.CollideCheck<Solid>(this.Position + Vector2.UnitX * 5f))
						{
							base.MoveHExact(5, null, null);
						}
						bool flag = this.ClimbCheck(-1, 0);
						bool flag2 = this.ClimbCheck(1, 0);
						if (Input.GrabCheck && ((this.moveX == 1 && flag2) || (this.moveX == -1 && flag)))
						{
							this.Facing = (Facings)this.moveX;
							if (!SaveData.Instance.Assists.NoGrabbing)
							{
								return 1;
							}
							this.ClimbTrigger(this.moveX);
							this.Speed.X = 0f;
						}
					}
					return 0;
				}
			}
			else
			{
				this.dreamBlock = dreamBlock;
				if (base.Scene.OnInterval(0.1f))
				{
					this.CreateTrail();
				}
				if (this.level.OnInterval(0.04f))
				{
					DisplacementRenderer.Burst burst = this.level.Displacement.AddBurst(base.Center, 0.3f, 0f, 40f, 1f, null, null);
					burst.WorldClipCollider = this.dreamBlock.Collider;
					burst.WorldClipPadding = 2;
				}
			}
			return 9;
		}

		private bool DreamDashedIntoSolid()
		{
			if (base.CollideCheck<Solid>())
			{
				for (int i = 1; i <= 5; i++)
				{
					for (int j = -1; j <= 1; j += 2)
					{
						for (int k = 1; k <= 5; k++)
						{
							for (int l = -1; l <= 1; l += 2)
							{
								Vector2 value = new Vector2((float)(i * j), (float)(k * l));
								if (!base.CollideCheck<Solid>(this.Position + value))
								{
									this.Position += value;
									return false;
								}
							}
						}
					}
				}
				return true;
			}
			return false;
		}

		public bool StartStarFly()
		{
			this.RefillStamina();
			if (this.StateMachine.State == 18)
			{
				return false;
			}
			if (this.StateMachine.State == 19)
			{
				this.starFlyTimer = 2f;
				this.Sprite.Color = this.starFlyColor;
				Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
			}
			else
			{
				this.StateMachine.State = 19;
			}
			return true;
		}

		private void StarFlyBegin()
		{
			this.Sprite.Play("startStarFly", false, false);
			this.starFlyTransforming = true;
			this.starFlyTimer = 2f;
			this.starFlySpeedLerp = 0f;
			this.jumpGraceTimer = 0f;
			if (this.starFlyBloom == null)
			{
				base.Add(this.starFlyBloom = new BloomPoint(new Vector2(0f, -6f), 0f, 16f));
			}
			this.starFlyBloom.Visible = true;
			this.starFlyBloom.Alpha = 0f;
			base.Collider = this.starFlyHitbox;
			this.hurtbox = this.starFlyHurtbox;
			if (this.starFlyLoopSfx == null)
			{
				base.Add(this.starFlyLoopSfx = new SoundSource());
				this.starFlyLoopSfx.DisposeOnTransition = false;
				base.Add(this.starFlyWarningSfx = new SoundSource());
				this.starFlyWarningSfx.DisposeOnTransition = false;
			}
			this.starFlyLoopSfx.Play("event:/game/06_reflection/feather_state_loop", "feather_speed", 1f);
			this.starFlyWarningSfx.Stop(true);
		}

		private void StarFlyEnd()
		{
			this.Play("event:/game/06_reflection/feather_state_end", null, 0f);
			this.starFlyWarningSfx.Stop(true);
			this.starFlyLoopSfx.Stop(true);
			this.Hair.DrawPlayerSpriteOutline = false;
			this.Sprite.Color = Color.White;
			this.level.Displacement.AddBurst(base.Center, 0.25f, 8f, 32f, 1f, null, null);
			this.starFlyBloom.Visible = false;
			this.Sprite.HairCount = this.startHairCount;
			this.StarFlyReturnToNormalHitbox();
			if (this.StateMachine.State != 2)
			{
				this.level.Particles.Emit(FlyFeather.P_Boost, 12, base.Center, Vector2.One * 4f, (-this.Speed).Angle());
			}
		}

		private void StarFlyReturnToNormalHitbox()
		{
			base.Collider = this.normalHitbox;
			this.hurtbox = this.normalHurtbox;
			if (!base.CollideCheck<Solid>())
			{
				return;
			}
			Vector2 position = this.Position;
			base.Y -= this.normalHitbox.Bottom - this.starFlyHitbox.Bottom;
			if (!base.CollideCheck<Solid>())
			{
				return;
			}
			this.Position = position;
			this.Ducking = true;
			base.Y -= this.duckHitbox.Bottom - this.starFlyHitbox.Bottom;
			if (base.CollideCheck<Solid>())
			{
				this.Position = position;
				throw new Exception("Could not get out of solids when exiting Star Fly State!");
			}
		}

		private IEnumerator StarFlyCoroutine()
		{
			while (this.Sprite.CurrentAnimationID == "startStarFly")
			{
				yield return null;
			}
			while (this.Speed != Vector2.Zero)
			{
				yield return null;
			}
			yield return 0.1f;
			this.Sprite.Color = this.starFlyColor;
			this.Sprite.HairCount = 7;
			this.Hair.DrawPlayerSpriteOutline = true;
			this.level.Displacement.AddBurst(base.Center, 0.25f, 8f, 32f, 1f, null, null);
			this.starFlyTransforming = false;
			this.starFlyTimer = 2f;
			this.RefillDash();
			this.RefillStamina();
			Vector2 vector = Input.Feather.Value;
			if (vector == Vector2.Zero)
			{
				vector = Vector2.UnitX * (float)this.Facing;
			}
			this.Speed = vector * 250f;
			this.starFlyLastDir = vector;
			this.level.Particles.Emit(FlyFeather.P_Boost, 12, base.Center, Vector2.One * 4f, (-vector).Angle());
			Input.Rumble(RumbleStrength.Strong, RumbleLength.Medium);
			this.level.DirectionalShake(this.starFlyLastDir, 0.3f);
			while (this.starFlyTimer > 0.5f)
			{
				yield return null;
			}
			this.starFlyWarningSfx.Play("event:/game/06_reflection/feather_state_warning", null, 0f);
			yield break;
		}

		private int StarFlyUpdate()
		{
			this.starFlyBloom.Alpha = Calc.Approach(this.starFlyBloom.Alpha, 0.7f, Engine.DeltaTime * 2f);
			Input.Rumble(RumbleStrength.Climb, RumbleLength.Short);
			if (this.starFlyTransforming)
			{
				this.Speed = Calc.Approach(this.Speed, Vector2.Zero, 1000f * Engine.DeltaTime);
			}
			else
			{
				Vector2 value = Input.Feather.Value;
				bool flag = false;
				if (value == Vector2.Zero)
				{
					flag = true;
					value = this.starFlyLastDir;
				}
				Vector2 vector = this.Speed.SafeNormalize(Vector2.Zero);
				if (vector == Vector2.Zero)
				{
					vector = value;
				}
				else
				{
					vector = vector.RotateTowards(value.Angle(), 5.5850534f * Engine.DeltaTime);
				}
				this.starFlyLastDir = vector;
				float target;
				if (flag)
				{
					this.starFlySpeedLerp = 0f;
					target = 91f;
				}
				else if (vector != Vector2.Zero && Vector2.Dot(vector, value) >= 0.45f)
				{
					this.starFlySpeedLerp = Calc.Approach(this.starFlySpeedLerp, 1f, Engine.DeltaTime / 1f);
					target = MathHelper.Lerp(140f, 190f, this.starFlySpeedLerp);
				}
				else
				{
					this.starFlySpeedLerp = 0f;
					target = 140f;
				}
				this.starFlyLoopSfx.Param("feather_speed", (float)(flag ? 0 : 1));
				float num = this.Speed.Length();
				num = Calc.Approach(num, target, 1000f * Engine.DeltaTime);
				this.Speed = vector * num;
				if (this.level.OnInterval(0.02f))
				{
					this.level.Particles.Emit(FlyFeather.P_Flying, 1, base.Center, Vector2.One * 2f, (-this.Speed).Angle());
				}
				if (Input.Jump.Pressed)
				{
					if (base.OnGround(3))
					{
						this.Jump(true, true);
						return 0;
					}
					if (this.WallJumpCheck(-1))
					{
						this.WallJump(1);
						return 0;
					}
					if (this.WallJumpCheck(1))
					{
						this.WallJump(-1);
						return 0;
					}
				}
				if (Input.GrabCheck)
				{
					bool flag2 = false;
					int dir = 0;
					if (Input.MoveX.Value != -1 && this.ClimbCheck(1, 0))
					{
						this.Facing = Facings.Right;
						dir = 1;
						flag2 = true;
					}
					else if (Input.MoveX.Value != 1 && this.ClimbCheck(-1, 0))
					{
						this.Facing = Facings.Left;
						dir = -1;
						flag2 = true;
					}
					if (flag2)
					{
						if (SaveData.Instance.Assists.NoGrabbing)
						{
							this.Speed = Vector2.Zero;
							this.ClimbTrigger(dir);
							return 0;
						}
						return 1;
					}
				}
				if (this.CanDash)
				{
					return this.StartDash();
				}
				this.starFlyTimer -= Engine.DeltaTime;
				if (this.starFlyTimer <= 0f)
				{
					if (Input.MoveY.Value == -1)
					{
						this.Speed.Y = -100f;
					}
					if (Input.MoveY.Value < 1)
					{
						this.varJumpSpeed = this.Speed.Y;
						this.AutoJump = true;
						this.AutoJumpTimer = 0f;
						this.varJumpTimer = 0.2f;
					}
					if (this.Speed.Y > 0f)
					{
						this.Speed.Y = 0f;
					}
					if (Math.Abs(this.Speed.X) > 140f)
					{
						this.Speed.X = 140f * (float)Math.Sign(this.Speed.X);
					}
					Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
					return 0;
				}
				if (this.starFlyTimer < 0.5f && base.Scene.OnInterval(0.05f))
				{
					if (this.Sprite.Color == this.starFlyColor)
					{
						this.Sprite.Color = Player.NormalHairColor;
					}
					else
					{
						this.Sprite.Color = this.starFlyColor;
					}
				}
			}
			return 19;
		}

		public bool DoFlingBird(FlingBird bird)
		{
			if (!this.Dead && this.StateMachine.State != 24)
			{
				this.flingBird = bird;
				this.StateMachine.State = 24;
				if (this.Holding != null)
				{
					this.Drop();
				}
				return true;
			}
			return false;
		}

		public void FinishFlingBird()
		{
			this.StateMachine.State = 0;
			this.AutoJump = true;
			this.forceMoveX = 1;
			this.forceMoveXTimer = 0.2f;
			this.Speed = FlingBird.FlingSpeed;
			this.varJumpTimer = 0.2f;
			this.varJumpSpeed = this.Speed.Y;
			this.launched = true;
		}

		private void FlingBirdBegin()
		{
			this.RefillDash();
			this.RefillStamina();
		}

		private void FlingBirdEnd()
		{
		}

		private int FlingBirdUpdate()
		{
			base.MoveTowardsX(this.flingBird.X, 250f * Engine.DeltaTime, null);
			base.MoveTowardsY(this.flingBird.Y + 8f + base.Collider.Height, 250f * Engine.DeltaTime, null);
			return 24;
		}

		private IEnumerator FlingBirdCoroutine()
		{
			yield break;
		}

		public void StartCassetteFly(Vector2 targetPosition, Vector2 control)
		{
			this.StateMachine.State = 21;
			this.cassetteFlyCurve = new SimpleCurve(this.Position, targetPosition, control);
			this.cassetteFlyLerp = 0f;
			this.Speed = Vector2.Zero;
			if (this.Holding != null)
			{
				this.Drop();
			}
		}

		private void CassetteFlyBegin()
		{
			this.Sprite.Play("bubble", false, false);
			this.Sprite.Y += 5f;
		}

		private void CassetteFlyEnd()
		{
		}

		private int CassetteFlyUpdate()
		{
			return 21;
		}

		private IEnumerator CassetteFlyCoroutine()
		{
			this.level.CanRetry = false;
			this.level.FormationBackdrop.Display = true;
			this.level.FormationBackdrop.Alpha = 0.5f;
			this.Sprite.Scale = Vector2.One * 1.25f;
			base.Depth = -2000000;
			yield return 0.4f;
			while (this.cassetteFlyLerp < 1f)
			{
				if (this.level.OnInterval(0.03f))
				{
					this.level.Particles.Emit(Player.P_CassetteFly, 2, base.Center, Vector2.One * 4f);
				}
				this.cassetteFlyLerp = Calc.Approach(this.cassetteFlyLerp, 1f, 1.6f * Engine.DeltaTime);
				this.Position = this.cassetteFlyCurve.GetPoint(Ease.SineInOut(this.cassetteFlyLerp));
				this.level.Camera.Position = this.CameraTarget;
				yield return null;
			}
			this.Position = this.cassetteFlyCurve.End;
			this.Sprite.Scale = Vector2.One * 1.25f;
			this.Sprite.Y -= 5f;
			this.Sprite.Play("fallFast", false, false);
			yield return 0.2f;
			this.level.CanRetry = true;
			this.level.FormationBackdrop.Display = false;
			this.level.FormationBackdrop.Alpha = 0.5f;
			this.StateMachine.State = 0;
			base.Depth = 0;
			yield break;
		}

		public void StartAttract(Vector2 attractTo)
		{
			this.attractTo = Calc.Round(attractTo);
			this.StateMachine.State = 22;
		}

		private void AttractBegin()
		{
			this.Speed = Vector2.Zero;
		}

		private void AttractEnd()
		{
		}

		private int AttractUpdate()
		{
			if (Vector2.Distance(this.attractTo, base.ExactPosition) <= 1.5f)
			{
				this.Position = this.attractTo;
				base.ZeroRemainderX();
				base.ZeroRemainderY();
			}
			else
			{
				Vector2 vector = Calc.Approach(base.ExactPosition, this.attractTo, 200f * Engine.DeltaTime);
				base.MoveToX(vector.X, null);
				base.MoveToY(vector.Y, null);
			}
			return 22;
		}

		public bool AtAttractTarget
		{
			get
			{
				return this.StateMachine.State == 22 && base.ExactPosition == this.attractTo;
			}
		}

		private void DummyBegin()
		{
			this.DummyMoving = false;
			this.DummyGravity = true;
			this.DummyAutoAnimate = true;
		}

		private int DummyUpdate()
		{
			if (this.CanUnDuck)
			{
				this.Ducking = false;
			}
			if (!this.onGround && this.DummyGravity)
			{
				float num = (Math.Abs(this.Speed.Y) < 40f && (Input.Jump.Check || this.AutoJump)) ? 0.5f : 1f;
				if (this.level.InSpace)
				{
					num *= 0.6f;
				}
				this.Speed.Y = Calc.Approach(this.Speed.Y, 160f, 900f * num * Engine.DeltaTime);
			}
			if (this.varJumpTimer > 0f)
			{
				if (this.AutoJump || Input.Jump.Check)
				{
					this.Speed.Y = Math.Min(this.Speed.Y, this.varJumpSpeed);
				}
				else
				{
					this.varJumpTimer = 0f;
				}
			}
			if (!this.DummyMoving)
			{
				if (Math.Abs(this.Speed.X) > 90f && this.DummyMaxspeed)
				{
					this.Speed.X = Calc.Approach(this.Speed.X, 90f * (float)Math.Sign(this.Speed.X), 2500f * Engine.DeltaTime);
				}
				if (this.DummyFriction)
				{
					this.Speed.X = Calc.Approach(this.Speed.X, 0f, 1000f * Engine.DeltaTime);
				}
			}
			if (this.DummyAutoAnimate)
			{
				if (this.onGround)
				{
					if (this.Speed.X == 0f)
					{
						this.Sprite.Play("idle", false, false);
					}
					else
					{
						this.Sprite.Play("walk", false, false);
					}
				}
				else if (this.Speed.Y < 0f)
				{
					this.Sprite.Play("jumpSlow", false, false);
				}
				else
				{
					this.Sprite.Play("fallSlow", false, false);
				}
			}
			return 11;
		}

		public IEnumerator DummyWalkTo(float x, bool walkBackwards = false, float speedMultiplier = 1f, bool keepWalkingIntoWalls = false)
		{
			this.StateMachine.State = 11;
			if (Math.Abs(base.X - x) > 4f && !this.Dead)
			{
				this.DummyMoving = true;
				if (walkBackwards)
				{
					this.Sprite.Rate = -1f;
					this.Facing = (Facings)Math.Sign(base.X - x);
				}
				else
				{
					this.Facing = (Facings)Math.Sign(x - base.X);
				}
				while (Math.Abs(x - base.X) > 4f && base.Scene != null && (keepWalkingIntoWalls || !base.CollideCheck<Solid>(this.Position + Vector2.UnitX * (float)Math.Sign(x - base.X))))
				{
					this.Speed.X = Calc.Approach(this.Speed.X, (float)Math.Sign(x - base.X) * 64f * speedMultiplier, 1000f * Engine.DeltaTime);
					yield return null;
				}
				this.Sprite.Rate = 1f;
				this.Sprite.Play("idle", false, false);
				this.DummyMoving = false;
			}
			yield break;
		}

		public IEnumerator DummyWalkToExact(int x, bool walkBackwards = false, float speedMultiplier = 1f, bool cancelOnFall = false)
		{
			this.StateMachine.State = 11;
			if (base.X != (float)x)
			{
				this.DummyMoving = true;
				if (walkBackwards)
				{
					this.Sprite.Rate = -1f;
					this.Facing = (Facings)Math.Sign(base.X - (float)x);
				}
				else
				{
					this.Facing = (Facings)Math.Sign((float)x - base.X);
				}
				int last = Math.Sign(base.X - (float)x);
				while (!this.Dead && base.X != (float)x && !base.CollideCheck<Solid>(this.Position + new Vector2((float)this.Facing, 0f)) && (!cancelOnFall || base.OnGround(1)))
				{
					this.Speed.X = Calc.Approach(this.Speed.X, (float)Math.Sign((float)x - base.X) * 64f * speedMultiplier, 1000f * Engine.DeltaTime);
					int num = Math.Sign(base.X - (float)x);
					if (num != last)
					{
						base.X = (float)x;
						break;
					}
					last = num;
					yield return null;
				}
				this.Speed.X = 0f;
				this.Sprite.Rate = 1f;
				this.Sprite.Play("idle", false, false);
				this.DummyMoving = false;
			}
			yield break;
		}

		public IEnumerator DummyRunTo(float x, bool fastAnim = false)
		{
			this.StateMachine.State = 11;
			if (Math.Abs(base.X - x) > 4f)
			{
				this.DummyMoving = true;
				if (fastAnim)
				{
					this.Sprite.Play("runFast", false, false);
				}
				else if (!this.Sprite.LastAnimationID.StartsWith("run"))
				{
					this.Sprite.Play("runSlow", false, false);
				}
				this.Facing = (Facings)Math.Sign(x - base.X);
				while (Math.Abs(base.X - x) > 4f)
				{
					this.Speed.X = Calc.Approach(this.Speed.X, (float)Math.Sign(x - base.X) * 90f, 1000f * Engine.DeltaTime);
					yield return null;
				}
				this.Sprite.Play("idle", false, false);
				this.DummyMoving = false;
			}
			yield break;
		}

		private int FrozenUpdate()
		{
			return 17;
		}

		private int TempleFallUpdate()
		{
			this.Facing = Facings.Right;
			if (!this.onGround)
			{
				int num = this.level.Bounds.Left + 160;
				int num2;
				if (Math.Abs((float)num - base.X) > 4f)
				{
					num2 = Math.Sign((float)num - base.X);
				}
				else
				{
					num2 = 0;
				}
				this.Speed.X = Calc.Approach(this.Speed.X, 54.000004f * (float)num2, 325f * Engine.DeltaTime);
			}
			if (!this.onGround && this.DummyGravity)
			{
				this.Speed.Y = Calc.Approach(this.Speed.Y, 320f, 225f * Engine.DeltaTime);
			}
			return 20;
		}

		private IEnumerator TempleFallCoroutine()
		{
			this.Sprite.Play("fallFast", false, false);
			while (!this.onGround)
			{
				yield return null;
			}
			this.Play("event:/char/madeline/mirrortemple_big_landing", null, 0f);
			if (this.Dashes <= 1)
			{
				this.Sprite.Play("fallPose", false, false);
			}
			else
			{
				this.Sprite.Play("idle", false, false);
			}
			this.Sprite.Scale.Y = 0.7f;
			Input.Rumble(RumbleStrength.Strong, RumbleLength.Medium);
			this.level.DirectionalShake(new Vector2(0f, 1f), 0.5f);
			this.Speed.X = 0f;
			this.level.Particles.Emit(Player.P_SummitLandA, 12, base.BottomCenter, Vector2.UnitX * 3f, -1.5707964f);
			this.level.Particles.Emit(Player.P_SummitLandB, 8, base.BottomCenter - Vector2.UnitX * 2f, Vector2.UnitX * 2f, 3.403392f);
			this.level.Particles.Emit(Player.P_SummitLandB, 8, base.BottomCenter + Vector2.UnitX * 2f, Vector2.UnitX * 2f, -0.2617994f);
			for (float p = 0f; p < 1f; p += Engine.DeltaTime)
			{
				yield return null;
			}
			this.StateMachine.State = 0;
			yield break;
		}

		private void ReflectionFallBegin()
		{
			this.IgnoreJumpThrus = true;
		}

		private void ReflectionFallEnd()
		{
			FallEffects.Show(false);
			this.IgnoreJumpThrus = false;
		}

		private int ReflectionFallUpdate()
		{
			this.Facing = Facings.Right;
			if (base.Scene.OnInterval(0.05f))
			{
				this.wasDashB = true;
				this.CreateTrail();
			}
			if (base.CollideCheck<Water>())
			{
				this.Speed.Y = Calc.Approach(this.Speed.Y, -20f, 400f * Engine.DeltaTime);
			}
			else
			{
				this.Speed.Y = Calc.Approach(this.Speed.Y, 320f, 225f * Engine.DeltaTime);
			}
			foreach (Entity entity in base.Scene.Tracker.GetEntities<FlyFeather>())
			{
				entity.RemoveSelf();
			}
			CrystalStaticSpinner crystalStaticSpinner = base.Scene.CollideFirst<CrystalStaticSpinner>(new Rectangle((int)(base.X - 6f), (int)(base.Y - 6f), 12, 12));
			if (crystalStaticSpinner != null)
			{
				crystalStaticSpinner.Destroy(false);
				this.level.Shake(0.3f);
				Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
				Celeste.Freeze(0.01f);
			}
			return 18;
		}

		private IEnumerator ReflectionFallCoroutine()
		{
			this.Sprite.Play("bigFall", false, false);
			this.level.StartCutscene(new Action<Level>(this.OnReflectionFallSkip), true, false, true);
			for (float t = 0f; t < 2f; t += Engine.DeltaTime)
			{
				this.Speed.Y = 0f;
				yield return null;
			}
			FallEffects.Show(true);
			this.Speed.Y = 320f;
			while (!base.CollideCheck<Water>())
			{
				yield return null;
			}
			Input.Rumble(RumbleStrength.Strong, RumbleLength.Medium);
			FallEffects.Show(false);
			this.Sprite.Play("bigFallRecover", false, false);
			this.level.Session.Audio.Music.Event = "event:/music/lvl6/main";
			this.level.Session.Audio.Apply(false);
			this.level.EndCutscene();
			yield return 1.2f;
			this.StateMachine.State = 0;
			yield break;
		}

		private void OnReflectionFallSkip(Level level)
		{
			level.OnEndOfFrame += delegate()
			{
				level.Remove(this);
				level.UnloadLevel();
				level.Session.Level = "00";
				level.Session.RespawnPoint = new Vector2?(level.GetSpawnPoint(new Vector2((float)level.Bounds.Left, (float)level.Bounds.Bottom)));
				level.LoadLevel(Player.IntroTypes.None, false);
				FallEffects.Show(false);
				level.Session.Audio.Music.Event = "event:/music/lvl6/main";
				level.Session.Audio.Apply(false);
			};
		}

		public IEnumerator IntroWalkCoroutine()
		{
			Vector2 start = this.Position;
			if (this.IntroWalkDirection == Facings.Right)
			{
				base.X = (float)(this.level.Bounds.Left - 16);
				this.Facing = Facings.Right;
			}
			else
			{
				base.X = (float)(this.level.Bounds.Right + 16);
				this.Facing = Facings.Left;
			}
			yield return 0.3f;
			this.Sprite.Play("runSlow", false, false);
			while (Math.Abs(base.X - start.X) > 2f && !base.CollideCheck<Solid>(this.Position + new Vector2((float)this.Facing, 0f)))
			{
				base.MoveTowardsX(start.X, 64f * Engine.DeltaTime, null);
				yield return null;
			}
			this.Position = start;
			this.Sprite.Play("idle", false, false);
			yield return 0.2f;
			this.StateMachine.State = 0;
			yield break;
		}

		private IEnumerator IntroJumpCoroutine()
		{
			Vector2 start = this.Position;
			bool wasSummitJump = this.StateMachine.PreviousState == 10;
			base.Depth = -1000000;
			this.Facing = Facings.Right;
			if (!wasSummitJump)
			{
				base.Y = (float)(this.level.Bounds.Bottom + 16);
				yield return 0.5f;
			}
			else
			{
				start.Y = (float)(this.level.Bounds.Bottom - 24);
				base.MoveToX((float)((int)Math.Round((double)(base.X / 8f)) * 8), null);
			}
			if (!wasSummitJump)
			{
				this.Sprite.Play("jumpSlow", false, false);
			}
			while (base.Y > start.Y - 8f)
			{
				base.Y += -120f * Engine.DeltaTime;
				yield return null;
			}
			base.Y = (float)Math.Round((double)base.Y);
			this.Speed.Y = -100f;
			while (this.Speed.Y < 0f)
			{
				this.Speed.Y = this.Speed.Y + Engine.DeltaTime * 800f;
				yield return null;
			}
			this.Speed.Y = 0f;
			if (wasSummitJump)
			{
				yield return 0.2f;
				this.Play("event:/char/madeline/summit_areastart", null, 0f);
				this.Sprite.Play("launchRecover", false, false);
				yield return 0.1f;
			}
			else
			{
				yield return 0.1f;
			}
			if (!wasSummitJump)
			{
				this.Sprite.Play("fallSlow", false, false);
			}
			while (!this.onGround)
			{
				this.Speed.Y = this.Speed.Y + Engine.DeltaTime * 800f;
				yield return null;
			}
			if (this.StateMachine.PreviousState != 10)
			{
				this.Position = start;
			}
			base.Depth = 0;
			this.level.DirectionalShake(Vector2.UnitY, 0.3f);
			Input.Rumble(RumbleStrength.Strong, RumbleLength.Medium);
			if (wasSummitJump)
			{
				this.level.Particles.Emit(Player.P_SummitLandA, 12, base.BottomCenter, Vector2.UnitX * 3f, -1.5707964f);
				this.level.Particles.Emit(Player.P_SummitLandB, 8, base.BottomCenter - Vector2.UnitX * 2f, Vector2.UnitX * 2f, 3.403392f);
				this.level.Particles.Emit(Player.P_SummitLandB, 8, base.BottomCenter + Vector2.UnitX * 2f, Vector2.UnitX * 2f, -0.2617994f);
				this.level.ParticlesBG.Emit(Player.P_SummitLandC, 30, base.BottomCenter, Vector2.UnitX * 5f);
				yield return 0.35f;
				for (int i = 0; i < this.Hair.Nodes.Count; i++)
				{
					this.Hair.Nodes[i] = new Vector2(0f, (float)(2 + i));
				}
			}
			this.StateMachine.State = 0;
			yield break;
		}

		private IEnumerator IntroMoonJumpCoroutine()
		{
			Vector2 start = this.Position;
			this.Facing = Facings.Right;
			this.Speed = Vector2.Zero;
			this.Visible = false;
			base.Y = (float)(this.level.Bounds.Bottom + 16);
			yield return 0.5f;
			yield return this.MoonLanding(start);
			this.StateMachine.State = 0;
			yield break;
		}

		public IEnumerator MoonLanding(Vector2 groundPosition)
		{
			base.Depth = -1000000;
			this.Speed = Vector2.Zero;
			this.Visible = true;
			this.Sprite.Play("jumpSlow", false, false);
			while (base.Y > groundPosition.Y - 8f)
			{
				base.MoveV(-200f * Engine.DeltaTime, null, null);
				yield return null;
			}
			this.Speed.Y = -200f;
			while (this.Speed.Y < 0f)
			{
				this.Speed.Y = this.Speed.Y + Engine.DeltaTime * 400f;
				yield return null;
			}
			this.Speed.Y = 0f;
			yield return 0.2f;
			this.Sprite.Play("fallSlow", false, false);
			float s = 100f;
			while (!base.OnGround(1))
			{
				this.Speed.Y = this.Speed.Y + Engine.DeltaTime * s;
				s = Calc.Approach(s, 2f, Engine.DeltaTime * 50f);
				yield return null;
			}
			base.Depth = 0;
			yield break;
		}

		private IEnumerator IntroWakeUpCoroutine()
		{
			this.Sprite.Play("asleep", false, false);
			yield return 0.5f;
			yield return this.Sprite.PlayRoutine("wakeUp", false);
			yield return 0.2f;
			this.StateMachine.State = 0;
			yield break;
		}

		private void IntroRespawnBegin()
		{
			this.Play("event:/char/madeline/revive", null, 0f);
			base.Depth = -1000000;
			this.introEase = 1f;
			Vector2 from = this.Position;
			from.X = MathHelper.Clamp(from.X, (float)this.level.Bounds.Left + 40f, (float)this.level.Bounds.Right - 40f);
			from.Y = MathHelper.Clamp(from.Y, (float)this.level.Bounds.Top + 40f, (float)this.level.Bounds.Bottom - 40f);
			this.deadOffset = from;
			from -= this.Position;
			this.respawnTween = Tween.Create(Tween.TweenMode.Oneshot, null, 0.6f, true);
			this.respawnTween.OnUpdate = delegate(Tween t)
			{
				this.deadOffset = Vector2.Lerp(from, Vector2.Zero, t.Eased);
				this.introEase = 1f - t.Eased;
			};
			this.respawnTween.OnComplete = delegate(Tween t)
			{
				if (this.StateMachine.State == 14)
				{
					this.StateMachine.State = 0;
					this.Sprite.Scale = new Vector2(1.5f, 0.5f);
				}
			};
			base.Add(this.respawnTween);
		}

		private void IntroRespawnEnd()
		{
			base.Depth = 0;
			this.deadOffset = Vector2.Zero;
			base.Remove(this.respawnTween);
			this.respawnTween = null;
		}

		public IEnumerator IntroThinkForABitCoroutine()
		{
			(base.Scene as Level).Camera.X += 8f;
			yield return 0.1f;
			this.Sprite.Play("walk", false, false);
			float target = base.X + 8f;
			while (base.X < target)
			{
				base.MoveH(32f * Engine.DeltaTime, null, null);
				yield return null;
			}
			this.Sprite.Play("idle", false, false);
			yield return 0.3f;
			this.Facing = Facings.Left;
			yield return 0.8f;
			this.Facing = Facings.Right;
			yield return 0.1f;
			this.StateMachine.State = 0;
			yield break;
		}

		private void BirdDashTutorialBegin()
		{
			this.DashBegin();
			this.Play("event:/char/madeline/dash_red_right", null, 0f);
			this.Sprite.Play("dash", false, false);
		}

		private int BirdDashTutorialUpdate()
		{
			return 16;
		}

		private IEnumerator BirdDashTutorialCoroutine()
		{
			yield return null;
			this.CreateTrail();
			base.Add(Alarm.Create(Alarm.AlarmMode.Oneshot, new Action(this.CreateTrail), 0.08f, true));
			base.Add(Alarm.Create(Alarm.AlarmMode.Oneshot, new Action(this.CreateTrail), 0.15f, true));
			Vector2 vector = new Vector2(1f, -1f).SafeNormalize();
			this.Facing = Facings.Right;
			this.Speed = vector * 240f;
			this.DashDir = vector;
			base.SceneAs<Level>().DirectionalShake(this.DashDir, 0.2f);
			SlashFx.Burst(base.Center, this.DashDir.Angle());
			for (float time = 0f; time < 0.15f; time += Engine.DeltaTime)
			{
				if (this.Speed != Vector2.Zero && this.level.OnInterval(0.02f))
				{
					this.level.ParticlesFG.Emit(Player.P_DashA, base.Center + Calc.Random.Range(Vector2.One * -2f, Vector2.One * 2f), this.DashDir.Angle());
				}
				yield return null;
			}
			this.AutoJump = true;
			this.AutoJumpTimer = 0f;
			if (this.DashDir.Y <= 0f)
			{
				this.Speed = this.DashDir * 160f;
			}
			if (this.Speed.Y < 0f)
			{
				this.Speed.Y = this.Speed.Y * 0.75f;
			}
			this.Sprite.Play("fallFast", false, false);
			bool climbing = false;
			while (!base.OnGround(1) && !climbing)
			{
				this.Speed.Y = Calc.Approach(this.Speed.Y, 160f, 900f * Engine.DeltaTime);
				if (base.CollideCheck<Solid>(this.Position + new Vector2(1f, 0f)))
				{
					climbing = true;
				}
				if (base.Top > (float)this.level.Bounds.Bottom)
				{
					this.level.CancelCutscene();
					this.Die(Vector2.Zero, false, true);
				}
				yield return null;
			}
			if (climbing)
			{
				this.Sprite.Play("wallslide", false, false);
				Dust.Burst(this.Position + new Vector2(4f, -6f), new Vector2(-4f, 0f).Angle(), 1, null);
				this.Speed.Y = 0f;
				yield return 0.2f;
				this.Sprite.Play("climbUp", false, false);
				while (base.CollideCheck<Solid>(this.Position + new Vector2(1f, 0f)))
				{
					base.Y += -45f * Engine.DeltaTime;
					yield return null;
				}
				base.Y = (float)Math.Round((double)base.Y);
				this.Play("event:/char/madeline/climb_ledge", null, 0f);
				this.Sprite.Play("jumpFast", false, false);
				this.Speed.Y = -105f;
				while (!base.OnGround(1))
				{
					this.Speed.Y = Calc.Approach(this.Speed.Y, 160f, 900f * Engine.DeltaTime);
					this.Speed.X = 20f;
					yield return null;
				}
				this.Speed.X = 0f;
				this.Speed.Y = 0f;
				this.Sprite.Play("walk", false, false);
				for (float time = 0f; time < 0.5f; time += Engine.DeltaTime)
				{
					base.X += 32f * Engine.DeltaTime;
					yield return null;
				}
				this.Sprite.Play("tired", false, false);
			}
			else
			{
				this.Sprite.Play("tired", false, false);
				this.Speed.Y = 0f;
				while (this.Speed.X != 0f)
				{
					this.Speed.X = Calc.Approach(this.Speed.X, 0f, 240f * Engine.DeltaTime);
					if (base.Scene.OnInterval(0.04f))
					{
						Dust.Burst(base.BottomCenter + new Vector2(0f, -2f), -2.3561945f, 1, null);
					}
					yield return null;
				}
			}
			yield break;
		}

		public SoundHandle Play(string sound, string param = null, float value = 0f)
		{
			float value2 = 0f;
			Level level = base.Scene as Level;
			if (level != null && level.Raining)
			{
				value2 = 1f;
			}
			this.AddChaserStateSound(sound, param, value, Player.ChaserStateSound.Actions.Oneshot);
			return Audio.Play(sound, base.Center, param, value, "raining", value2);
		}

		public void Loop(SoundSource sfx, string sound)
		{
			this.AddChaserStateSound(sound, null, 0f, Player.ChaserStateSound.Actions.Loop);
			sfx.Play(sound, null, 0f);
		}

		public void Stop(SoundSource sfx)
		{
			if (sfx.Playing)
			{
				this.AddChaserStateSound(sfx.EventName, null, 0f, Player.ChaserStateSound.Actions.Stop);
				sfx.Stop(true);
			}
		}

		private void AddChaserStateSound(string sound, Player.ChaserStateSound.Actions action)
		{
			this.AddChaserStateSound(sound, null, 0f, action);
		}

		private void AddChaserStateSound(string sound, string param = null, float value = 0f, Player.ChaserStateSound.Actions action = Player.ChaserStateSound.Actions.Oneshot)
		{
			string text = null;
			SFX.MadelineToBadelineSound.TryGetValue(sound, out text);
			if (text != null)
			{
				this.activeSounds.Add(new Player.ChaserStateSound
				{
					Event = text,
					Parameter = param,
					ParameterValue = value,
					Action = action
				});
			}
		}

		private ParticleType DustParticleFromSurfaceIndex(int index)
		{
			if (index == 40)
			{
				return ParticleTypes.SparkyDust;
			}
			return ParticleTypes.Dust;
		}

		public static ParticleType P_DashA = new ParticleType(); // NOTE: init de conteudo stub (sem visual)

		public static ParticleType P_DashB = new ParticleType(); // NOTE: init de conteudo stub (sem visual)

		public static ParticleType P_DashBadB = new ParticleType(); // NOTE: init de conteudo stub (sem visual)

		public static ParticleType P_CassetteFly = new ParticleType(); // NOTE: init de conteudo stub (sem visual)

		public static ParticleType P_Split = new ParticleType(); // NOTE: init de conteudo stub (sem visual)

		public static ParticleType P_SummitLandA = new ParticleType(); // NOTE: init de conteudo stub (sem visual)

		public static ParticleType P_SummitLandB = new ParticleType(); // NOTE: init de conteudo stub (sem visual)

		public static ParticleType P_SummitLandC = new ParticleType(); // NOTE: init de conteudo stub (sem visual)

		public const float MaxFall = 160f;

		private const float Gravity = 900f;

		private const float HalfGravThreshold = 40f;

		private const float FastMaxFall = 240f;

		private const float FastMaxAccel = 300f;

		public const float MaxRun = 90f;

		public const float RunAccel = 1000f;

		private const float RunReduce = 400f;

		private const float AirMult = 0.65f;

		private const float HoldingMaxRun = 70f;

		private const float HoldMinTime = 0.35f;

		private const float BounceAutoJumpTime = 0.1f;

		private const float DuckFriction = 500f;

		private const int DuckCorrectCheck = 4;

		private const float DuckCorrectSlide = 50f;

		private const float DodgeSlideSpeedMult = 1.2f;

		private const float DuckSuperJumpXMult = 1.25f;

		private const float DuckSuperJumpYMult = 0.5f;

		private const float JumpGraceTime = 0.1f;

		private const float JumpSpeed = -105f;

		private const float JumpHBoost = 40f;

		private const float VarJumpTime = 0.2f;

		private const float CeilingVarJumpGrace = 0.05f;

		private const int UpwardCornerCorrection = 4;

		private const int DashingUpwardCornerCorrection = 5;

		private const float WallSpeedRetentionTime = 0.06f;

		private const int WallJumpCheckDist = 3;

		private const int SuperWallJumpCheckDist = 5;

		private const float WallJumpForceTime = 0.16f;

		private const float WallJumpHSpeed = 130f;

		public const float WallSlideStartMax = 20f;

		private const float WallSlideTime = 1.2f;

		private const float BounceVarJumpTime = 0.2f;

		private const float BounceSpeed = -140f;

		private const float SuperBounceVarJumpTime = 0.2f;

		private const float SuperBounceSpeed = -185f;

		private const float SuperJumpSpeed = -105f;

		private const float SuperJumpH = 260f;

		private const float SuperWallJumpSpeed = -160f;

		private const float SuperWallJumpVarTime = 0.25f;

		private const float SuperWallJumpForceTime = 0.2f;

		private const float SuperWallJumpH = 170f;

		private const float DashSpeed = 240f;

		private const float EndDashSpeed = 160f;

		private const float EndDashUpMult = 0.75f;

		private const float DashTime = 0.15f;

		private const float SuperDashTime = 0.3f;

		private const float DashCooldown = 0.2f;

		private const float DashRefillCooldown = 0.1f;

		private const int DashHJumpThruNudge = 6;

		private const int DashCornerCorrection = 4;

		private const int DashVFloorSnapDist = 3;

		private const float DashAttackTime = 0.3f;

		private const float BoostMoveSpeed = 80f;

		public const float BoostTime = 0.25f;

		private const float DuckWindMult = 0f;

		private const int WindWallDistance = 3;

		private const float ReboundSpeedX = 120f;

		private const float ReboundSpeedY = -120f;

		private const float ReboundVarJumpTime = 0.15f;

		private const float ReflectBoundSpeed = 220f;

		private const float DreamDashSpeed = 240f;

		private const int DreamDashEndWiggle = 5;

		private const float DreamDashMinTime = 0.1f;

		public const float ClimbMaxStamina = 110f;

		private const float ClimbUpCost = 45.454544f;

		private const float ClimbStillCost = 10f;

		private const float ClimbJumpCost = 27.5f;

		private const int ClimbCheckDist = 2;

		private const int ClimbUpCheckDist = 2;

		private const float ClimbNoMoveTime = 0.1f;

		public const float ClimbTiredThreshold = 20f;

		private const float ClimbUpSpeed = -45f;

		private const float ClimbDownSpeed = 80f;

		private const float ClimbSlipSpeed = 30f;

		private const float ClimbAccel = 900f;

		private const float ClimbGrabYMult = 0.2f;

		private const float ClimbHopY = -120f;

		private const float ClimbHopX = 100f;

		private const float ClimbHopForceTime = 0.2f;

		private const float ClimbJumpBoostTime = 0.2f;

		private const float ClimbHopNoWindTime = 0.3f;

		private const float LaunchSpeed = 280f;

		private const float LaunchCancelThreshold = 220f;

		private const float LiftYCap = -130f;

		private const float LiftXCap = 250f;

		private const float JumpThruAssistSpeed = -40f;

		private const float FlyPowerFlashTime = 0.5f;

		private const float ThrowRecoil = 80f;

		private static readonly Vector2 CarryOffsetTarget = new Vector2(0f, -12f);

		private const float ChaserStateMaxTime = 4f;

		public const float WalkSpeed = 64f;

		private const float LowFrictionMult = 0.35f;

		private const float LowFrictionAirMult = 0.5f;

		private const float LowFrictionStopTime = 0.15f;

		private const float HiccupTimeMin = 1.2f;

		private const float HiccupTimeMax = 1.8f;

		private const float HiccupDuckMult = 0.5f;

		private const float HiccupAirBoost = -60f;

		private const float HiccupAirVarTime = 0.15f;

		private const float GliderMaxFall = 40f;

		private const float GliderWindMaxFall = 0f;

		private const float GliderWindUpFall = -32f;

		public const float GliderFastFall = 120f;

		private const float GliderSlowFall = 24f;

		private const float GliderGravMult = 0.5f;

		private const float GliderMaxRun = 108.00001f;

		private const float GliderRunMult = 0.5f;

		private const float GliderUpMinPickupSpeed = -105f;

		private const float GliderDashMinPickupSpeed = -240f;

		private const float GliderWallJumpForceTime = 0.26f;

		private const float DashGliderBoostTime = 0.55f;

		public const int StNormal = 0;

		public const int StClimb = 1;

		public const int StDash = 2;

		public const int StSwim = 3;

		public const int StBoost = 4;

		public const int StRedDash = 5;

		public const int StHitSquash = 6;

		public const int StLaunch = 7;

		public const int StPickup = 8;

		public const int StDreamDash = 9;

		public const int StSummitLaunch = 10;

		public const int StDummy = 11;

		public const int StIntroWalk = 12;

		public const int StIntroJump = 13;

		public const int StIntroRespawn = 14;

		public const int StIntroWakeUp = 15;

		public const int StBirdDashTutorial = 16;

		public const int StFrozen = 17;

		public const int StReflectionFall = 18;

		public const int StStarFly = 19;

		public const int StTempleFall = 20;

		public const int StCassetteFly = 21;

		public const int StAttract = 22;

		public const int StIntroMoonJump = 23;

		public const int StFlingBird = 24;

		public const int StIntroThinkForABit = 25;

		public const string TalkSfx = "player_talk";

		public Vector2 Speed;

		public Facings Facing;

		public PlayerSprite Sprite;

		public PlayerHair Hair;

		public StateMachine StateMachine;

		public Vector2 CameraAnchor;

		public bool CameraAnchorIgnoreX;

		public bool CameraAnchorIgnoreY;

		public Vector2 CameraAnchorLerp;

		public bool ForceCameraUpdate;

		public Leader Leader;

		public VertexLight Light;

		public int Dashes;

		public float Stamina = 110f;

		public bool StrawberriesBlocked;

		public Vector2 PreviousPosition;

		public bool DummyAutoAnimate = true;

		public Vector2 ForceStrongWindHair;

		public Vector2? OverrideDashDirection;

		public bool FlipInReflection;

		public bool JustRespawned;

		public bool EnforceLevelBounds = true;

		private Level level;

		private Collision onCollideH;

		private Collision onCollideV;

		private bool onGround;

		private bool wasOnGround;

		private int moveX;

		private bool flash;

		private bool wasDucking;

		private int climbTriggerDir;

		private bool holdCannotDuck;

		private bool windMovedUp;

		private float idleTimer;

		private static Chooser<string> idleColdOptions = new Chooser<string>().Add("idleA", 5f).Add("idleB", 3f).Add("idleC", 1f);

		private static Chooser<string> idleNoBackpackOptions = new Chooser<string>().Add("idleA", 1f).Add("idleB", 3f).Add("idleC", 3f);

		private static Chooser<string> idleWarmOptions = new Chooser<string>().Add("idleA", 5f).Add("idleB", 3f);

		public int StrawberryCollectIndex;

		public float StrawberryCollectResetTimer;

		private Hitbox hurtbox;

		private float jumpGraceTimer;

		public bool AutoJump;

		public float AutoJumpTimer;

		private float varJumpSpeed;

		private float varJumpTimer;

		private int forceMoveX;

		private float forceMoveXTimer;

		private int hopWaitX;

		private float hopWaitXSpeed;

		private Vector2 lastAim;

		private float dashCooldownTimer;

		private float dashRefillCooldownTimer;

		public Vector2 DashDir;

		private float wallSlideTimer = 1.2f;

		private int wallSlideDir;

		private float climbNoMoveTimer;

		private Vector2 carryOffset;

		private Vector2 deadOffset;

		private float introEase;

		private float wallSpeedRetentionTimer;

		private float wallSpeedRetained;

		private int wallBoostDir;

		private float wallBoostTimer;

		private float maxFall;

		private float dashAttackTimer;

		private float gliderBoostTimer;

		public List<Player.ChaserState> ChaserStates;

		private bool wasTired;

		private HashSet<Trigger> triggersInside;

		private float highestAirY;

		private bool dashStartedOnGround;

		private bool fastJump;

		private int lastClimbMove;

		private float noWindTimer;

		private float dreamDashCanEndTimer;

		private Solid climbHopSolid;

		private Vector2 climbHopSolidPosition;

		private SoundSource wallSlideSfx;

		private SoundSource swimSurfaceLoopSfx;

		private float playFootstepOnLand;

		private float minHoldTimer;

		public Booster CurrentBooster;

		public Booster LastBooster;

		private bool calledDashEvents;

		private int lastDashes;

		private Sprite sweatSprite;

		private int startHairCount;

		private bool launched;

		private float launchedTimer;

		private float dashTrailTimer;

		private int dashTrailCounter;

		private bool canCurveDash;

		private float lowFrictionStopTimer;

		private float hiccupTimer;

		private List<Player.ChaserStateSound> activeSounds = new List<Player.ChaserStateSound>();

		private SoundHandle idleSfx;

		public bool MuffleLanding;

		private Vector2 gliderBoostDir;

		private float explodeLaunchBoostTimer;

		private float explodeLaunchBoostSpeed;

		private bool demoDashed;

		private readonly Hitbox normalHitbox = new Hitbox(8f, 11f, -4f, -11f);

		private readonly Hitbox duckHitbox = new Hitbox(8f, 6f, -4f, -6f);

		private readonly Hitbox normalHurtbox = new Hitbox(8f, 9f, -4f, -11f);

		private readonly Hitbox duckHurtbox = new Hitbox(8f, 4f, -4f, -6f);

		private readonly Hitbox starFlyHitbox = new Hitbox(8f, 8f, -4f, -10f);

		private readonly Hitbox starFlyHurtbox = new Hitbox(6f, 6f, -3f, -9f);

		private Vector2 normalLightOffset = new Vector2(0f, -8f);

		private Vector2 duckingLightOffset = new Vector2(0f, -3f);

		private List<Entity> temp = new List<Entity>();

		public static readonly Color NormalHairColor = Calc.HexToColor("AC3232");

		public static readonly Color FlyPowerHairColor = Calc.HexToColor("F2EB6D");

		public static readonly Color UsedHairColor = Calc.HexToColor("44B7FF");

		public static readonly Color FlashHairColor = Color.White;

		public static readonly Color TwoDashesHairColor = Calc.HexToColor("ff6def");

		public static readonly Color NormalBadelineHairColor = BadelineOldsite.HairColor;

		public static readonly Color UsedBadelineHairColor = Player.UsedHairColor;

		public static readonly Color TwoDashesBadelineHairColor = Player.TwoDashesHairColor;

		private float hairFlashTimer;

		private bool startHairCalled;

		public Color? OverrideHairColor;

		private Vector2 windDirection;

		private float windTimeout;

		private float windHairTimer;

		public Player.IntroTypes IntroType;

		private MirrorReflection reflection;

		public PlayerSpriteMode DefaultSpriteMode;

		private PlayerSpriteMode? nextSpriteMode;

		private const float LaunchedBoostCheckSpeedSq = 10000f;

		private const float LaunchedJumpCheckSpeedSq = 48400f;

		private const float LaunchedMinSpeedSq = 19600f;

		private const float LaunchedDoubleSpeedSq = 22500f;

		private const float SideBounceSpeed = 240f;

		private const float SideBounceThreshold = 240f;

		private const float SideBounceForceMoveXTime = 0.3f;

		private const float SpacePhysicsMult = 0.6f;

		private SoundHandle conveyorLoopSfx;

		private const float WallBoosterSpeed = -160f;

		private const float WallBoosterLiftSpeed = -80f;

		private const float WallBoosterAccel = 600f;

		private const float WallBoostingHopHSpeed = 100f;

		private const float WallBoosterOverTopSpeed = -180f;

		private const float IceBoosterSpeed = 40f;

		private const float IceBoosterAccel = 300f;

		private bool wallBoosting;

		private Vector2 beforeDashSpeed;

		private bool wasDashB;

		private const float SwimYSpeedMult = 0.5f;

		private const float SwimMaxRise = -60f;

		private const float SwimVDeccel = 600f;

		private const float SwimMax = 80f;

		private const float SwimUnderwaterMax = 60f;

		private const float SwimAccel = 600f;

		private const float SwimReduce = 400f;

		private const float SwimDashSpeedMult = 0.75f;

		private Vector2 boostTarget;

		private bool boostRed;

		private const float HitSquashNoMoveTime = 0.1f;

		private const float HitSquashFriction = 800f;

		private float hitSquashNoMoveTimer;

		private float? launchApproachX;

		private float summitLaunchTargetX;

		private float summitLaunchParticleTimer;

		private DreamBlock dreamBlock;

		private SoundSource dreamSfxLoop;

		private bool dreamJump;

		private const float StarFlyTransformDeccel = 1000f;

		private const float StarFlyTime = 2f;

		private const float StarFlyStartSpeed = 250f;

		private const float StarFlyTargetSpeed = 140f;

		private const float StarFlyMaxSpeed = 190f;

		private const float StarFlyMaxLerpTime = 1f;

		private const float StarFlySlowSpeed = 91f;

		private const float StarFlyAccel = 1000f;

		private const float StarFlyRotateSpeed = 5.5850534f;

		private const float StarFlyEndX = 160f;

		private const float StarFlyEndXVarJumpTime = 0.1f;

		private const float StarFlyEndFlashDuration = 0.5f;

		private const float StarFlyEndNoBounceTime = 0.2f;

		private const float StarFlyWallBounce = -0.5f;

		private const float StarFlyMaxExitY = 0f;

		private const float StarFlyMaxExitX = 140f;

		private const float StarFlyExitUp = -100f;

		private Color starFlyColor = Calc.HexToColor("ffd65c");

		private BloomPoint starFlyBloom;

		private float starFlyTimer;

		private bool starFlyTransforming;

		private float starFlySpeedLerp;

		private Vector2 starFlyLastDir;

		private SoundSource starFlyLoopSfx;

		private SoundSource starFlyWarningSfx;

		private FlingBird flingBird;

		private SimpleCurve cassetteFlyCurve;

		private float cassetteFlyLerp;

		private Vector2 attractTo;

		public bool DummyMoving;

		public bool DummyGravity = true;

		public bool DummyFriction = true;

		public bool DummyMaxspeed = true;

		private Facings IntroWalkDirection;

		private Tween respawnTween;

		public enum IntroTypes
		{
			Transition,
			Respawn,
			WalkInRight,
			WalkInLeft,
			Jump,
			WakeUp,
			Fall,
			TempleMirrorVoid,
			None,
			ThinkForABit
		}

		public struct ChaserStateSound
		{
			public string Event;

			public string Parameter;

			public float ParameterValue;

			public Player.ChaserStateSound.Actions Action;

			public enum Actions
			{
				Oneshot,
				Loop,
				Stop
			}
		}

		public struct ChaserState
		{
			public ChaserState(Player player)
			{
				this.Position = player.Position;
				this.TimeStamp = player.Scene.TimeActive;
				this.Animation = player.Sprite.CurrentAnimationID;
				this.Facing = player.Facing;
				this.OnGround = player.onGround;
				this.HairColor = player.Hair.Color;
				this.Depth = player.Depth;
				this.Scale = new Vector2(Math.Abs(player.Sprite.Scale.X) * (float)player.Facing, player.Sprite.Scale.Y);
				this.DashDirection = player.DashDir;
				List<Player.ChaserStateSound> activeSounds = player.activeSounds;
				this.Sounds = Math.Min(5, activeSounds.Count);
				this.sound0 = ((this.Sounds > 0) ? activeSounds[0] : default(Player.ChaserStateSound));
				this.sound1 = ((this.Sounds > 1) ? activeSounds[1] : default(Player.ChaserStateSound));
				this.sound2 = ((this.Sounds > 2) ? activeSounds[2] : default(Player.ChaserStateSound));
				this.sound3 = ((this.Sounds > 3) ? activeSounds[3] : default(Player.ChaserStateSound));
				this.sound4 = ((this.Sounds > 4) ? activeSounds[4] : default(Player.ChaserStateSound));
			}

			public Player.ChaserStateSound this[int index]
			{
				get
				{
					switch (index)
					{
					case 0:
						return this.sound0;
					case 1:
						return this.sound1;
					case 2:
						return this.sound2;
					case 3:
						return this.sound3;
					case 4:
						return this.sound4;
					default:
						return default(Player.ChaserStateSound);
					}
				}
			}

			public Vector2 Position;

			public float TimeStamp;

			public string Animation;

			public Facings Facing;

			public bool OnGround;

			public Color HairColor;

			public int Depth;

			public Vector2 Scale;

			public Vector2 DashDirection;

			private Player.ChaserStateSound sound0;

			private Player.ChaserStateSound sound1;

			private Player.ChaserStateSound sound2;

			private Player.ChaserStateSound sound3;

			private Player.ChaserStateSound sound4;

			public int Sounds;
		}
	}
}
