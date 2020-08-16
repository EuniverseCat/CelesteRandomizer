﻿using System;
using System.Collections.Generic;

namespace Celeste.Mod.Randomizer {
    public enum SeedType {
        Random,
        Custom,
        Last
    }

    public enum Ruleset {
        Custom,
        A,
        B,
        C,
        D,
        E,
        F,
        Last
    }

    public enum LogicType {
        Pathway,
        Labyrinth,
        Last
    }

    public enum MapLength {
        Short,
        Medium,
        Long,
        Enormous,
        Last
    }

    public enum NumDashes {
        Zero,
        One, 
        Two,
        Last
    }

    public enum Difficulty {
        Normal,
        Hard,
        Expert,
        Perfect,
        Last
    }

    public enum ShineLights {
        Off,
        Hubs,
        On,
        Last
    }

    public enum Darkness {
        Never,
        Vanilla,
        Always,
        Last,
    }

    public class RandoSettings {
        public SeedType SeedType;
        public string Seed = "achene";
        public Ruleset Rules;
        public bool RepeatRooms;
        public bool EnterUnknown;
        public bool SpawnGolden;
        public LogicType Algorithm;
        public MapLength Length;
        public NumDashes Dashes = NumDashes.One;
        public Difficulty Difficulty;
        public ShineLights Lights = ShineLights.Hubs;
        public Darkness Darkness = Darkness.Vanilla;
        private HashSet<AreaKeyNotStupid> IncludedMaps = new HashSet<AreaKeyNotStupid>();

        public void Enforce() {
            if (this.SeedType == SeedType.Random) {
                this.Seed = "";
                var r = new Random();
                for (int i = 0; i < 6; i++) {
                    var val = r.Next(36);
                    if (val < 10) {
                        this.Seed += ((char)('0' + val)).ToString();
                    } else {
                        this.Seed += ((char)('a' + val - 10)).ToString();
                    }
                }
            }
            switch (this.Rules) {
                case Ruleset.A:
                    this.SetNormalMaps();
                    this.RepeatRooms = false;
                    this.EnterUnknown = false;
                    this.Algorithm = LogicType.Pathway;
                    this.Length = MapLength.Short;
                    this.Dashes = NumDashes.One;
                    this.Difficulty = Difficulty.Normal;
                    this.Lights = ShineLights.Hubs;
                    this.Darkness = Darkness.Never;
                    break;
                case Ruleset.B:
                    this.SetNormalMaps();
                    this.RepeatRooms = false;
                    this.EnterUnknown = false;
                    this.Algorithm = LogicType.Pathway;
                    this.Length = MapLength.Medium;
                    this.Dashes = NumDashes.Two;
                    this.Difficulty = Difficulty.Normal;
                    this.Lights = ShineLights.Hubs;
                    this.Darkness = Darkness.Never;
                    break;
                case Ruleset.C:
                    this.SetNormalMaps();
                    this.RepeatRooms = false;
                    this.EnterUnknown = false;
                    this.Algorithm = LogicType.Pathway;
                    this.Length = MapLength.Medium;
                    this.Dashes = NumDashes.One;
                    this.Difficulty = Difficulty.Expert;
                    this.Lights = ShineLights.Hubs;
                    this.Darkness = Darkness.Vanilla;
                    break;
                case Ruleset.D:
                    this.SetNormalMaps();
                    this.RepeatRooms = false;
                    this.EnterUnknown = false;
                    this.Algorithm = LogicType.Pathway;
                    this.Length = MapLength.Long;
                    this.Dashes = NumDashes.Two;
                    this.Difficulty = Difficulty.Expert;
                    this.Lights = ShineLights.Hubs;
                    this.Darkness = Darkness.Vanilla;
                    break;
                case Ruleset.E:
                    this.SetNormalMaps();
                    this.RepeatRooms = false;
                    this.EnterUnknown = false;
                    this.Algorithm = LogicType.Labyrinth;
                    this.Length = MapLength.Medium;
                    this.Dashes = NumDashes.One;
                    this.Difficulty = Difficulty.Normal;
                    this.Lights = ShineLights.Hubs;
                    this.Darkness = Darkness.Never;
                    break;
                case Ruleset.F:
                    this.SetNormalMaps();
                    this.RepeatRooms = false;
                    this.EnterUnknown = false;
                    this.Algorithm = LogicType.Labyrinth;
                    this.Length = MapLength.Medium;
                    this.Dashes = NumDashes.Two;
                    this.Difficulty = Difficulty.Hard;
                    this.Lights = ShineLights.Hubs;
                    this.Darkness = Darkness.Never;
                    break;
            }
        }

