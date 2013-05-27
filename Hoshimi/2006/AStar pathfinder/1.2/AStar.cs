using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace Pathfinder
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

    class PrecomputedPath
    {
        private Point From;
        private Point To;
        private Point[] Path;

        public PrecomputedPath(Point p1, Point p2, Point[] p)
        {
            this.From = p1;
            this.To = p2;
            this.Path = p;
        }

        #region Properties
        public Point pFrom
        {
            get
            {
                return From;
            }
            set
            {
                From = value;
            }
        }

        public Point pTo
        {
            get
            {
                return To;
            }
            set
            {
                To = value;
            }
        }

        public Point[] pPath
        {
            get
            {
                return Path;
            }
            set
            {
                Path = value;
            }
        }
        #endregion
    }

    class AStar
    {
        private List<Node> openList;
        private List<Node> closeList;
        private List<PrecomputedPath> pathCache;
        private Node current;
        private int x;
        private int y;
        private VG.Map.Tissue tissue;
        int[,] existsArray; /*массив для определения в каком списке лежит point
                             * 0 - точка ни в одном списке
                             * 1 - точка в открытом списке
                             * 2 - точка в закрытом списке 
                             * =))
                             * */

        public AStar(VG.Map.Tissue tissue)
        {
            openList = new List<Node>();
            closeList = new List<Node>();
            pathCache = new List<PrecomputedPath>();

            this.tissue = tissue;
            this.x = tissue.Width;
            this.y = tissue.Width;
            this.existsArray = new int[this.x, this.y];
        }

        public Point[] FindPath(Point start, Point end)
        {
            //Ищем путь в кэше, если находим, то тут же завершаемся
            Point[] PrecompPath = ExaminePathCache(start, end);
            if (PrecompPath != null)
                return PrecompPath;

            List<Point> p = new List<Point>(); //конечный список

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

                //проверка соседних клеток
                Point nb = current.P;
                nbPoint(new Point(nb.X + 1, nb.Y), current, end, x, y);
                nbPoint(new Point(nb.X, nb.Y - 1), current, end, x, y);
                nbPoint(new Point(nb.X - 1, nb.Y), current, end, x, y);
                nbPoint(new Point(nb.X, nb.Y + 1), current, end, x, y);

                if (openList.Count == 0) //открытый списко пустой - путь невозможен
                {
                    ex = true;
                    can = false;
                }
                if (existsArray[end.X, end.Y] == 1) //дошли до финала
                    ex = true;
            }

            //составление искомого пути
            if (can)
            {
                p.Add(end);
                Point pp = end;
                int i = 0;

                while (pp != start)
                {
                    if (existsArray[pp.X, pp.Y] == 1)
                    {
                        i = inList(pp, openList);
                        if (i < 0)
                            break;
                        pp = openList[i].Parent;
                    }
                    else if (existsArray[pp.X, pp.Y] == 2)
                    {
                        i = inList(pp, closeList);
                        if (i < 0)
                            break;
                        pp = closeList[i].Parent;
                    }
                    p.Add(pp);
                }
            }

            Point[] ret = new Point[p.Count];
            for (int i = 0; i < p.Count; i++)
                ret[i] = p[p.Count - i - 1];

            clear();

            pathCache.Add(new PrecomputedPath(start, end, ret)); //запись пути в кеш
            return ret;
        }

        private void nbPoint(Point nb, Node current, Point end, int X, int Y) //функция обрабатывющая "соседнюю" клетку
        {
            //различные проверки клетки - границы массива,закрытый список итд.
            if (nb.X >= X || nb.X < 0)
                return;
            if (nb.Y >= Y || nb.Y < 0)
                return;
            if (existsArray[nb.X, nb.Y] == 2)
                return;
            if (!tissue.IsInMap(nb.X, nb.Y))
                return;
            if( tissue[nb.X,nb.Y].AreaType == VG.Map.AreaEnum.Bone )
                return;

            //если нет в открытом списке - добавляем
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
            else //если есть - страдаем некоторой фигней =)
            {
                int j = inList(nb, openList);
                Node n = openList[j];
                if (n.G > calcG(n))
                {
                    n.Parent = current.P;
                    n.G = calcG(n);
                    n.F = n.G + n.H;
                    openList.RemoveAt(j);
                    openList.Add(n);
                }
            }
        }
        private int findMinF(List<Node> list) //поиск элемента с минимальным "F"
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
        private int inList(Point p, List<Node> list) //возвращает индекс точки "p" в списке
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
            
            if (tissue[node.P.X, node.P.Y].AreaType == VG.Map.AreaEnum.LowDensity)
                g += 2;
            else if (tissue[node.P.X, node.P.Y].AreaType == VG.Map.AreaEnum.MediumDensity)
                g += 3;
            else if (tissue[node.P.X, node.P.Y].AreaType == VG.Map.AreaEnum.HighDensity)
                g += 4;
            
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

                if (tissue[node.P.X, node.P.Y].AreaType == VG.Map.AreaEnum.LowDensity)
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
        private int calcH(Point start, Point end)
        {
            int ret = Math.Abs(start.X - end.X) + Math.Abs(start.Y - end.Y);
            return ret;
        }
        private void clear()
        {
            openList.Clear();
            closeList.Clear();
            for (int i = 0; i < this.x; i++)
            {
                for (int j = 0; j < this.y; j++)
                    existsArray[i, j] = 0;
            }

        }


        private Point[] ExaminePathCache(Point Start, Point End) //Поиск пути в кэше
        {
            for (int i = 0; i < pathCache.Count; i++)
            {
                if ((pathCache[i].pFrom == Start) && (pathCache[i].pTo == End))
                    return pathCache[i].pPath;
            }
            return null;
        }
    }
}