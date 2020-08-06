﻿using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;

namespace Celeste.Mod.Randomizer {
    public class LinkedMap {
        public List<LinkedRoom> Rooms = new List<LinkedRoom>();
        private LinkedRoom CachedHit;
        private int nonce;
        public float Worth { get; private set; }

        public bool AreaFree(Rectangle rect) {
            if (this.CachedHit != null) {
                if (CachedHit.Bounds.Intersects(rect)) {
                    return false;
                }
                foreach (var r in CachedHit.ExtraBounds) {
                    if (r.Intersects(rect)) {
                        return false;
                    }
                }
            }

            foreach (var room in this.Rooms) {
                if (room.Bounds.Intersects(rect)) {
                    this.CachedHit = room;
                    return false;
                }
                foreach (var r in room.ExtraBounds) {
                    if (r.Intersects(rect)) {
                        this.CachedHit = room;
                        return false;
                    }
                }
            }

            return true;
        }

        public bool AreaFree(LinkedRoom room) {
            if (!this.AreaFree(room.Bounds)) {
                return false;
            }
            foreach (var r in room.ExtraBounds) {
                if (!this.AreaFree(r)) {
                    return false;
                }
            }
            return true;
        }

        public bool HoleFree(LinkedRoom level, Hole hole) {
            Vector2 outward = hole.Side.Unit() * (hole.Side == ScreenDirection.Up || hole.Side == ScreenDirection.Down ? 180 : 320);
            Vector2 pt1v = hole.LowCoord(level.Bounds) + outward;
            Vector2 pt2v = hole.HighCoord(level.Bounds) + outward;
            Point pt1 = new Point((int)pt1v.X, (int)pt1v.Y);
            Point pt2 = new Point((int)pt2v.X, (int)pt2v.Y);

            foreach (var room in this.Rooms) {
                if (room.Bounds.Contains(pt1)) {
                    return false;
                }
            }
            foreach (var room in this.Rooms) {
                if (room.Bounds.Contains(pt2)) {
                    return false;
                }
            }
            return true;
        }

        public void AddRoom(LinkedRoom room) {
            this.Rooms.Add(room);
            this.Worth += room.Static.Worth;
        }

        public void RemoveRoom(LinkedRoom room) {
            this.Rooms.Remove(room);
            this.Worth -= room.Static.Worth;
            if (room == this.CachedHit) {
                this.CachedHit = null;
            }
        }

        public void FillMap(MapData map, Random random) {
            foreach (var room in this.Rooms) {
                map.Levels.Add(room.Bake(this.nonce++, random));
            }
        }

        public int Count {
            get {
                return this.Rooms.Count;
            }
        }

        public void Clear() {
            this.Rooms.Clear();
            this.CachedHit = null;
            this.nonce = 0;
        }
    }

    public class LinkedRoom {
        public Rectangle Bounds;
        public List<Rectangle> ExtraBounds;
        public StaticRoom Static;
        public Dictionary<string, LinkedNode> Nodes = new Dictionary<string, LinkedNode>();
        public HashSet<int> UsedKeyholes = new HashSet<int>();

        public LinkedRoom(StaticRoom Room, Vector2 Position) {
            this.Static = Room;
            this.Bounds = new Rectangle((int)Position.X, (int)Position.Y, Room.Level.Bounds.Width, Room.Level.Bounds.Height);
            this.ExtraBounds = new List<Rectangle>();
            foreach (var r in Room.ExtraSpace) {
                this.ExtraBounds.Add(new Rectangle((int)Position.X + r.X, (int)Position.Y + r.Y, r.Width, r.Height));
            }
            foreach (var staticnode in Room.Nodes.Values) {
                var node = new LinkedNode() { Static = staticnode, Room = this };
                this.Nodes.Add(staticnode.Name, node);
            }
        }

        public virtual LevelData Bake(int? nonce, Random random) => Static.MakeLevelData(new Vector2(this.Bounds.Left, this.Bounds.Top), nonce);

    }

    public class LinkedNode : IComparable<LinkedNode>, IComparable {
        public StaticNode Static;
        public LinkedRoom Room;
        public List<LinkedEdge> Edges = new List<LinkedEdge>();
        public Dictionary<StaticCollectable, LinkedCollectable> Collectables = new Dictionary<StaticCollectable, LinkedCollectable>();

