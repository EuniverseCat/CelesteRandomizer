using System;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.Randomizer {
    public partial class RandoLogic {
        public abstract class Receipt {
            public abstract void Undo();
        }

        public class StartRoomReceipt : Receipt {
            private RandoLogic Logic;
            public LinkedRoom NewRoom;

            public static StartRoomReceipt Do(RandoLogic logic, StaticRoom newRoomStatic) {
                var newRoom = new LinkedRoom(newRoomStatic, Vector2.Zero);

                logic.Map.AddRoom(newRoom);

                if (!logic.Settings.RepeatRooms) {
                    logic.RemainingRooms.Remove(newRoomStatic);
                }

                return new StartRoomReceipt {
                    Logic = logic,
                    NewRoom = newRoom,
                };
            }

            public override void Undo() {
                Logic.Map.RemoveRoom(NewRoom);

                if (!this.Logic.Settings.RepeatRooms) {
                    this.Logic.RemainingRooms.Add(this.NewRoom.Static);
                }
            }
        }

        public class ConnectAndMapReceipt : Receipt {
            public LinkedRoom NewRoom;
            private RandoLogic Logic;
            public LinkedEdge Edge;
            public LinkedNode EntryNode;

            public static ConnectAndMapReceipt Do(RandoLogic logic, UnlinkedEdge fromEdge, StaticEdge toEdge) {
                var toRoomStatic = toEdge.FromNode.ParentRoom;
                var fromRoom = fromEdge.Node.Room;

                if (fromEdge.Static.HoleTarget == null || toEdge.HoleTarget == null) {
                    return null;
                }

                var newOffset = fromEdge.Static.HoleTarget.Compatible(toEdge.HoleTarget);
                if (newOffset == Hole.INCOMPATIBLE) {
                    return null;
                }

                var newPosition = toRoomStatic.AdjacentPosition(fromRoom.Bounds, fromEdge.Static.HoleTarget.Side, newOffset);
                var toRoom = new LinkedRoom(toRoomStatic, newPosition);
                if (!logic.Map.AreaFree(toRoom)) {
                    return null;
                }

                logic.Map.AddRoom(toRoom);
                var newEdge = new LinkedEdge {
                    NodeA = fromEdge.Node,
                    NodeB = toRoom.Nodes[toEdge.FromNode.Name],
                    StaticA = fromEdge.Static,
                    StaticB = toEdge,
                };
                newEdge.NodeA.Edges.Add(newEdge);
                newEdge.NodeB.Edges.Add(newEdge);

                if (!logic.Settings.RepeatRooms) {
                    logic.RemainingRooms.Remove(toRoomStatic);
                }
				
                return new ConnectAndMapReceipt {
                    NewRoom = toRoom,
                    Logic = logic,
                    Edge = newEdge,
                    EntryNode = toRoom.Nodes[toEdge.FromNode.Name],
                };
            }

            public override void Undo() {
                this.Logic.Map.RemoveRoom(this.NewRoom);
                this.Edge.NodeA.Edges.Remove(this.Edge);
                this.Edge.NodeB.Edges.Remove(this.Edge);

                if (!this.Logic.Settings.RepeatRooms) {
                    this.Logic.RemainingRooms.Add(this.NewRoom.Static);
                }
            }
        }

        public class PlaceCollectableReceipt : Receipt {
            private LinkedNode Node;
            private StaticCollectable Place;

            public static PlaceCollectableReceipt Do(LinkedNode node, StaticCollectable place, LinkedNode.LinkedCollectable item) {
                node.Collectables[place] = item;
                return new PlaceCollectableReceipt {
                    Node = node,
                    Place = place
                };
            }

            public override void Undo() {
                this.Node.Collectables.Remove(this.Place);
            }
        }
    }
}
