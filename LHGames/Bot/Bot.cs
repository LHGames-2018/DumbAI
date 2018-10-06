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
            Point nextMove;
            if (PlayerInfo.Position == PlayerInfo.HouseLocation)
            {
                nextMove = new Point();
                if (PlayerInfo.GetUpgradeLevel(UpgradeType.CarryingCapacity) == 0 && PlayerInfo.TotalResources >= upgrades[0])
                {
                    return AIHelper.CreateUpgradeAction(UpgradeType.CarryingCapacity);
                }
                if (PlayerInfo.GetUpgradeLevel(UpgradeType.AttackPower) == 0 && PlayerInfo.TotalResources >= upgrades[0])
                {
                    return AIHelper.CreateUpgradeAction(UpgradeType.AttackPower);
                }
                if (PlayerInfo.GetUpgradeLevel(UpgradeType.CarryingCapacity) == 1 && PlayerInfo.TotalResources >= upgrades[1])
                {
                    return AIHelper.CreateUpgradeAction(UpgradeType.CarryingCapacity);
                }
                if (PlayerInfo.GetUpgradeLevel(UpgradeType.CollectingSpeed) == 0 && PlayerInfo.TotalResources >= upgrades[0])
                {
                    return AIHelper.CreateUpgradeAction(UpgradeType.CollectingSpeed);
                }
            }

         if (PlayerInfo.CarriedResources == PlayerInfo.CarryingCapacity)
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
                  currentObject.priority = Math.Abs(currentObject.distanceFromUser[0]) + Math.Abs(currentObject.distanceFromUser[1]) - 2; //+5 to make the enemies always prioritary over resources
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
               Math.Abs(i + deltaX - end.X) + Math.Abs(j + deltaY - end.Y), tile == TileContent.Wall ? 3 : 1);
         }
      }

      startNode = nodes[start.X - deltaX, start.Y - deltaY];
      int x, y;
      x = end.X - deltaX;
      y = end.Y - deltaY;
      overflow = x < 0 ? new Point(-1, 0) : (x >= mapSize ? new Point(1, 0) : (y < 0 ? new Point(0, -1) : (y >= mapSize ? new Point(0, 1) : null)));
      var tileov = map.GetTileAt(start.X + overflow.X, start.Y + overflow.Y);
      if (tileov == TileContent.Resource || tileov == TileContent.Lava || tileov == TileContent.Shop)
      {
         overflow = new Point(1 - Math.Abs(overflow.X), 1 - Math.Abs(overflow.Y));
      }
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
      List<Node> openNodes = new List<Node>();
      List<Node> closedNodes = new List<Node>();
      openNodes.Add(currentNode);

      while (openNodes.Count > 0)
      {
         openNodes.Sort((x, y) => x.F.CompareTo(y.F));
         Node parent = openNodes.First();

         List<Node> successors = GetAdjacentNodes(parent).ToList();

         foreach (Node suc in successors)
         {
            if (suc == endNode)
            {
               suc.ParentNode = parent;
               return true;
            }

            if (suc.IsWalkable)
            {
               if (openNodes.Contains(suc) && suc.G > parent.G + suc.TileValue)
               {
                  suc.ParentNode = parent;
               }
               else if (closedNodes.Contains(suc))
               {
                  if (suc.G > parent.G + suc.TileValue)
                  {
                     suc.ParentNode = parent;
                     openNodes.Add(suc);
                     closedNodes.Remove(suc);
                  }
               }
               else
               {
                  suc.ParentNode = parent;
                  openNodes.Add(suc);
               }
            }

         }
         openNodes.Remove(parent);
         closedNodes.Add(parent);
      }

      return false;
   }

   IEnumerable<Node> GetAdjacentNodes(Node fromNode)
   {
      IEnumerable<Point> nextLocations = GetAdjacentLocations(fromNode.Location);
      return nextLocations.Where(x => x.X >= 0 && x.X < nodes.GetLength(0) && x.Y >= 0 && x.Y < nodes.GetLength(1)).Select(x => this.nodes[x.X, x.Y]);
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
      public Node ParentNode { get; set; }

      public Node(bool isWalkable, Point location, float h, int tileValue)
      {
         IsWalkable = isWalkable;
         Location = location;
         H = h;
         TileValue = tileValue;
      }
   }
}

class InterestingObject
{
   public int[] distanceFromUser;
   public TileContent type;
   public int priority;
   public Point position;
}