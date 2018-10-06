using System;
using System.Collections.Generic;
using LHGames.Helper;

namespace LHGames.Bot
{
    internal class Bot
    {
        internal IPlayer PlayerInfo { get; set; }
        private int _currentDirection = 1;

        internal Bot() { }

        /// <summary>
        /// Gets called before ExecuteTurn. This is where you get your bot's state.
        /// </summary>
        /// <param name="playerInfo">Your bot's current state.</param>
        internal void BeforeTurn(IPlayer playerInfo)
        {
            PlayerInfo = playerInfo;
            Console.WriteLine(playerInfo);
        }

        /// <summary>
        /// Implement your bot here.
        /// </summary>
        /// <param name="map">The gamemap.</param>
        /// <param name="visiblePlayers">Players that are visible to your bot.</param>
        /// <returns>The action you wish to execute.</returns>
        internal string ExecuteTurn(Map map, IEnumerable<IPlayer> visiblePlayers)
        {
            Point pointToGo;
            if (PlayerInfo.CarriedResources == PlayerInfo.CarryingCapacity)
            {
                pointToGo = PlayerInfo.HouseLocation;
            }
            else
            {
                pointToGo = StorageHelper.Read<Point>("pointToGo");
            }
            if (pointToGo == null || map.GetTileAt(pointToGo.X, pointToGo.Y) != TileContent.Resource)
            {
                foreach (Tile tile in map.GetVisibleTiles())
                {
                    if (tile.TileType == TileContent.Resource)
                    {
                        pointToGo = tile.Position;
                        break;
                    }
                }
            }
            StorageHelper.Write("pointToGo", pointToGo);
            Point deplacement = pointToGo - PlayerInfo.Position;
            Point mouvement;
            bool maxX = deplacement.X >= deplacement.Y;

            if (maxX)
            {
                if(Math.Abs(deplacement.X) > 1 || deplacement.X == deplacement.Y)
                {
                    mouvement = new Point(deplacement.X / Math.Abs(deplacement.X), 0);
                }
                else
                {
                    if(PlayerInfo.HouseLocation == pointToGo)
                    {
                        mouvement = new Point(deplacement.X / Math.Abs(deplacement.X));
                    }
                    else
                    {
                        return AIHelper.CreateCollectAction(deplacement);
                    }
                }
            }
            else
            {
                if (Math.Abs(deplacement.Y) > 1)
                {
                    mouvement = new Point(0, deplacement.Y / Math.Abs(deplacement.Y));
                }
                else
                {
                    if (PlayerInfo.HouseLocation == pointToGo)
                    {
                        mouvement = new Point(0, deplacement.Y / Math.Abs(deplacement.Y));
                    }
                    else
                    {
                        return AIHelper.CreateCollectAction(deplacement);
                    }
                }
            }

            var data = StorageHelper.Read<TestClass>("Test");
            Console.WriteLine(data?.Test);
            return AIHelper.CreateMoveAction(mouvement);
        }

        /// <summary>
        /// Gets called after ExecuteTurn.
        /// </summary>
        internal void AfterTurn()
        {
        }
    }
}

class TestClass
{
    public string Test { get; set; }
}