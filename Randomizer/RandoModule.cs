using FMOD.Studio;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;
using MonoMod.Cil;
using MonoMod.Utils;

namespace Celeste.Mod.Randomizer {
    public class RandoModule {
        public static RandoModule Instance;

        public RandoSettings Settings;
        public const int MAX_SEED_CHARS = 20;

		public RandoModule() {
			Instance = this;
			Settings = new RandoSettings();
		}

    }
}
