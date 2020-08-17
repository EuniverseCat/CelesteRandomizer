using Celeste.Mod.Randomizer;
using Monocle;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Runtime.Serialization;
using MonoMod.RuntimeDetour;
using System.IO;
using Celeste;
using System.Threading;
using Celeste.Mod;

namespace Bruter {

	class Program {
		static int threads = 4;
		static int seeds = 100000;



		public static List<Program> pool = new List<Program>();
		static Stopwatch stopwatch;
		public int ID;
		const int divisionSize = 50000;
		static Random rng = new Random();
		bool done;

		StreamWriter swShort = null;
		StreamWriter swLong = null;
		static /*Linked*/List<MapOutput> outputShort = new List<MapOutput>();
		static /*Linked*/List<MapOutput> outputLong = new List<MapOutput>();

		delegate MapData d_MakeMap(RandoLogic self);
		static d_MakeMap orig_MakeMap;

		static void Main(string[] args) {
			RandoModule r = new RandoModule();

			Detour d_content = new Detour(typeof(Engine).GetMethod("get_ContentDirectory"), typeof(Program).GetMethod("ContentDirectory"));
			Detour d_mtn = new Detour(typeof(AreaData).GetMethod("ReloadMountainViews"), typeof(Program).GetMethod("EmptyMethod"));

			Directory.SetCurrentDirectory(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName);
			typeof(Everest.Content).GetProperty("PathContentOrig").SetValue(null, "!");

			AreaData.Load();
			RandoLogic.ProcessAreas();
			r.Settings.SetNormalMaps();


			//MapData ctor calls MapData.Load()
			//which would be nice if we weren't calling it 60 times a second because for us it doesn't do shit
			//and is thread unsafe
			Detour d_mapLoad = new Detour(typeof(MapData).GetMethod("Load", BindingFlags.NonPublic | BindingFlags.Instance), typeof(Program).GetMethod("EmptyMethod"));

			//Create a trampoline delegate to invoke MakeMap() externally
			Detour d_LogicMakeMap = new Detour(typeof(RandoLogic).GetMethod("MakeMap"), typeof(Program).GetMethod("OverrideMakeMap"));
			orig_MakeMap = d_LogicMakeMap.GenerateTrampoline<d_MakeMap>();

			stopwatch = Stopwatch.StartNew();
			Thread t = null;
			for (int i = 0; i < threads; i++) {
				Program p = new Program(i);
				t = new Thread(() => p.Brute(seeds));
				pool.Add(p);
				t.Start();
			}
			bool done = false;
			while (!done) {
				foreach (Program p in pool) {
					if (!p.done) {
						Thread.Sleep(2000);
						break;
					}
					done = true;
				}
			}
			lock (outputLong) {
				outputLong.Sort();
				foreach (var o in outputLong) Console.WriteLine(o);
			}

			Console.ReadKey();
			//d_content.Dispose();
			//d_mapLoad.Dispose();
			//d_mtn.Dispose();
			//d_LogicMakeMap.Dispose();
		}

		public static string ContentDirectory() => Path.Combine(Directory.GetCurrentDirectory(), "Content");
		public static void EmptyMethod() { }

		Program(int ID) {
			this.ID = ID;
		}

		void Brute(int numToTest, int divisionSize = 50000, int timeout = 300) {


			for (int i = 0; i < numToTest; i++) {

				genseed:
				string seed = "";
				lock (rng) {
					for (int j = 0; j < 6; j++) {
						seed += ((char)rng.Next(32, 127)).ToString();
					}
				}
				if (!MapOutput.InitialCheck(seed))
					goto genseed;
				RandoSettings settings = RandoModule.Instance.Settings.Clone();
				settings.Seed = seed;
				Thread randoThread = new Thread(() => RandoLogic.GenerateMap(settings));
				randoThread.Start();

				int ms = 0;
				while (randoThread.IsAlive) {
					if (ms > timeout) {
						randoThread.Abort();
						//Console.WriteLine("aborted attempt");
					}
					ms++;
					Thread.Sleep(1);
				}
			}



			done = true;
			Console.WriteLine($"{numToTest} seeds generated in {stopwatch.Elapsed}");
		}

		public static MapData OverrideMakeMap(RandoLogic self) {
			MapData data = orig_MakeMap(self);
			MapOutput _out = new MapOutput(self, data);
			lock (outputShort) {
				outputShort.Add(_out);
			}
			lock (outputLong) {
				outputLong.Add(_out);
			}
			return data;
		}

		void OutputData(StreamWriter stream, object idfk) {
			stream?.Dispose();
		}
	}
}