        public int CompareTo(LinkedNode obj) {
            return 0;
        }

        public int CompareTo(object obj) {
            if (!(obj is LinkedNode other)) {
                throw new ArgumentException("Must compare LinkedNode to LinkedNode");
            }
            return this.CompareTo(other);
        }

        public enum LinkedCollectable {
            Strawberry,
            WingedStrawberry,
            Key,
            Gem1,
            Gem2,
            Gem3,
            Gem4,
            Gem5,
            Gem6,
        }

        public IEnumerable<LinkedNode> Successors(Capabilities capsForward, Capabilities capsReverse, bool onlyInternal = false) {
            foreach (var iedge in this.Static.Edges) {
                if (iedge.NodeTarget == null) {
                    continue;
                }
                if (capsForward != null && !iedge.ReqOut.Able(capsForward)) {
                    continue;
                }
                if (capsReverse != null && !iedge.ReqIn.Able(capsReverse)) {
                    continue;
                }
                yield return this.Room.Nodes[iedge.NodeTarget.Name];
            }

            if (!onlyInternal) {
                foreach (var edge in this.Edges) {
                    var check1 = edge.CorrespondingEdge(this);
                    var check2 = edge.OtherEdge(this);

                    if (capsForward != null && (!check1.ReqOut.Able(capsForward) || !check2.ReqIn.Able(capsForward))) {
                        continue;
                    }
                    if (capsReverse != null && (!check1.ReqIn.Able(capsReverse) || !check2.ReqOut.Able(capsReverse))) {
                        continue;
                    }
                    yield return edge.OtherNode(this);
                }
            }
        }

        public IEnumerable<Tuple<LinkedNode, Requirement>> SuccessorsRequires(Capabilities capsForward, bool onlyInternal = false) {
            foreach (var iedge in this.Static.Edges) {
                if (iedge.NodeTarget == null) {
                    continue;
                }
                var reqs = iedge.ReqOut.Conflicts(capsForward);
                if (reqs is Impossible) {
                    continue;
                }

                yield return Tuple.Create(this.Room.Nodes[iedge.NodeTarget.Name], reqs);
            }

            if (!onlyInternal) {
                foreach (var edge in this.Edges) {
                    var check1 = edge.CorrespondingEdge(this);
                    var check2 = edge.OtherEdge(this);

                    var reqs = Requirement.And(new List<Requirement> { check1.ReqOut.Conflicts(capsForward), check2.ReqIn.Conflicts(capsForward) });
                    if (reqs is Impossible) {
                        continue;
                    }
                    yield return Tuple.Create(edge.OtherNode(this), reqs);
                }
            }
        }

        public IEnumerable<StaticEdge> UnlinkedEdges(Capabilities capsForward, Capabilities capsReverse) {
            foreach (var staticedge in this.Static.Edges) {
                if (staticedge.HoleTarget == null) {
                    continue;
                }
                bool found = false;
                foreach (var linkededge in this.Edges) {
                    if (linkededge.CorrespondingEdge(this) == staticedge) {
                        found = true;
                        break;
                    }
                }
                if (found) {
                    continue;
                }
                if (capsForward != null && !staticedge.ReqOut.Able(capsForward)) {
                    continue;
                }
                if (capsReverse != null && !staticedge.ReqIn.Able(capsReverse)) {
                    continue;
                }
                yield return staticedge;
            }
        }

        public IEnumerable<StaticCollectable> UnlinkedCollectables() {
            foreach (var c in this.Static.Collectables) {
                if (!this.Collectables.ContainsKey(c)) {
                    yield return c;
                }
            }
        }
    }

    public class LinkedEdge {
        public StaticEdge StaticA, StaticB;
        public LinkedNode NodeA, NodeB;

        public LinkedNode OtherNode(LinkedNode One) {
            return One == NodeA ? NodeB : One == NodeB ? NodeA : null;
        }

        public StaticEdge CorrespondingEdge(LinkedNode One) {
            return One == NodeA ? StaticA : One == NodeB ? StaticB : null;
        }

        public StaticEdge OtherEdge(LinkedNode One) {
            return One == NodeA ? StaticB : One == NodeB ? StaticA : null;
        }
    }

    public class UnlinkedEdge {
        public StaticEdge Static;
        public LinkedNode Node;
    }

    public class UnlinkedCollectable {
        public StaticCollectable Static;
        public LinkedNode Node;
    }

