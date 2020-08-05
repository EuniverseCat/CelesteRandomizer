using Celeste;
using Celeste.Mod.Randomizer;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bruter {
	class MapOutput : IComparable {
		string seed;
		public double weight;
		string info = "";
		public MapOutput(RandoLogic logic, MapData data) {
			seed = logic.Seed;
			weight = CalculateWeight(logic, logic.Map);
		}

		public double CalculateWeight(RandoLogic logic, LinkedMap map) {
			double weight = 0;
			Vector2? last = null;
			foreach (Vector2 loc in logic.NodePoints) {
				if (last == null) {
					last = loc;
					continue;
				}
				//assume constant average speed of 200, i guess
				float dist = (loc - last.Value).Length();
				dist *= 60f / 200f;
				weight += dist;

				weight += 40;

				last = loc;
			}
			return weight;
		}
		
		public int CompareTo(object obj) {
			if (!(obj is MapOutput other))
				return -1;
			return other.weight.CompareTo(this.weight);

		}
		public override string ToString() {
			return $"{seed} {weight} {info}";
		}
	}
}
