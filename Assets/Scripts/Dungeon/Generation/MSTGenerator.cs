using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MSTGenerator
{
    public List<MSTEdge> GenerateMST(List<MSTEdge> delaunayEdges, List<Room> rooms)
    {
        return PrimAlgorithmFromDelaunay(delaunayEdges, rooms);
    }

    public int AddCyclesToMST(List<MSTEdge> mstEdges, List<MSTEdge> delaunayEdges, 
        float loopQualityBias, float randomnessFactor, float minGraphDistanceThreshold, int extraCycleEdges)
    {
        if (delaunayEdges == null || delaunayEdges.Count == 0)
        {
            return 0;
        }

        HashSet<(Vector2Int, Vector2Int)> existingEdges =
            new HashSet<(Vector2Int, Vector2Int)>(mstEdges.Select(edge => NormalizeEdgeTuple(edge.p1, edge.p2)));
        Dictionary<Vector2Int, List<(Vector2Int, float)>> currentGraph = BuildEdgeGraph(mstEdges);

        int added = 0;
        int cycleCount = extraCycleEdges;
        
        while (added < cycleCount)
        {
            var candidateEdges = delaunayEdges
                .Where(edge => !existingEdges.Contains(NormalizeEdgeTuple(edge.p1, edge.p2)))
                .Select(edge => new { Edge = edge, Score = EvaluateLoopEdge(edge, currentGraph, loopQualityBias, randomnessFactor, minGraphDistanceThreshold) })
                .Where(candidate => candidate.Score > 0f)
                .OrderByDescending(candidate => candidate.Score)
                .ToList();

            if (candidateEdges.Count == 0)
            {
                break;
            }

            MSTEdge best = candidateEdges[0].Edge;
            mstEdges.Add(best);
            existingEdges.Add(NormalizeEdgeTuple(best.p1, best.p2));
            AddEdgeToGraph(currentGraph, best);
            added++;
        }

        return added;
    }

    public void ControlDeadEnds(List<MSTEdge> mstEdges, List<MSTEdge> delaunayEdges, 
        float deadEndKeepChance, float deadEndConnectChance)
    {
        Dictionary<Vector2Int, int> nodeDegree = new Dictionary<Vector2Int, int>();

        foreach (var edge in mstEdges)
        {
            if (!nodeDegree.ContainsKey(edge.p1)) nodeDegree[edge.p1] = 0;
            if (!nodeDegree.ContainsKey(edge.p2)) nodeDegree[edge.p2] = 0;

            nodeDegree[edge.p1]++;
            nodeDegree[edge.p2]++;
        }

        foreach (var node in nodeDegree)
        {
            if (node.Value == 1)
            {
                float roll = Random.value;

                if (roll < deadEndKeepChance)
                    continue;

                if (roll < deadEndKeepChance + deadEndConnectChance)
                {
                    var candidates = delaunayEdges
                        .Where(e => e.p1 == node.Key || e.p2 == node.Key)
                        .Where(e => !mstEdges.Any(me =>
                            (me.p1 == e.p1 && me.p2 == e.p2) ||
                            (me.p1 == e.p2 && me.p2 == e.p1)))
                        .OrderBy(e => e.distance)
                        .ToList();

                    if (candidates.Count > 0)
                    {
                        mstEdges.Add(candidates[0]);
                    }
                }
            }
        }
    }

    private List<MSTEdge> PrimAlgorithmFromDelaunay(List<MSTEdge> edges, List<Room> roomList)
    {
        List<MSTEdge> mst = new List<MSTEdge>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        List<Vector2Int> points = roomList.Select(room => room.center).ToList();

        if (points.Count == 0)
        {
            return mst;
        }

        visited.Add(points[0]);

        while (visited.Count < points.Count)
        {
            float minDistance = float.MaxValue;
            MSTEdge bestEdge = default;
            bool foundEdge = false;

            foreach (MSTEdge edge in edges)
            {
                bool p1Visited = visited.Contains(edge.p1);
                bool p2Visited = visited.Contains(edge.p2);

                if ((p1Visited && !p2Visited) || (!p1Visited && p2Visited))
                {
                    if (edge.distance < minDistance)
                    {
                        minDistance = edge.distance;
                        bestEdge = edge;
                        foundEdge = true;
                    }
                }
            }

            if (!foundEdge)
            {
                break;
            }

            mst.Add(bestEdge);
            visited.Add(visited.Contains(bestEdge.p1) ? bestEdge.p2 : bestEdge.p1);
        }

        return mst;
    }

    private float EvaluateLoopEdge(MSTEdge edge, Dictionary<Vector2Int, List<(Vector2Int, float)>> graph,
        float loopQualityBias, float randomnessFactor, float minGraphDistanceThreshold)
    {
        float pathDistance = GetPathDistance(graph, edge.p1, edge.p2);
        if (pathDistance == float.MaxValue || edge.distance <= 0.001f)
        {
            return 0f;
        }

        float graphRatio = pathDistance / edge.distance;
        if (graphRatio < minGraphDistanceThreshold)
        {
            return 0f;
        }

        float improvement = pathDistance - edge.distance;
        float distanceFactor = improvement > 0f ? graphRatio : 0.5f;
        float euclideanFactor = Mathf.Log(1f + edge.distance);
        float combinedScore = (distanceFactor * loopQualityBias) + (euclideanFactor * (1f - loopQualityBias));
        float randomMultiplier = Random.Range(1f - randomnessFactor, 1f + randomnessFactor);

        return combinedScore * randomMultiplier;
    }

    private Dictionary<Vector2Int, List<(Vector2Int, float)>> BuildEdgeGraph(IEnumerable<MSTEdge> edges)
    {
        Dictionary<Vector2Int, List<(Vector2Int, float)>> graph = new Dictionary<Vector2Int, List<(Vector2Int, float)>>();

        foreach (MSTEdge edge in edges)
        {
            if (!graph.ContainsKey(edge.p1))
            {
                graph[edge.p1] = new List<(Vector2Int, float)>();
            }

            if (!graph.ContainsKey(edge.p2))
            {
                graph[edge.p2] = new List<(Vector2Int, float)>();
            }

            graph[edge.p1].Add((edge.p2, edge.distance));
            graph[edge.p2].Add((edge.p1, edge.distance));
        }

        return graph;
    }

    private void AddEdgeToGraph(Dictionary<Vector2Int, List<(Vector2Int, float)>> graph, MSTEdge edge)
    {
        if (!graph.ContainsKey(edge.p1))
        {
            graph[edge.p1] = new List<(Vector2Int, float)>();
        }

        if (!graph.ContainsKey(edge.p2))
        {
            graph[edge.p2] = new List<(Vector2Int, float)>();
        }

        graph[edge.p1].Add((edge.p2, edge.distance));
        graph[edge.p2].Add((edge.p1, edge.distance));
    }

    private float GetPathDistance(Dictionary<Vector2Int, List<(Vector2Int, float)>> graph, Vector2Int start, Vector2Int target)
    {
        if (!graph.ContainsKey(start) || !graph.ContainsKey(target))
        {
            return float.MaxValue;
        }

        Dictionary<Vector2Int, float> distances = graph.Keys.ToDictionary(node => node, _ => float.MaxValue);
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        distances[start] = 0f;

        while (visited.Count < graph.Count)
        {
            Vector2Int current = default;
            float currentDist = float.MaxValue;

            foreach (Vector2Int node in graph.Keys)
            {
                if (visited.Contains(node))
                {
                    continue;
                }

                if (distances[node] < currentDist)
                {
                    currentDist = distances[node];
                    current = node;
                }
            }

            if (currentDist == float.MaxValue)
            {
                break;
            }

            if (current == target)
            {
                return currentDist;
            }

            visited.Add(current);

            foreach ((Vector2Int, float) connection in graph[current])
            {
                Vector2Int neighbor = connection.Item1;
                float weight = connection.Item2;

                if (visited.Contains(neighbor))
                {
                    continue;
                }

                float nextDist = currentDist + weight;
                if (nextDist < distances[neighbor])
                {
                    distances[neighbor] = nextDist;
                }
            }
        }

        return distances[target];
    }

    private (Vector2Int, Vector2Int) NormalizeEdgeTuple(Vector2Int a, Vector2Int b)
    {
        if (a.x > b.x || (a.x == b.x && a.y > b.y))
        {
            return (b, a);
        }

        return (a, b);
    }
}
