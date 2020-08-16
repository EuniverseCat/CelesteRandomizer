﻿using FMOD.Studio;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;
using System.Linq;
using MonoMod.Cil;
using MonoMod.Utils;
using System.Collections.Generic;

namespace Celeste.Mod.Randomizer {
    public class RandoModule {
        public static RandoModule Instance;
        public override Type SettingsType => typeof(RandoModuleSettings);
        public RandoModuleSettings SavedData {
            get {
                var result = Instance._Settings as RandoModuleSettings;
                if (result.CurrentVersion != this.Metadata.VersionString) {
                    result.CurrentVersion = this.Metadata.VersionString;
                    result.BestTimes = new Dictionary<uint, long>();
                    result.BestSetSeedTimes = new Dictionary<Ruleset, RecordTuple>();
                    result.BestRandomSeedTimes = new Dictionary<Ruleset, RecordTuple>();
                }
                return result;
            }
        }

        public RandoSettings Settings;
        public const int MAX_SEED_CHARS = 20;

		public RandoModule() {
			Instance = this;
			Settings = new RandoSettings();
		}
    }
}