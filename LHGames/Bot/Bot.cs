using System;
using System.Collections.Generic;
using LHGames.Helper;

namespace LHGames.Bot
{
    internal class Bot
    {
        internal IPlayer PlayerInfo { get; set; }

        internal Bot() { }

        Point PointToGo { get; set; }

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
            if (PlayerInfo.CarriedResources >= 500)
            {
                PointToGo = PlayerInfo.HouseLocation;
            }
            //else if (PlayerInfo.TotalResources >= 10000 && PlayerInfo.Position == PlayerInfo.HouseLocation)
            //{
            //    return AIHelper.CreateUpgradeAction(UpgradeType.CollectingSpeed);
            //}
            else if (PointToGo == null || map.GetTileAt(PointToGo.X, PointToGo.Y) != TileContent.Resource)
            {
                Point closerRessource = null;
                foreach (Tile tile in map.GetVisibleTiles())
                {
                    if (tile.TileType == TileContent.Resource)
                    {
                        if (closerRessource == null)
                        {
                            closerRessource = tile.Position;
                        }
                        else
                        {
                            if (Point.DistanceSquared(closerRessource, PlayerInfo.Position) > Point.DistanceSquared(tile.Position, PlayerInfo.Position))
                            {
                                closerRessource = tile.Position;
                            }
                        }
                    }
                }
                PointToGo = closerRessource;
            }

            StorageHelper.Write("PointToGo", PointToGo);
            Point deplacement = PointToGo - PlayerInfo.Position;
            Point mouvement;
            bool maxX = Math.Abs(deplacement.X) >= Math.Abs(deplacement.Y);

            if (maxX)
            {
                if (Math.Abs(deplacement.X) > 1 || Math.Abs(deplacement.X) == Math.Abs(deplacement.Y))
                {
                    mouvement = new Point(deplacement.X / Math.Abs(deplacement.X), 0);
                }
                else
                {
                    if (PlayerInfo.HouseLocation == PointToGo)
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
                    if (PlayerInfo.HouseLocation == PointToGo)
                    {
                        mouvement = new Point(0, deplacement.Y / Math.Abs(deplacement.Y));
                    }
                    else
                    {
                        return AIHelper.CreateCollectAction(deplacement);
                    }
                }
            }

            TestClass data = StorageHelper.Read<TestClass>("Test");
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