        public void SetNormalMaps() {
            this.DisableAllMaps();
            foreach (var key in RandoLogic.AvailableAreas) {
                    this.EnableMap(key);
            }
        }

        private IEnumerable<uint> HashParts() {
            yield return 0;
            yield return 3;
            yield return 2;
            yield return this.IntSeed;
            yield return RepeatRooms ? 1u : 0u;
            yield return EnterUnknown ? 1u : 0u;
            yield return (uint)Algorithm;
            yield return (uint)Length;
            yield return (uint)Dashes;
            yield return (uint)Difficulty;
            yield return (uint)Lights;
            yield return (uint)Darkness;
        }

        public string Hash {
            get {
                // djb2 impl
                uint h = 5381;
                foreach (var i in this.HashParts()) {
                    h = ((h << 5) + h) + i;
                }
                return h.ToString();
            }
        }

        public uint IntSeed {
            get {
                // djb2 impl
                uint h = 5381;
                foreach (var i in this.Seed) {
                    h = ((h << 5) + h) + i;
                }
                return h;
            }
        }

        public int LevelCount {
            get {
                int sum = 0;
                foreach (var room in RandoLogic.AllRooms) {
                    if (this.MapIncluded(room.Area)) {
                        sum++;
                    }
                }
                return sum;
            }
        }

        public struct AreaKeyNotStupid {
            public int ID;
            public AreaMode Mode;

            public AreaKeyNotStupid(int ID, AreaMode Mode) {
                this.ID = ID;
                this.Mode = Mode;
            }

            public AreaKeyNotStupid(AreaKey Stupid) {
                this.ID = Stupid.ID; 
                this.Mode = Stupid.Mode;
            }

            public AreaKey Stupid {
                get {
                    return new AreaKey(this.ID, this.Mode);
                }
            }
        }

        public bool MapIncluded(AreaKey map) {
            return this.IncludedMaps.Contains(new AreaKeyNotStupid(map));
        }

        public void EnableMap(AreaKey map) {
            this.IncludedMaps.Add(new AreaKeyNotStupid(map));
        }

        public void DisableMap(AreaKey map) {
            this.IncludedMaps.Remove(new AreaKeyNotStupid(map));
        }

        public void DisableAllMaps() {
            this.IncludedMaps.Clear();
        }

        public IEnumerable<AreaKey> EnabledMaps {
            get {
                var result = new List<AreaKey>();
                foreach (var area in this.IncludedMaps) {
                    result.Add(area.Stupid);
                }
                return result;
            }
        }

		public RandoSettings Clone() {
			return new RandoSettings() {
				SeedType = this.SeedType,
				Seed = this.Seed,
				Rules = this.Rules,
				RepeatRooms = this.RepeatRooms,
				EnterUnknown = this.EnterUnknown,
				SpawnGolden = this.SpawnGolden,
				Algorithm = this.Algorithm,
				Length = this.Length,
				Dashes = this.Dashes,
				Difficulty = this.Difficulty,
				Lights = this.Lights,
				Darkness = this.Darkness,
				IncludedMaps = this.IncludedMaps
			};
		}
    }
}
