using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace PlayerSampleMission1CSharp
{
    struct FPath
    {
        public Point start;
        public Point end;
        public Point[] path;

        public FPath(Point start, Point end, Point[] path)
        {
            this.start = start;
            this.end = end;
            this.path = path;
        }
    }

    class AStar
    {
        //Version 2.0
        #region Members

        protected int X, Y;
        protected VG.Map.Tissue tissue;
        protected int[,] map;
        protected int bogStartX;
        protected int bogStartY;
        protected int bogWidth;

        protected bool explorer;

        protected int[,] exArray;
        protected int[] openList;
        protected int[] openX;
        protected int[] openY;
        protected int[,] parentX;
        protected int[,] parentY;
        protected int[] Fcost;
        protected int[,] Gcost;
        protected int[] Hcost;
        protected int onClosedList = 10;
        protected int onOpenList = 0;
        protected int parentXval = 0, parentYval = 0;
        protected int numberOfOpenListItems = 0, newOpenListItemID = 0;
        protected int m = 0;

        protected List<FPath> history;

        #endregion 

        public AStar(VG.Map.Tissue tissue)
            : this(tissue, new Point(0, 0), 0)
        {
        }
        public AStar(VG.Map.Tissue tissue, Point start, int width)
            : this(tissue, false, start, width)
        {
        }     
        protected AStar(VG.Map.Tissue tissue, bool expl, Point start, int width)
        {
            this.tissue = tissue;
            X = tissue.Width;
            Y = tissue.Height;
            exArray = new int[X + 1, Y + 1];
            openList = new int[X * Y + 2];
            openX = new int[X * Y + 2];
            openY = new int[X * Y + 2];
            parentX = new int[X + 1, Y + 1];
            parentY = new int[X + 1, Y + 1];
            Fcost = new int[X * Y + 2];
            Gcost = new int[X + 1, Y + 1];
            Hcost = new int[X * Y + 2];
            history = new List<FPath>();

            map = new int[X, Y];
            for (int i = 0; i < X; i++)
            {
                for (int j = 0; j < Y; j++)
                    map[i, j] = (int)tissue[i, j].AreaType;
            }
            this.bogStartX = start.X;
            this.bogStartY = start.Y;
            this.bogWidth = width;

            this.explorer = expl;
        }

        public Point[] FindPath(Point start, Point end)
        {
            int t = 0;
            return FindPath(start, end, ref t);
        }
        public Point[] FindPath(Point start, Point end, ref int Time)
        {
            int t = 1000000;
            return FindPath(start, end, ref Time, t);
        }
        public Point[] FindPath(Point start, Point end, ref int Time, int TimeLimit)
        {
            for (int i = 0; i < history.Count; i++)
            {
                if (start == history[i].start && end == history[i].end)
                {
                    return history[i].path;
                }
            }

            newOpenListItemID = 0;

            if (start == end)
            {
                Point[] ret = new Point[1];
                ret[0] = start;
                return ret;
            }

            if (onClosedList > 1000000)
            {
                for (int i = 0; i < X; i++)
                {
                    for (int j = 0; j < Y; j++)
                        exArray[i, j] = 0;
                }
                onClosedList = 10;
            }
            onClosedList += 2;
            onOpenList = onClosedList - 1;
            Gcost[start.X, start.Y] = 0;

            numberOfOpenListItems = 1;
            openList[1] = 1;
            openX[1] = start.X;
            openY[1] = start.Y;

            #region Main Loop
            bool can = false;
            bool ex = true;
            bool leaveBog = true;
            if ((end.X >= bogStartX && end.X <= bogStartX + bogWidth) && (end.Y >= bogStartY && end.Y < bogStartY + bogWidth))
                leaveBog = false;

            while (ex)
            {
                if (numberOfOpenListItems == 0)
                {
                    ex = false;
                    break;
                }
                parentXval = openX[openList[1]];
                parentYval = openY[openList[1]];
                exArray[parentXval, parentYval] = onClosedList;

                if (Gcost[parentXval, parentYval] > TimeLimit)
                    return null;


                numberOfOpenListItems -= 1;
                openList[1] = openList[numberOfOpenListItems + 1];
                int v = 1;
                int u = 0;
                bool ok = true;
                while (ok)
                {
                    u = v;
                    if (2 * u + 1 <= numberOfOpenListItems)
                    {
                        if (Fcost[openList[u]] >= Fcost[openList[2 * u]])
                            v = 2 * u;
                        if (Fcost[openList[v]] >= Fcost[openList[2 * u + 1]])
                            v = 2 * u + 1;
                    }
                    else
                    {
                        if (2 * u <= numberOfOpenListItems)
                        {
                            if (Fcost[openList[u]] >= Fcost[openList[2 * u]])
                                v = 2 * u;
                        }
                    }

                    if (u != v)
                    {
                        int temp = openList[u];
                        openList[u] = openList[v];
                        openList[v] = temp;
                    }
                    else
                        break;
                }

                int b = parentYval;
                int a = parentXval;
                nbPoint(a, b - 1, end, leaveBog);
                nbPoint(a + 1, b, end, leaveBog);
                nbPoint(a, b + 1, end, leaveBog);
                nbPoint(a - 1, b, end, leaveBog);

                if (exArray[end.X, end.Y] == onOpenList)
                {
                    ex = false;
                    can = true;
                }
            }
            #endregion

            #region Making path
            if (can)
            {
                Point[] ret_p;
                int count = 0;
                int t = 0;
                int px = end.X;
                int py = end.Y;
                Time = Gcost[end.X, end.Y];

                while (px != start.X || py != start.Y)
                {
                    t = parentX[px, py];
                    py = parentY[px, py];
                    px = t;
                    count++;
                }
                ret_p = new Point[count + 1];
                px = end.X;
                py = end.Y;
                int i = 0;
                while (px != start.X || py != start.Y)
                {
                    t = parentX[px, py];
                    py = parentY[px, py];
                    px = t;
                    ret_p[count - i - 1].X = px;
                    ret_p[count - i - 1].Y = py;
                    i++;
                }
                ret_p[count].X = end.X;
                ret_p[count].Y = end.Y;

                history.Add(new FPath(start, end, ret_p));
                return ret_p;
            }
            #endregion

            return new Point[0];
        }

        private void nbPoint(int x, int y, Point end, bool leave)
        {
            if (x < 0 || x >= X)
                return;
            if (y < 0 || y >= Y)
                return;
            if (exArray[x, y] == onClosedList)
                return;
            if (map[x, y] == 5)
                return;
            if (leave)
            {
                if ((x >= bogStartX && x <= bogStartX + bogWidth) && (y >= bogStartY && y < bogStartY + bogWidth))
                    return;
            }

            if (exArray[x, y] != onOpenList)
            {
                newOpenListItemID += 1;
                m = numberOfOpenListItems + 1;
                openList[m] = newOpenListItemID;
                openX[newOpenListItemID] = x;
                openY[newOpenListItemID] = y;

                Gcost[x, y] = calcG(x, y, parentXval, parentYval);
                Hcost[openList[m]] = calcH(x, y, end.X, end.Y);
                Fcost[openList[m]] = Gcost[x, y] + Hcost[openList[m]];
                parentX[x, y] = parentXval;
                parentY[x, y] = parentYval;

                while (m != 1)
                {
                    if (Fcost[openList[m]] <= Fcost[openList[m / 2]])
                    {
                        int temp = openList[m / 2];
                        openList[m / 2] = openList[m];
                        openList[m] = temp;
                        m = m / 2;
                    }
                    else
                        break;
                }
                numberOfOpenListItems = numberOfOpenListItems + 1;

                exArray[x, y] = onOpenList;
            }
            else if (exArray[x, y] == onOpenList)
            {
                int tempG = calcG(x, y, parentXval, parentYval);
                if (tempG < Gcost[x, y])
                {
                    parentX[x, y] = parentXval;
                    parentY[x, y] = parentYval;
                    Gcost[x, y] = tempG;

                    for (int i = 1; i <= numberOfOpenListItems; i++)
                    {
                        if (openX[openList[i]] == x && openY[openList[i]] == y)
                        {
                            Fcost[openList[i]] = Gcost[x, y] + Hcost[openList[i]];
                            m = i;
                            while (m != 1)
                            {
                                if (Fcost[openList[m]] < Fcost[openList[m / 2]])
                                {
                                    int temp = openList[m / 2];
                                    openList[m / 2] = openList[m];
                                    openList[m] = temp;
                                    m = m / 2;
                                }
                                else
                                    break;
                            }
                            break;
                        }
                    }
                }
            }
        }
        private int calcH(int x, int y, int endX, int endY)
        {
            return Math.Abs(x - endX) + Math.Abs(y - endY);
        }
        private int calcG(int x, int y, int pX, int pY)
        {
            int g = Gcost[pX, pY];

            if (explorer)
            {
                g += 1;
                return g;
            }

            if (map[x, y] == 0)
                g += 2;
            else if (map[x, y] == 1)
                g += 3;
            else if (map[x, y] == 2)
                g += 4;

            VG.Map.BloodStream bs = tissue.IsInStream(x, y);
            if (bs != null)
            {
                int dy = y - pY;
                int dx = x - pX;

                #region Directions
                bool plus = false;
                if (bs.Direction == VG.Map.BloodStreamDirection.NorthSouth)
                {
                    if (dy >= 0)
                        plus = false;
                    else
                        plus = true;
                }
                else if (bs.Direction == VG.Map.BloodStreamDirection.SouthNorth)
                {
                    if (dy <= 0)
                        plus = false;
                    else
                        plus = true;
                }
                else if (bs.Direction == VG.Map.BloodStreamDirection.EstWest)
                {
                    if (dx <= 0)
                        plus = false;
                    else
                        plus = true;
                }
                else if (bs.Direction == VG.Map.BloodStreamDirection.WestEst)
                {
                    if (dx >= 0)
                        plus = false;
                    else
                        plus = true;
                }

                if (tissue[x, y].AreaType == VG.Map.AreaEnum.LowDensity)
                {
                    if (plus)
                        g += 2;
                    else
                        g -= 1;
                }
                else
                {
                    if (plus)
                        g += 2;
                    else
                        g -= 2;
                }

                #endregion
            }
            return g;
        }
    }

    class eAstar : AStar
    {
        public eAstar(VG.Map.Tissue tissue, Point start, int width)
            : base(tissue, true, start, width)
        {
        }
        public eAstar(VG.Map.Tissue tissue)
            : this(tissue, new Point(0, 0), 0)
        {
        }
    }
}