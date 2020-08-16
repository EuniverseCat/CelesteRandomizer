using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Celeste;
using Priority_Queue;


//Adapted from the pseudocode on wikipedia
namespace Bruter {
	public partial class AStar {
		public class Node {
			public float gScore = float.MaxValue;
			public float fScore = float.MaxValue;
			public Node parent;
			public int x;
			public int y;
		}

		public LevelData level;
		public Node[][] graph;

		public static string Do(LevelData level) {
			return null;
		}

		AStar(LevelData level) {
		}



		// A* finds a path from start to goal.
		// h is the heuristic function. h(n) estimates the cost to reach goal from node n.
		List<Node> A_Star(Node start, Node goal, Func<Node, float> h) {
			// The set of discovered nodes that may need to be (re-)expanded.
			// Initially, only the start node is known.
			// This is usually implemented as a min-heap or priority queue rather than a hash-set.
			SimplePriorityQueue <Node> openSet = new SimplePriorityQueue<Node>();

			// For node n, gScore[n] is the cost of the cheapest path from start to n currently known.
			start.gScore = 0;

			// For node n, fScore[n] := gScore[n] + h(n). fScore[n] represents our current best guess as to
			// how short a path from start to finish can be if it goes through n.
			start.fScore = h(start);

			start.parent = start;

			while (!(openSet.Count == 0)) {
				// This operation can occur in O(1) time if openSet is a min-heap or a priority queue
				Node current = openSet.Dequeue();

				if (current == goal)
					return reconstruct_path(current);


				foreach (Node neighbor in GetNeighbors(current)) {
					// d(current,neighbor) is the weight of the edge from current to neighbor
					// tentative_gScore is the distance from start to the neighbor through current
					float tentative_gScore = current.gScore + d(current, neighbor);

					if (tentative_gScore < neighbor.gScore) {
						// This path to neighbor is better than any previous one. Record it!
						neighbor.parent = current;
						neighbor.gScore = tentative_gScore;

						neighbor.fScore = neighbor.gScore + h(neighbor);
						if (!openSet.Contains(neighbor)) {
							openSet.Enqueue(neighbor, neighbor.fScore);
						}
					}
				}
			}
			return null;
		}

		List<Node> reconstruct_path(Node current) {
			List<Node> path = new List<Node>();
			path.Add(current);
			while (current.parent != current) {
				current = current.parent;
				path.Add(current);
			}
			return path;
		}

		List<Node> AStar_PostSmoothing(Node start, Node goal, Func<Node, float> h) {
			return A_Star(start, goal, h);

		}

		float d(Node from, Node to) {
			if (from.x == to.x || from.y == to.y)
				return 1;
			return 1.41421f;
		}

		public List<Node> GetNeighbors(Node node) {
			List<Node> nodes = new List<Node>();

			if (node.x != 0 && node.y != 0)
				nodes.Add(graph[node.x - 1][node.y - 1]);
			if (node.x < graph.Length && node.y != 0)
				nodes.Add(graph[node.x + 1][node.y - 1]);
			if (node.x != 0 && node.y < graph[0].Length)
				nodes.Add(graph[node.x - 1][node.y + 1]);
			if (node.x < graph.Length && node.y < graph[0].Length)
				nodes.Add(graph[node.x + 1][node.y + 1]);

			return nodes;
		}
	}
}
