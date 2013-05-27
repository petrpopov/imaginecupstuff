using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;


namespace AStar
{
    class Program
    {
        static void Main()
        {
            const int X = 4;
            const int Y = 4;
            int[,] map = new int[X, Y];
            for (int i = 0; i < X; i++)
                for (int j = 0; j < Y; j++)
                    map[i, j] = 0;
            map[1, 2] = 1;
            map[2, 2] = 1;
            map[3, 2] = 1;
            

            for (int i = 0; i < X; i++)
            {
                for (int j = 0; j < Y; j++)
                    Console.Write(map[i,j]);
                Console.WriteLine();
            }
            Console.WriteLine();

            AStar star = new AStar();
            Point[] list = star.findPath(new Point(3, 1), new Point(3, 3), X, Y, map);
            for (int i = 0; i < list.Length; i++)
            {
                Console.WriteLine("Point {0}: {1}, {2}", i, list[i].X, list[i].Y);
            }

            Console.ReadLine();
        }
    }

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

        public AStar()
        {
            openList = new List<Node>();
            closeList = new List<Node>();
        }

        public Point[] findPath(Point start, Point end, int X, int Y, int[,] Map)
        {
            List<Point> p = new List<Point>();
            Node current = new Node(start, 0, 0, 0);
            current.H = calcH(start, end);
            current.F = current.H;
            openList.Add(current);

            bool ex = false;
            bool can = true;
            while (!ex)
            {
                int i = findMinF(openList);
                current = openList[i];
                openList.RemoveAt(i);
                closeList.Add(current);

                Point nb = current.P;
                nbPoint(new Point(nb.X + 1, nb.Y), current, end, X, Y, Map);
                nbPoint(new Point(nb.X + 1, nb.Y - 1), current, end, X, Y, Map);
                nbPoint(new Point(nb.X, nb.Y - 1), current, end, X, Y, Map);
                nbPoint(new Point(nb.X - 1, nb.Y - 1), current, end, X, Y, Map);
                nbPoint(new Point(nb.X - 1, nb.Y), current, end, X, Y, Map);
                nbPoint(new Point(nb.X - 1, nb.Y + 1), current, end, X, Y, Map);
                nbPoint(new Point(nb.X, nb.Y + 1), current, end, X, Y, Map);
                nbPoint(new Point(nb.X + 1, nb.Y + 1), current, end, X, Y, Map);

                if (openList.Count == 0)
                {
                    ex = true;
                    can = false;
                }
                if (ifExistsInList(end, openList))
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

        private void nbPoint(Point nb, Node current, Point end, int X, int Y, int[,] Map)
        {
            if (nb.X >= X || nb.X < 0)
                return;
            if (nb.Y >= Y || nb.Y < 0)
                return;
            if (ifExistsInList(nb, closeList))
                return;
            if (Map[nb.X, nb.Y] == 1)
                return;
            
            if (!ifExistsInList(nb, openList))
            {
                    Node n = new Node(nb, 0, 0, 0);
                    n.H = calcH(nb, end);
                    n.G = calcG(n);
                    n.F = n.G + n.H;
                    n.Parent = current.P;
                    openList.Add(n);
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
                    openList.RemoveAt(j);
                    openList.Add(n);
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

            foreach (Node n in list)
            {
                if (p == n.P)
                    ret = true;
            }

            return ret;
        }
        private int inList(Point p, List<Node> list)
        {
            int index = -1;
            for (int i = 0; i < list.Count; i++)
            {
                if (p == list[i].P)
                    index = i;
            }

            return index;   
        }
        private int calcG(Node node)
        {
            int g = 0;
            foreach (Node n in closeList)
                g += n.G;

            Point p = closeList[closeList.Count - 1].P;
            if (p.X == node.P.X && p.Y != node.P.Y)
                g += 10;
            else if (p.X != node.P.X && p.Y == node.P.Y)
                g += 10;
            else if (p.X != node.P.X && p.Y != node.P.Y)
                g += 22;

            return g;
        }
        public bool ifExistsInPointList(Point p, List<Point> list)
        {
            bool ret = false;
            foreach (Point point in list)
            {
                if (p == point)
                    ret = true;
            }
            return ret;
        }
        private int calcH(Point start, Point end)
        {
            int ret = Math.Abs(start.X - end.X) + Math.Abs(start.Y - end.Y);
            ret *= 10;
            return ret;
        }
    }
}