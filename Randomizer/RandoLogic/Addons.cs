using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.Randomizer {
	public class Addons {
		static Dictionary<string, List<string>> blacklist = new Dictionary<string, List<string>>();

		static Addons() {
			using (StreamReader sr =  new StreamReader("blacklist.tas")) {
				string s;
				string currentLvl = "";
				while ((s = sr.ReadLine()) != null) {
					if (!s.StartsWith("#")) continue;
					string[] lvl = s.Split('_');

					if (lvl.Length == 1) {
						currentLvl = s.Substring(1);
						blacklist.Add(currentLvl, new List<string>());
						continue;
					}

					string lvlName = lvl[1];

					blacklist[currentLvl].Add(lvlName);
				}

			}
		}

		public static bool CheckScreen(string level, string screen) {
			if (blacklist.TryGetValue(level, out var screens))
				return !screens.Contains(screen.Substring(2));
			return true;
		}
	}
}
