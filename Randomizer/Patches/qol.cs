using System;
using System.Collections;
using Monocle;
using Microsoft.Xna.Framework;
using MonoMod.Cil;

namespace Celeste.Mod.Randomizer {
    public partial class RandoModule : EverestModule {
        private void LoadQol() {
            On.Celeste.TextMenu.MoveSelection += DisableMenuMovement;
            On.Celeste.Cassette.CollectRoutine += NeverCollectCassettes;
            On.Celeste.AngryOshiro.Added += DontSpawnTwoOshiros;
            On.Celeste.BadelineOldsite.Added += PlayBadelineCutscene;
            On.Celeste.Player.SummitLaunchUpdate += SummitLaunchReset;
            On.Celeste.Player.Added += DontMoveOnWakeup;
            IL.Celeste.Level.EnforceBounds += DontBlockOnTheo;
            IL.Celeste.TheoCrystal.Update += BeGracefulOnTransitions;
            IL.Celeste.SummitGem.OnPlayer += GemRefillsDashes;
            IL.Celeste.SummitGem.OnPlayer += DashlessAccessability;
            IL.Celeste.HeartGem.OnPlayer += DashlessAccessability;
            IL.Celeste.CS10_Gravestone.OnEnd += DontGiveTwoDashes;
            IL.Celeste.CS10_Gravestone.BadelineRejoin += DontGiveTwoDashes;
            IL.Celeste.CS07_Ascend.OnEnd += DontGiveTwoDashes;
            IL.Celeste.CS07_Ascend.Cutscene += DontGiveTwoDashes;
            IL.Celeste.AngryOshiro.ChaseUpdate += MoveOutOfTheWay;
            IL.Celeste.NPC03_Oshiro_Lobby.Added += PleaseDontStopTheMusic;
        }

        private void UnloadQol() {
            On.Celeste.TextMenu.MoveSelection -= DisableMenuMovement;
            On.Celeste.Cassette.CollectRoutine -= NeverCollectCassettes;
            On.Celeste.AngryOshiro.Added -= DontSpawnTwoOshiros;
            On.Celeste.BadelineOldsite.Added -= PlayBadelineCutscene;
            On.Celeste.Player.SummitLaunchUpdate -= SummitLaunchReset;
            On.Celeste.Player.Added -= DontMoveOnWakeup;
            IL.Celeste.Level.EnforceBounds -= DontBlockOnTheo;
            IL.Celeste.TheoCrystal.Update -= BeGracefulOnTransitions;
            IL.Celeste.SummitGem.OnPlayer -= GemRefillsDashes;
            IL.Celeste.SummitGem.OnPlayer -= DashlessAccessability;
            IL.Celeste.HeartGem.OnPlayer -= DashlessAccessability;
            IL.Celeste.CS10_Gravestone.OnEnd -= DontGiveTwoDashes;
            IL.Celeste.CS10_Gravestone.BadelineRejoin -= DontGiveTwoDashes;
            IL.Celeste.CS07_Ascend.OnEnd -= DontGiveTwoDashes;
            IL.Celeste.CS07_Ascend.Cutscene -= DontGiveTwoDashes;
            IL.Celeste.AngryOshiro.ChaseUpdate -= MoveOutOfTheWay;
            IL.Celeste.NPC03_Oshiro_Lobby.Added -= PleaseDontStopTheMusic;
        }

        private void DisableMenuMovement(On.Celeste.TextMenu.orig_MoveSelection orig, TextMenu self, int direction, bool wiggle = false) {
            if (self is DisablableTextMenu newself && newself.DisableMovement) {
                return;
            }
            orig(self, direction, wiggle);
        }

        private IEnumerator NeverCollectCassettes(On.Celeste.Cassette.orig_CollectRoutine orig, Cassette self, Player player) {
            var thing = orig(self, player);
            while (thing.MoveNext()) {  // why does it not let me use foreach?
                yield return thing.Current;
            }

            if (this.InRandomizer) {
                var level = self.Scene as Level;
                level.Session.Cassette = false;
            }
        }

        private void PlayBadelineCutscene(On.Celeste.BadelineOldsite.orig_Added orig, BadelineOldsite self, Scene scene) {
            orig(self, scene);
            var level = scene as Level;
            if (!level.Session.GetFlag("evil_maddy_intro") && level.Session.Level.StartsWith("Celeste/2-OldSite/A/3")) {
                foreach (var c in self.Components) {
                    if (c is Coroutine) {
                        self.Components.Remove(c);
                        break;
                    }
                }

                self.Hovering = false;
                self.Visible = true;
                self.Hair.Visible = false;
                self.Sprite.Play("pretendDead", false, false);
                if (level.Session.Area.Mode == AreaMode.Normal) {
                    level.Session.Audio.Music.Event = null;
                    level.Session.Audio.Apply(false);
                }
                scene.Add(new CS02_BadelineIntro(self));
            }
        }

