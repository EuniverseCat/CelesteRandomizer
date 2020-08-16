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
                        this.entering = true;
                        RandoModule.StartMe = newArea;
                        while (RandoModule.StartMe != null) {
                            Thread.Sleep(1);
                        }
                        this.builderThread = null;
                        this.entering = false;
                    });
                    builderThread.Start();
                }
				else
                    builderThread.Abort();
                ;
        }
    }
}
