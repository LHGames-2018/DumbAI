using System;
using System.Collections.Generic;
using System.Linq;
using LHGames.Helper;

namespace LHGames.Bot
{
    internal class Bot
    {
        internal IPlayer PlayerInfo { get; set; }
        private int _currentDirection = 1;
        List<InterestingObject> objects = new List<InterestingObject>();
        //InterestingObject currentObject = null;
        private int[] upgrades = new int[] { 10000, 15000, 25000, 50000, 100000 };

        internal Bot() { }

        /// <summary>
        /// Gets called before ExecuteTurn. This is where you get your bot's state.
        /// </summary>
        /// <param name="playerInfo">Your bot's current state.</param>
        internal void BeforeTurn(IPlayer playerInfo)
        {
            PlayerInfo = playerInfo;
        }

        /// <summary>
        /// Implement your bot here.
        /// </summary>
        /// <param name="map">The gamemap.</param>
        /// <param name="visiblePlayers">Players that are visible to your bot.</param>
        /// <returns>The action you wish to execute.</returns>
        internal string ExecuteTurn(Map map, IEnumerable<IPlayer> visiblePlayers)
        {
            getInterestingObjects(map, PlayerInfo.Position);
            objects.Sort((x, y) => x.priority.CompareTo(y.priority));
            PathFinder pathfinder;
            Point nextMove;
            if (PlayerInfo.Position == PlayerInfo.HouseLocation)
            {
                nextMove = new Point();
                if (PlayerInfo.GetUpgradeLevel(UpgradeType.CarryingCapacity) < 2)
                {
                    return AIHelper.CreateUpgradeAction(UpgradeType.CarryingCapacity);
                }
                if (PlayerInfo.GetUpgradeLevel(UpgradeType.AttackPower) < 2)
                {
                    return AIHelper.CreateUpgradeAction(UpgradeType.AttackPower);
                }
            }
            else if (PlayerInfo.CarriedResources == PlayerInfo.CarryingCapacity)
            {
                nextMove = new PathFinder(map, PlayerInfo.Position, PlayerInfo.HouseLocation).FindNextMove();
            }
            else
            {
                nextMove = new PathFinder(map, PlayerInfo.Position, objects[0].position).FindNextMove();
            }
            Point shouldIStayOrShouldIGoNow = PlayerInfo.Position + nextMove;

            TileContent content = map.GetTileAt(shouldIStayOrShouldIGoNow.X, shouldIStayOrShouldIGoNow.Y);
            switch (content)
            {
                case TileContent.Empty:
                case TileContent.House:
                    return AIHelper.CreateMoveAction(nextMove);
                case TileContent.Resource:
                    return AIHelper.CreateCollectAction(nextMove);
                case TileContent.Wall:
                    return AIHelper.CreateMeleeAttackAction(nextMove);
                case TileContent.Player:
                    return AIHelper.CreateMeleeAttackAction(nextMove);
                default:
                    return AIHelper.CreateEmptyAction();
            }
        }

        /// <summary>
        /// Gets called after ExecuteTurn.
        /// </summary>
        internal void AfterTurn()
        {
        }

        internal void getInterestingObjects(Map map, Point ownPosition)
        {
            List<InterestingObject> tmpList = new List<InterestingObject>();
            List<Tile> visibleTiles = new List<Tile>(map.GetVisibleTiles());
            for (int i = 0; i < visibleTiles.Count; i++)
            {
                Tile currentTile = visibleTiles[i];
                InterestingObject currentObject = null;
                switch ((int)currentTile.TileType)
                {
                    case 4:
                        currentObject = new InterestingObject() { distanceFromUser = new int[2] { currentTile.Position.X - PlayerInfo.Position.X, currentTile.Position.Y - PlayerInfo.Position.Y }, type = currentTile.TileType, position = currentTile.Position };
                        currentObject.priority = Math.Abs(currentObject.distanceFromUser[0]) + Math.Abs(currentObject.distanceFromUser[1]);
                        break;

                    case 6:
                        currentObject = new InterestingObject() { distanceFromUser = new int[2] { currentTile.Position.X - PlayerInfo.Position.X, currentTile.Position.Y - PlayerInfo.Position.Y }, type = currentTile.TileType, position = currentTile.Position };
                        currentObject.priority = Math.Abs(currentObject.distanceFromUser[0]) + Math.Abs(currentObject.distanceFromUser[1]) - 5; //+5 to make the enemies always prioritary over resources
                        break;

                    default:
                        break;

                }
                if (currentObject != null && currentObject.position != ownPosition)
                    tmpList.Add(currentObject);
                objects = tmpList;
            }
        }
    }
}



class TestClass
{
    public string Test { get; set; }
}

internal class PathFinder
{
    readonly Node[,] nodes;
    private Point overflow;
    readonly Node startNode;
    readonly Node endNode;

