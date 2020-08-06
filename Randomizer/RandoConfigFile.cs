﻿using System;
using System.Collections.Generic;
using System.IO;
using YamlDotNet.Serialization;

namespace Celeste.Mod.Randomizer {
    public class RandoConfigFile {
        public List<RandoConfigRoom> ASide { get; set; }
        public List<RandoConfigRoom> BSide { get; set; }
        public List<RandoConfigRoom> CSide { get; set; }

        public static RandoConfigFile Load(AreaData area) {

			string fullpath = Directory.GetCurrentDirectory() + "/Randomizer/Config/Celeste";

			string file;
			if (area.ID == 10)
				file = Directory.GetFiles(fullpath, "Lost*")[0];
			else
				file = Directory.GetFiles(fullpath, area.ID + "*.rando.yaml")[0];
            using (StreamReader reader = new StreamReader(file)) {
                return new DeserializerBuilder().IgnoreUnmatchedProperties().Build().Deserialize<RandoConfigFile>(reader);
            }
        }

        public static void YamlSkeleton(MapData map) {
            foreach (LevelData lvl in map.Levels) {
                List<Hole> holes = RandoLogic.FindHoles(lvl);
                ScreenDirection lastDirection = ScreenDirection.Up;
                int holeIdx = -1;
                foreach (Hole hole in holes) {
                    if (hole.Side == lastDirection) {
                        holeIdx++;
                    } else {
                        holeIdx = 0;
                        lastDirection = hole.Side;
                    }

                    LevelData targetlvl = map.GetAt(hole.LowCoord(lvl.Bounds)) ?? map.GetAt(hole.HighCoord(lvl.Bounds));
                }
            }
        }

        public static void YamlSkeleton(AreaData area) {
            if (area.Mode[0] != null) {
                YamlSkeleton(area.Mode[0].MapData);
            }
            if (area.Mode.Length > 1 && area.Mode[1] != null) {
                YamlSkeleton(area.Mode[1].MapData);
            }
            if (area.Mode.Length > 2 && area.Mode[2] != null) {
                YamlSkeleton(area.Mode[2].MapData);
            }
        }

        public Dictionary<String, RandoConfigRoom> GetRoomMapping(AreaMode mode) {
            List<RandoConfigRoom> rooms = null;
            switch (mode) {
                case AreaMode.Normal:
                default:
                    rooms = this.ASide;
                    break;
                case AreaMode.BSide:
                    rooms = this.BSide;
                    break;
                case AreaMode.CSide:
                    rooms = this.CSide;
                    break;
            }

            if (rooms == null) {
                return null;
            }

            var result = new Dictionary<String, RandoConfigRoom>();
            foreach (RandoConfigRoom room in rooms) {
                result.Add(room.Room, room);
            }

            return result;
        }
    }

    public class RandoConfigRoom {
        public String Room;
        public List<RandoConfigCollectable> Collectables = new List<RandoConfigCollectable>();
        public List<RandoConfigHole> Holes { get; set; }
        public List<RandoConfigRoom> Subrooms { get; set; }
        public List<RandoConfigInternalEdge> InternalEdges { get; set; }
        public bool End { get; set; }
        public bool Hub { get; set; }
        public List<RandoConfigEdit> Tweaks { get; set; }
        public RandoConfigCoreMode Core { get; set; }
        public List<RandoConfigRectangle> ExtraSpace { get; set; }
        public float? Worth;
    }

    public class RandoConfigRectangle {
        public int X, Y;
        public int Width, Height;
    }

    public class RandoConfigHole {
        public ScreenDirection Side { get; set; }
        public int Idx { get; set; }
        public int? LowBound { get; set; }
        public int? HighBound { get; set; }
        public bool? HighOpen { get; set; }

        public RandoConfigReq ReqIn { get; set; }
        public RandoConfigReq ReqOut { get; set; }
        public RandoConfigReq ReqBoth {
            get {
                return null;
            }

            set {
                this.ReqIn = value;
                this.ReqOut = value;
            }
        }
        public HoleKind Kind { get; set; }
        public int? Launch;
    }

    public class RandoConfigCollectable {
        public int? Idx;
        public int? X;
        public int? Y;
        public bool MustFly;
    }

    public class RandoConfigInternalEdge {
        public String To { get; set; }
        public RandoConfigReq ReqIn { get; set; }
        public RandoConfigReq ReqOut { get; set; }
        public RandoConfigReq ReqBoth {
            get {
                return null;
            }

            set {
                this.ReqIn = value;
                this.ReqOut = value;
            }
        }

        public enum SplitKind {
            TopToBottom,
            BottomToTop,
            LeftToRight,
            RightToLeft,
        }

        public SplitKind? Split;

        public int? Collectable;
        public bool MustFly;
    }

    public class RandoConfigReq {
        public List<RandoConfigReq> And { get; set; }
        public List<RandoConfigReq> Or { get; set; }

        public Difficulty Difficulty { get; set; }
        public NumDashes? Dashes { get; set; }
        public bool Key;
        public int? KeyholeID;
    }

    public class RandoConfigEdit {
        public String Name { get; set; }
        public int? ID { get; set; }
        public float? X { get; set; }
        public float? Y { get; set; }
        public RandoConfigUpdate Update { get; set; }
    }

    public class RandoConfigUpdate {
        public bool Remove { get; set; }
        public bool Add { get; set; }
        public bool Default { get; set; }

        public float? X { get; set; }
        public float? Y { get; set; }
        public int? Width { get; set; }
        public int? Height { get; set; }
        public List<RandoConfigNode> Nodes { get; set; }
        public Dictionary<string, string> Values { get; set; }
    }

    public class RandoConfigNode {
        public int Idx { get; set; }
        public float? X { get; set; }
        public float? Y { get; set; }
    }

    public class RandoConfigCoreMode {
        private Session.CoreModes? left, right, up, down;
        public Session.CoreModes All = Session.CoreModes.None;

        public Session.CoreModes Left {
            get { return left ?? All; }
            set { left = value; }
        }

        public Session.CoreModes Right {
            get { return right ?? All; }
            set { right = value; }
        }

        public Session.CoreModes Up {
            get { return up ?? All; }
            set { up = value; }
        }

        public Session.CoreModes Down {
            get { return down ?? All; }
            set { down = value; }
        }
    }
}
