using Monocle;
using Microsoft.Xna.Framework;
using System;
using System.Collections;
using System.Threading;

namespace Celeste.Mod.Randomizer {
    public class OuiRandoSettings {
        private static Thread builderThread;

        public static RandoSettings Settings {
            get {
                return RandoModule.Instance.Settings;
            }
        }

        public static void BuildMapThreadSafe() {

                if (builderThread == null) {

                    builderThread = new Thread(() => {
                        Settings.Enforce();
                        AreaKey newArea;
                        try {
                            newArea = RandoLogic.GenerateMap(Settings);
                        }
						catch (ThreadAbortException) {
                            return;
                        }
						catch (Exception e) {
                            return;
                        }

                    });
                    builderThread.Start();
                }
				else
                    builderThread.Abort();
                ;
        }
    }
}