    public Point FindNextMove()
    {
        if (overflow != null)
        {
            return overflow;
        }
        List<Point> path = FindPath();
        return path.Count > 0 ? path[0] - startNode.Location : new Point(0, 0);
    }

    public PathFinder(Map map, Point start, Point end)
    {
        int mapSize = map.VisibleDistance * 2 + 1;
        int deltaX = start.X - map.VisibleDistance;
        int deltaY = start.Y - map.VisibleDistance;
        nodes = new Node[mapSize, mapSize];
        for (int i = 0; i < mapSize; i++)
        {
            for (int j = 0; j < mapSize; j++)
            {
                TileContent tile = map.GetTileAt(i + deltaX, j + deltaY);
                nodes[i, j] = new Node(tile == TileContent.Empty || tile == TileContent.Wall || tile == TileContent.House,
                   new Point(i, j),
                   Math.Abs(i + deltaX - end.X) + Math.Abs(j + deltaY - end.Y), tile == TileContent.Wall ? 1 : 1);
            }
        }

        startNode = nodes[start.X - deltaX, start.Y - deltaY];
        int x, y;
        x = end.X - deltaX;
        y = end.Y - deltaY;
        overflow = x < 0 ? new Point(-1, 0) : (x >= mapSize ? new Point(1, 0) : (y < 0 ? new Point(0, -1) : (y >= mapSize ? new Point(0, 1) : null)));
        if (overflow == null)
        {
            endNode = nodes[end.X - deltaX, end.Y - deltaY];
        }
    }

    List<Point> FindPath()
    {
        List<Point> path = new List<Point>();
        bool success = Search(startNode);
        if (success)
        {
            Node node = this.endNode;
            while (node.ParentNode != null)
            {
                path.Add(node.Location);
                node = node.ParentNode;
            }
            path.Reverse();
        }
        return path;
    }

    bool Search(Node currentNode)
    {
        currentNode.State = NodeState.Closed;
        List<Node> nextNodes = GetAdjacentWalkableNodes(currentNode);
        nextNodes.Sort((node1, node2) => node1.F.CompareTo(node2.F));
        foreach (var nextNode in nextNodes)
        {
            if (nextNode.Location == this.endNode.Location)
            {
                return true;
            }
            else
            {
                if (Search(nextNode))
                    return true;
            }
        }
        return false;
    }

    List<Node> GetAdjacentWalkableNodes(Node fromNode)
    {
        List<Node> walkableNodes = new List<Node>();
        IEnumerable<Point> nextLocations = GetAdjacentLocations(fromNode.Location);

        foreach (var location in nextLocations)
        {
            int x = location.X;
            int y = location.Y;

            // Stay within the grid's boundaries
            if (x < 0 || x >= nodes.GetLength(0) || y < 0 || y >= nodes.GetLength(1))
                continue;

            Node node = this.nodes[x, y];
            // Ignore non-walkable nodes
            if (!node.IsWalkable && node != endNode)
                continue;

            // Ignore already-closed nodes
            if (node.State == NodeState.Closed)
                continue;

            // Already-open nodes are only added to the list if their G-value is lower going via this route.
            if (node.State == NodeState.Open)
            {
                float gTemp = fromNode.G + node.TileValue;
                if (gTemp < node.G)
                {
                    node.ParentNode = fromNode;
                    walkableNodes.Add(node);
                }
            }
            else
            {
                // If it's untested, set the parent and flag it as 'Open' for consideration
                node.ParentNode = fromNode;
                node.State = NodeState.Open;
                walkableNodes.Add(node);
            }
        }

        return walkableNodes;
    }

    List<Point> GetAdjacentLocations(Point pt)
    {
        List<Point> adj = new List<Point>();
        adj.Add(new Point(pt.X - 1, pt.Y));
        adj.Add(new Point(pt.X + 1, pt.Y));
        adj.Add(new Point(pt.X, pt.Y + 1));
        adj.Add(new Point(pt.X, pt.Y - 1));
        return adj;
    }

    public class Node
    {
        public Point Location { get; private set; }
        public bool IsWalkable { get; set; }
        public int TileValue { get; set; }

        public float G
        {
            get { return ParentNode == null ? 0 : ParentNode.G + TileValue; }
        }
        public float H { get; private set; }
        public float F { get { return this.G + this.H; } }
        public NodeState State { get; set; }
        public Node ParentNode { get; set; }

        public Node(bool isWalkable, Point location, float h, int tileValue)
        {
            IsWalkable = isWalkable;
            Location = location;
            H = h;
            State = NodeState.Untested;
            TileValue = tileValue;
        }
    }

    public enum NodeState { Untested, Open, Closed }
}

class InterestingObject
{
    public int[] distanceFromUser;
    public TileContent type;
    public int priority;
    public Point position;
}