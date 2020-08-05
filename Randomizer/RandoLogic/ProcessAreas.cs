﻿using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Celeste.Mod.Randomizer {
    public partial class RandoLogic {
        public static List<StaticRoom> AllRooms = new List<StaticRoom>();
        public static List<AreaKey> AvailableAreas = new List<AreaKey>();

        public static List<Hole> FindHoles(LevelData data) {
            List<Hole> result = new List<Hole>();
            Regex regex = new Regex("\\r\\n|\\n\\r|\\n|\\r");
            string[] lines = regex.Split(data.Solids);

            // returns whether the given tile is clear
            bool lookup(int x, int y) {
                if (y >= lines.Length) {
                    return true;
                }
                string line = lines[y];
                if (x >= line.Length) {
                    return true;
                }
                return line[x] == '0';
            }

            int curHole = -1;
            bool clear;

            //Logger.Log("findholes", $"{data.TileBounds.Width} x {data.TileBounds.Height} => {data.Solids.Length}");
            //Logger.Log("findholes", $"\n{data.Solids}");

            for (int i = 0; i < data.TileBounds.Width; i++) {
                clear = lookup(i, 0);
                if (clear && curHole == -1) {
                    curHole = i;
                } else if (!clear && curHole != -1) {
                    result.Add(new Hole(ScreenDirection.Up, curHole, i - 1, false));
                    curHole = -1;
                }
            }
            if (curHole != -1) {
                result.Add(new Hole(ScreenDirection.Up, curHole, data.TileBounds.Width - 1, true));
            }

            curHole = -1;
            for (int i = 0; i < data.TileBounds.Height; i++) {
                clear = lookup(data.TileBounds.Width - 1, i);
                if (clear && curHole == -1) {
                    curHole = i;
                } else if (!clear && curHole != -1) {
                    result.Add(new Hole(ScreenDirection.Right, curHole, i - 1, false));
                    curHole = -1;
                }
            }
            if (curHole != -1) {
                result.Add(new Hole(ScreenDirection.Right, curHole, data.TileBounds.Height - 1, true));
            }

            curHole = -1;

            for (int i = 0; i < data.TileBounds.Height; i++) {
                clear = lookup(0, i);
                if (clear && curHole == -1) {
                    curHole = i;
                } else if (!clear && curHole != -1) {
                    result.Add(new Hole(ScreenDirection.Left, curHole, i - 1, false));
                    curHole = -1;
                }
            }
            if (curHole != -1) {
                result.Add(new Hole(ScreenDirection.Left, curHole, data.TileBounds.Height - 1, true));
            }

            curHole = -1;

            for (int i = 0; i < data.TileBounds.Width; i++) {
                clear = lookup(i, data.TileBounds.Height - 1);
                if (clear && curHole == -1) {
                    curHole = i;
                } else if (!clear && curHole != -1) {
                    result.Add(new Hole(ScreenDirection.Down, curHole, i - 1, false));
                    curHole = -1;
                }
            }
            if (curHole != -1) {
                result.Add(new Hole(ScreenDirection.Down, curHole, data.TileBounds.Width - 1, true));
            }

            return result;
        }

        private static List<StaticRoom> ProcessMap(MapData map, Dictionary<String, RandoConfigRoom> config) {
            var result = new List<StaticRoom>();

            foreach (LevelData level in map.Levels) {
                if (level.Dummy) {
                    continue;
                }
                if (!config.TryGetValue(level.Name, out RandoConfigRoom roomConfig)) {
                    continue;
                }
                if (roomConfig == null || roomConfig.Holes == null) {
                    continue;
                }
                var holes = RandoLogic.FindHoles(level);
                result.Add(new StaticRoom(map.Area, roomConfig, level, holes));
            }

            return result;
        }

        private static void ProcessArea(AreaData area, AreaMode? mode = null) {
            RandoConfigFile config = RandoConfigFile.Load(area);
            if (config == null) {
                return;
            }

            for (int i = 0; i < area.Mode.Length; i++) {
                if (area.Mode[i] == null) {
                    continue;
                }
                if (mode != null && mode != (AreaMode?)i) {
                    continue;
                }
                var mapConfig = config.GetRoomMapping((AreaMode)i);
                if (mapConfig == null) {
                    continue;
                }

                RandoLogic.AllRooms.AddRange(RandoLogic.ProcessMap(area.Mode[i].MapData, mapConfig));
                RandoLogic.AvailableAreas.Add(new AreaKey(area.ID, (AreaMode)i));
            }
        }

        public static void ProcessAreas() {
            if (RandoLogic.AllRooms.Count != 0) {
                return;
            }

            foreach (var area in AreaData.Areas) {
                RandoLogic.ProcessArea(area);
            }
        }


    }
}