        private int SummitLaunchReset(On.Celeste.Player.orig_SummitLaunchUpdate orig, Player self) {
            var level = Engine.Scene as Level;
            if (this.InRandomizer && self.Y < level.Bounds.Y - 8) {
                // teleport to spawn point
                self.Position = level.Session.RespawnPoint.Value;

                // reset camera
                var tmp = level.CameraLockMode;
                level.CameraLockMode = Level.CameraLockModes.None;
                level.Camera.Position = level.GetFullCameraTargetAt(self, self.Position);
                level.CameraLockMode = tmp;
                level.CameraUpwardMaxY = level.Camera.Y + 180f;

                // remove effects
                AscendManager mgr = null;
                Entity fader = null;
                HeightDisplay h = null;
                BadelineDummy b = null;
                foreach (var ent in Engine.Scene.Entities) {
                    if (ent is AscendManager manager) {
                        mgr = manager;
                    }
                    if (ent.GetType().Name == "Fader") {
                        fader = ent;
                    }
                    if (ent is HeightDisplay heightDisplay) {
                        h = heightDisplay;
                    }
                    if (ent is BadelineDummy bd) {
                        b = bd;
                    }
                }
                if (mgr != null) {
                    level.Remove(mgr);
                }
                if (fader != null) {
                    level.Remove(fader);
                }
                if (h != null) {
                    level.Remove(h);
                }
                if (b != null) {
                    level.Remove(b);
                }
                level.NextTransitionDuration = 0.65f;

                // return to normal
                return Player.StNormal;
            } else {
                return orig(self);
            }
        }

        private void DontSpawnTwoOshiros(On.Celeste.AngryOshiro.orig_Added orig, AngryOshiro self, Scene scene) {
            orig(self, scene);
            var level = scene as Level;
            if (!level.Session.GetFlag("oshiro_resort_roof") && level.Session.Level.StartsWith("Celeste/3-CelestialResort/A/roof00")) {
                self.RemoveSelf();
            }
        }

        private void DontMoveOnWakeup(On.Celeste.Player.orig_Added orig, Player self, Scene scene) {
            orig(self, scene);
            if (this.InRandomizer) {
                self.JustRespawned = true;
            }
        }

        private void DontBlockOnTheo(ILContext il) {
            ILCursor cursor = new ILCursor(il);
            cursor.TryGotoNext(MoveType.After, instr => instr.MatchCallvirt<Monocle.Tracker>("GetEntity"));
            cursor.EmitDelegate<Func<TheoCrystal, TheoCrystal>>((theo) => {
                return this.InRandomizer ? null : theo;
            });
        }

        private void BeGracefulOnTransitions(ILContext il) {
            ILCursor cursor = new ILCursor(il);
            while (cursor.TryGotoNext(MoveType.Before, instr => instr.MatchCallvirt<Level>("get_Bounds"))) {
                cursor.Remove();
                cursor.EmitDelegate<Func<Level, Rectangle>>((level) => {
                    if (level.Transitioning && this.InRandomizer) {
                        return level.Session.MapData.Bounds;
                    }
                    return level.Bounds;
                });
            }
        }

        private void DashlessAccessability(ILContext il) {
            ILCursor cursor = new ILCursor(il);
            cursor.TryGotoNext(MoveType.After, instr => instr.MatchCallvirt<Player>("get_DashAttacking"));
            cursor.EmitDelegate<Func<bool, bool>>((dobreak) => {
                if (this.InRandomizer && this.Settings.Dashes == NumDashes.Zero) {
                    return true;
                }
                return dobreak;
            });
        }

        private void GemRefillsDashes(ILContext il) {
            ILCursor cursor = new ILCursor(il);
            cursor.TryGotoNext(MoveType.After, instr => instr.MatchCallvirt<Monocle.Entity>("Add"));
            cursor.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_1);
            cursor.EmitDelegate<Action<Player>>((player) => {
                player.RefillDash();
            });
        }

        private void DontGiveTwoDashes(ILContext il) {
            ILCursor cursor = new ILCursor(il);
            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcI4(2))) {
                cursor.EmitDelegate<Func<int, int>>((dashes) => {
                    if (this.InRandomizer) {
                        return (Engine.Scene as Level).Session.Inventory.Dashes;
                    }
                    return dashes;
                });
            }
        }

        private void MoveOutOfTheWay(ILContext il) {
            ILCursor cursor = new ILCursor(il);
            cursor.TryGotoNext(MoveType.After, instr => instr.MatchCallvirt<AngryOshiro>("get_TargetY"));
            cursor.EmitDelegate<Func<float, float>>((targety) => {
                if (this.InRandomizer) {
                    var level = Engine.Scene as Level;
                    var player = level.Tracker.GetEntity<Player>();
                    if (player.Facing == Facings.Left && player.X < level.Bounds.X + 70) {
                        return targety - 50;
                    }
                }
                return targety;
            });
        }

        private void PleaseDontStopTheMusic(ILContext il) {
            var cursor = new ILCursor(il);
            cursor.TryGotoNext(MoveType.After, instr => instr.MatchCallvirt<AudioTrackState>("set_Event"));
            if (!cursor.TryGotoNext(MoveType.Before, instr => instr.MatchCallvirt<AudioTrackState>("set_Event"))) {
                throw new Exception("Could not find patching spot");
            }
            cursor.Remove();
            cursor.EmitDelegate<Action<AudioTrackState, string>>((music, track) => {
                if (!this.InRandomizer) {
                    music.Event = track;
                }
            });
        }
    }

    public class DisablableTextMenu : TextMenu {
        public bool DisableMovement;
    }
}