    public class LinkedNodeSet {
        private List<LinkedNode> Nodes;
        private Capabilities CapsForward;
        private Capabilities CapsReverse;
        private Random Random;

        public static LinkedNodeSet Closure(LinkedNode start, Capabilities capsForward, Capabilities capsReverse, bool internalOnly) {
            var result = new HashSet<LinkedNode>();
            var queue = new Queue<LinkedNode>();
            void enqueue(LinkedNode node) {
                if (!result.Contains(node)) {
                    queue.Enqueue(node);
                    result.Add(node);
                }
            }
            enqueue(start);

            while (queue.Count != 0) {
                var item = queue.Dequeue();

                foreach (var succ in item.Successors(capsForward, capsReverse, internalOnly)) {
                    enqueue(succ);
                }
            }

            return new LinkedNodeSet(result) {
                CapsForward = capsForward,
                CapsReverse = capsReverse,
            };
        }

        public static Requirement TraversalRequires(LinkedNode start, Capabilities capsForward, bool internalOnly, UnlinkedEdge end) {
            return Requirement.And(new List<Requirement> { TraversalRequires(start, capsForward, internalOnly, end.Node),
                                                           end.Static.ReqOut.Conflicts(capsForward)
            });
        }

        public static Requirement TraversalRequires(LinkedNode start, Capabilities capsForward, bool internalOnly, LinkedNode end) {
            var queue = new PriorityQueue<Tuple<Requirement, LinkedNode>>();
            var seen = new Dictionary<LinkedNode, List<Requirement>>();

            Requirement p = new Possible();
            queue.Enqueue(Tuple.Create(p, start));
            seen[start] = new List<Requirement> { p };

            // implementation question: should this loop break as soon as one path to end is found, or should it exhaust the queue?
            // for now, let's go with exhaust the queue so if there's a Possible we don't miss it

            while (queue.Count != 0) {
                var entry = queue.Dequeue();
                var entryReq = entry.Item1;
                var entryNode = entry.Item2;

                foreach (var where in entryNode.SuccessorsRequires(capsForward)) {
                    var realReq = Requirement.And(new List<Requirement> { entryReq, where.Item2 });
                    var nextNode = where.Item1;

                    if (!seen.TryGetValue(nextNode, out var seenLst)) {
                        seenLst = new List<Requirement>();
                        seen[nextNode] = seenLst;
                    }

                    // search for any requirement already seen which obsoletes this new requirement
                    var found = false;
                    foreach (var req in seenLst) {
                        if (req.Equals(realReq) || req.StrictlyBetterThan(realReq)) {
                            found = true;
                            break;
                        }
                    }
                    if (!found) {
                        seenLst.Add(realReq);
                        queue.Enqueue(Tuple.Create(realReq, nextNode));
                    }
                }
            }

            if (!seen.TryGetValue(end, out var disjunct)) {
                return new Impossible();
            }

            return Requirement.Or(disjunct);
        }

        private LinkedNodeSet(List<LinkedNode> nodes) {
            this.Nodes = nodes;
        }

        private LinkedNodeSet(IEnumerable<LinkedNode> nodes) : this(new List<LinkedNode>(nodes)) { }

        public LinkedNodeSet Shuffle(Random random) {
            this.Random = random;
            return this;
        }

        public List<UnlinkedEdge> UnlinkedEdges(Func<UnlinkedEdge, bool> filter=null) {
            var result = new List<UnlinkedEdge>();
            foreach (var node in this.Nodes) {
                foreach (var edge in node.UnlinkedEdges(this.CapsForward, this.CapsReverse)) {
                    var uEdge = new UnlinkedEdge { Node = node, Static = edge };
                    if (filter != null && !filter(uEdge)) {
                        continue;
                    }
                    result.Add(uEdge);
                }
            }
            if (this.Random != null) {
                result.Shuffle(this.Random);
            }
            return result;
        }

        public List<UnlinkedCollectable> UnlinkedCollectables() {
            var result = new List<UnlinkedCollectable>();
            foreach (var node in this.Nodes) {
                foreach (var col in node.UnlinkedCollectables()) {
                    result.Add(new UnlinkedCollectable { Node = node, Static = col });
                }
            }
            if (this.Random != null) {
                result.Shuffle(this.Random);
            }
            return result;
        }
    }
}
