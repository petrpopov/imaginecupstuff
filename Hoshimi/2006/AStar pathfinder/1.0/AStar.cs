using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.IO;

namespace PathFinder
{
    class Node
    {
        private Point point;
        private int f;
        private int g;
        private int h;
        private Point parent;

        public Node(Point p, int f, int g, int h)
        {
            this.point = p;
            this.f = 0;
            this.g = 0;
            this.h = 0;
            this.parent = this.point;
        }

        #region Properties
        public Point P
        {
            get
            {
                return point;
            }
            set
            {
                point = value;
            }
        }

        public Point Parent
        {
            get
            {
                return parent;
            }
            set
            {
                parent = value;
            }
        }

        public int F
        {
            get
            {
                return f;
            }
            set
            {
                f = value;
            }
        }
        public int G
        {
            get
            {
                return g;
            }
            set
            {
                g = value;
            }
        }
        public int H
        {
            get
            {
                return h;
            }
            set
            {
                h = value;
            }
        }
        #endregion
    }

    class AStar
    {
        private List<Node> openList;
        private List<Node> closeList;
        private Node current;
        private int x;
        private int y;
        private VG.Map.Tissue tissue;
        int[,] existsArray;

        public AStar(VG.Map.Tissue tissue)
        {
            openList = new List<Node>();
            closeList = new List<Node>();

            this.tissue = tissue;
            this.x = tissue.Width;
            this.y = tissue.Width;
            this.existsArray = new int[this.x, this.y];
        }

        public Point[] findPath(Point start, Point end)
        {
            List<Point> p = new List<Point>();
            current = new Node(start, 0, 0, 0);
            current.H = calcH(start, end);
            current.F = current.H;
            openList.Add(current);
            existsArray[current.P.X, current.P.Y] = 1;

            bool ex = false;
            bool can = true;
            while (!ex)
            {
                int i = findMinF(openList);
                current = openList[i];
                openList.RemoveAt(i);
                closeList.Add(current);
                existsArray[current.P.X, current.P.Y] = 2;

                Point nb = current.P;
                nbPoint(new Point(nb.X + 1, nb.Y), current, end, x, y);
                nbPoint(new Point(nb.X, nb.Y - 1), current, end, x, y);
                nbPoint(new Point(nb.X - 1, nb.Y), current, end, x, y);
                nbPoint(new Point(nb.X, nb.Y + 1), current, end, x, y);

                if (openList.Count == 0)
                {
                    ex = true;
                    can = false;
                }
                //if (ifExistsInList(end, openList))
                if (existsArray[end.X, end.Y] == 1)
                    ex = true;
            }

            if (can)
            {
                List<Node> list = new List<Node>();
                foreach (Node n in openList)
                    list.Add(n);
                foreach (Node n in closeList)
                    list.Add(n);

                p.Add(end);
                Point pp = end;
                int i = 0;

                while (pp != start)
                {
                    i = inList(pp, list);
                    if (i < 0)
                        break;
                    pp = list[i].Parent;
                    p.Add(pp);
                    list.RemoveAt(i);
                }
            }

            Point[] ret = new Point[p.Count];
            for (int i = 0; i < p.Count; i++)
                ret[i] = p[p.Count - i - 1];

            return ret;
        }

        private void nbPoint(Point nb, Node current, Point end, int X, int Y)
        {
            if (nb.X >= X || nb.X < 0)
                return;
            if (nb.Y >= Y || nb.Y < 0)
                return;
           // if (ifExistsInList(nb, closeList))
            if (existsArray[nb.X, nb.Y] == 2)
                return;
            if (!tissue.IsInMap(nb.X, nb.Y))
                return;
            if( tissue[nb.X,nb.Y].AreaType == VG.Map.AreaEnum.Bone )
                return;

            //if (!ifExistsInList(nb, openList))
            if(existsArray[nb.X,nb.Y] !=1 )
            {
                Node n = new Node(nb, 0, 0, 0);
                n.H = calcH(nb, end);
                n.G = calcG(n);

                n.F = n.G + n.H;
                n.Parent = current.P;
                openList.Add(n);
                existsArray[n.P.X, n.P.Y] = 1;
            }
            else
            {
                Node n = openList[inList(nb, openList)];
                if (n.G < calcG(n))
                {
                    n.Parent = current.P;
                    n.G = calcG(n);
                    n.F = n.G + n.H;

                    int j = inList(nb, openList);
                    existsArray[openList[j].P.X, openList[j].P.Y] = 0;
                    openList.RemoveAt(j);
                    openList.Add(n);
                    existsArray[n.P.X, n.P.Y] = 1;
                }
            }
        }
        private int findMinF(List<Node> list)
        {
            int index = 0;
            int min = list[0].F;

            for (int i = 0; i < list.Count; i++)
            {
                if (min >= list[i].F)
                {
                    min = list[i].F;
                    index = i;
                }
            }

            return index;
        }
        private bool ifExistsInList(Point p, List<Node> list)
        {
            bool ret = false;

           /* foreach (Node n in list)
            {
                if (p == n.P)
                {
                    ret = true;
                    break;
                }
            }*/

            return ret;
        }
        private int inList(Point p, List<Node> list)
        {
            int index = -1;
            for (int i = 0; i < list.Count; i++)
            {
                if (p == list[i].P)
                {
                    index = i;
                    break;
                }
            }

            return index;
        }
        private int calcG(Node node)
        {
            int g = current.G;
            if (current.P.X == node.P.X && current.P.Y != node.P.Y)
                g += 1;
            else if (current.P.X != node.P.X && current.P.Y == node.P.Y)
                g += 1;

            if (tissue[node.P.X, node.P.Y].AreaType == VG.Map.AreaEnum.LowDensity)
                g += 1;
            else if (tissue[node.P.X, node.P.Y].AreaType == VG.Map.AreaEnum.MediumDensity)
                g += 2;
            else if (tissue[node.P.X, node.P.Y].AreaType == VG.Map.AreaEnum.HighDensity)
                g += 3;

            
            VG.Map.BloodStream bs = tissue.IsInStream(node.P.X, node.P.Y);
            if (bs != null)
            {
                int dy = node.P.Y - current.P.Y;
                int dx = node.P.X - current.P.X;

                #region Direction
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

                if (plus)
                    g += 2;
                else
                    g -= 2;

                #endregion
            }

            return g;
        }
        private bool ifExistsInPointList(Point p, List<Point> list)
        {
            bool ret = false;
            foreach (Point point in list)
            {
                if (p == point)
                {
                    ret = true;
                    break;
                }
            }
            return ret;
        }
        private int calcH(Point start, Point end)
        {
            int ret = Math.Abs(start.X - end.X) + Math.Abs(start.Y - end.Y);
            return ret;
        }
    }
}