using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using VG.Map;

namespace Anganar
{
    struct node
    {
        public int Number;
        public Point Location;
        public int g;
        public double h;
        public double f;
        public int parent;
        public int terrain;
        public int costUp;
        public int costDown;
        public int costLeft;
        public int costRight;
    }

    class AStarPathfinder
    {
        node[,] Map;
        int[,] States;
        double[,] fs;
        int Xdim;
        int Ydim;

        public AStarPathfinder(MyAI _player)
        {
            Xdim = _player.Tissue.Width;
            Ydim = _player.Tissue.Height;
            Map = new node[Xdim, Ydim];
            States = new int[Xdim, Ydim];
            fs = new double[Xdim, Ydim];
            GetMap(Xdim, Ydim, _player);
        }

        private bool GetMap(int X_dimension, int Y_dimension, MyAI _player)
        {
            Point p = new Point();
            for (int i = 0; i < X_dimension; i++)
            {
                for (int j = 0; j < Y_dimension; j++)
                {
                    p.X = i;
                    p.Y = j;
                    Map[i, j].Location = p;
                    Map[i, j].g = -1;
                    Map[i, j].h = -1;
                    Map[i, j].f = -1;
                    Map[i, j].Number = i * X_dimension + j;
                    Map[i, j].parent = -1;
                    switch ((int)_player.Tissue[i, j].AreaType)
                    {
                        case 0:
                            {
                                Map[i, j].costDown = 2;
                                Map[i, j].costLeft = 2;
                                Map[i, j].costRight = 2;
                                Map[i, j].costUp = 2;
                                Map[i, j].terrain = 2;
                                break;
                            }
                        case 1:
                            {
                                Map[i, j].costDown = 3;
                                Map[i, j].costLeft = 3;
                                Map[i, j].costRight = 3;
                                Map[i, j].costUp = 3;
                                Map[i, j].terrain = 3;
                                break;
                            }
                        case 2:
                            {
                                Map[i, j].costDown = 4;
                                Map[i, j].costLeft = 4;
                                Map[i, j].costRight = 4;
                                Map[i, j].costUp = 4;
                                Map[i, j].terrain = 4;
                                break;
                            }
                        case 3:
                            {
                                Map[i, j].costDown = 6;
                                Map[i, j].costLeft = 6;
                                Map[i, j].costRight = 6;
                                Map[i, j].costUp = 6;
                                Map[i, j].terrain = 6;
                                break;
                            }
                        default:
                            {
                                Map[i, j].costDown = -1;
                                Map[i, j].costLeft = -1;
                                Map[i, j].costRight = -1;
                                Map[i, j].costUp = -1;
                                Map[i, j].terrain = -1;
                                break;
                            }
                    }
                    if (Map[i, j].terrain != -1)
                    {
                        switch (GetStreamDirection(_player, p))
                        {
                            case 0:
                                {
                                    //NorthSouth
                                    if (Map[i, j].terrain != 2)
                                    {
                                        Map[i, j].costLeft -= 2;
                                        Map[i, j].costRight -= 2;
                                        Map[i, j].costUp -= 2;
                                        Map[i, j].costDown += 2;
                                    }
                                    else
                                    {
                                        Map[i, j].costLeft -= 1;
                                        Map[i, j].costRight -= 1;
                                        Map[i, j].costUp -= 1;
                                        Map[i, j].costDown += 2;
                                    }
                                    break;
                                }
                            case 1:
                                {
                                    //SouthNorth
                                    if (Map[i, j].terrain != 2)
                                    {
                                        Map[i, j].costLeft -= 2;
                                        Map[i, j].costRight -= 2;
                                        Map[i, j].costUp += 2;
                                        Map[i, j].costDown -= 2;
                                    }
                                    else
                                    {
                                        Map[i, j].costLeft -= 1;
                                        Map[i, j].costRight -= 1;
                                        Map[i, j].costUp += 2;
                                        Map[i, j].costDown -= 1;
                                    }
                                    break;
                                }
                            case 2:
                                {
                                    //WestEst
                                    if (Map[i, j].terrain != 2)
                                    {
                                        Map[i, j].costLeft += 2;
                                        Map[i, j].costRight -= 2;
                                        Map[i, j].costUp -= 2;
                                        Map[i, j].costDown -= 2;
                                    }
                                    else
                                    {
                                        Map[i, j].costLeft += 2;
                                        Map[i, j].costRight -= 1;
                                        Map[i, j].costUp -= 1;
                                        Map[i, j].costDown -= 1;
                                    }
                                    break;
                                }
                            case 3:
                                {
                                    //EstWest
                                    if (Map[i, j].terrain != 2)
                                    {
                                        Map[i, j].costLeft -= 2;
                                        Map[i, j].costRight += 2;
                                        Map[i, j].costUp -= 2;
                                        Map[i, j].costDown -= 2;
                                    }
                                    else
                                    {
                                        Map[i, j].costLeft -= 1;
                                        Map[i, j].costRight += 2;
                                        Map[i, j].costUp -= 2;
                                        Map[i, j].costDown -= 2;
                                    }
                                    break;
                                }
                            default:
                                {
                                    //NoStream
                                    break;
                                }
                        }
                    }
                }
            }
            return true;
        }

        private double HeuristicFunction(Point from, Point to)
        {
            return (Math.Abs(from.X - to.X) + Math.Abs(from.Y - to.Y)) * (1.00001);
        }

        private static int CompareNodesByFValues(node n1, node n2)
        {
            if (n1.f > n2.f)
            {
                return -1;
            }
            else if (n1.f == n2.f)
            {
                return 0;
            }
            else
            {
                return 1;
            }
        }

        private int FindIndexOfNode(List<node> List, int Number)
        {
            int i = 0;
            while (List[i].Number != Number)
                i++;
            return i;
        }

        public Point[] FindPath(Point Start, Point Target)
        {
            List<node> Opened = new List<node>();
            List<node> Closed = new List<node>();
            int i;
            int j;
            double tmp;
            int k;
            node tmpnode;
            node curnode;
            int TargetNumber = Target.X * Xdim + Target.Y;

            for (i = 0; i < Xdim; i++)
            {
                for (j = 0; j < Ydim; j++)
                {
                    Map[i, j].g = -1;
                    Map[i, j].h = -1;
                    Map[i, j].f = -1;
                    Map[i, j].parent = -1;
                    States[i, j] = 0;
                    fs[i, j] = 0;
                }
            }

            Map[Start.X, Start.Y].g = 0;
            Map[Start.X, Start.Y].h = HeuristicFunction(Map[Start.X, Start.Y].Location, Target);
            Map[Start.X, Start.Y].f = Map[Start.X, Start.Y].g + Map[Start.X, Start.Y].h;
            States[Start.X, Start.Y] = 1;
            fs[Start.X, Start.Y] = Map[Start.X, Start.Y].f;

            curnode = Map[Start.X, Start.Y];

            while (curnode.Number != TargetNumber)
            {
                //ExaminationOfNewNodes
                if (curnode.Location.X < Xdim - 1)
                {
                    i = curnode.Location.X + 1;
                    j = curnode.Location.Y;
                    if (Map[i, j].terrain != -1)
                    {
                        switch (States[i, j])
                        {
                            case 0:
                                {
                                    Map[i, j].g = curnode.g + curnode.costRight;
                                    Map[i, j].h = HeuristicFunction(Map[i, j].Location, Target);
                                    Map[i, j].f = Map[i, j].g + Map[i, j].h;
                                    Map[i, j].parent = curnode.Number;
                                    States[i, j] = 1;
                                    fs[i, j] = Map[i, j].f;
                                    Opened.Add(Map[i, j]);
                                    break;
                                }
                            case 1:
                                {
                                    tmp = curnode.g + curnode.costRight + HeuristicFunction(new Point(i, j), Target);
                                    if (tmp < fs[i, j])
                                    {
                                        k = FindIndexOfNode(Opened, i * Xdim + j);
                                        tmpnode = Opened[k];
                                        tmpnode.g = curnode.g + curnode.costRight;
                                        tmpnode.h = HeuristicFunction(Opened[k].Location, Target);
                                        tmpnode.f = tmpnode.g + tmpnode.h;
                                        fs[i, j] = tmpnode.f;
                                        tmpnode.parent = curnode.Number;
                                        Map[i, j].parent = curnode.Number;
                                        Opened.RemoveAt(k);
                                        Opened.Add(tmpnode);
                                    }
                                    break;
                                }
                            case 2:
                                {
                                    tmp = curnode.g + curnode.costRight + HeuristicFunction(new Point(i, j), Target);
                                    if (tmp < fs[i, j])
                                    {
                                        k = FindIndexOfNode(Closed, i * Xdim + j);
                                        tmpnode = Closed[k];
                                        tmpnode.g = curnode.g + curnode.costRight;
                                        tmpnode.h = HeuristicFunction(Closed[k].Location, Target);
                                        tmpnode.f = tmpnode.g + tmpnode.h;
                                        fs[i, j] = tmpnode.f;
                                        tmpnode.parent = curnode.Number;
                                        Map[i, j].parent = curnode.Number;
                                        Closed.RemoveAt(k);
                                        Closed.Add(tmpnode);
                                    }
                                    break;
                                }
                            default:
                                {
                                    Console.WriteLine("Logikal error!");
                                    break;
                                }
                        }
                    }
                }
                if (curnode.Location.X > 0)
                {
                    i = curnode.Location.X - 1;
                    j = curnode.Location.Y;
                    if (Map[i, j].terrain != -1)
                    {
                        switch (States[i, j])
                        {
                            case 0:
                                {
                                    Map[i, j].g = curnode.g + curnode.costLeft;
                                    Map[i, j].h = HeuristicFunction(Map[i, j].Location, Target);
                                    Map[i, j].f = Map[i, j].g + Map[i, j].h;
                                    Map[i, j].parent = curnode.Number;
                                    States[i, j] = 1;
                                    fs[i, j] = Map[i, j].f;
                                    Opened.Add(Map[i, j]);
                                    break;
                                }
                            case 1:
                                {
                                    tmp = curnode.g + curnode.costLeft + HeuristicFunction(new Point(i, j), Target);
                                    if (tmp < fs[i, j])
                                    {
                                        k = FindIndexOfNode(Opened, i * Xdim + j);
                                        tmpnode = Opened[k];
                                        tmpnode.g = curnode.g + curnode.costLeft;
                                        tmpnode.h = HeuristicFunction(Opened[k].Location, Target);
                                        tmpnode.f = tmpnode.g + tmpnode.h;
                                        fs[i, j] = tmpnode.f;
                                        tmpnode.parent = curnode.Number;
                                        Map[i, j].parent = curnode.Number;
                                        Opened.RemoveAt(k);
                                        Opened.Add(tmpnode);
                                    }
                                    break;
                                }
                            case 2:
                                {
                                    tmp = curnode.g + curnode.costLeft + HeuristicFunction(new Point(i, j), Target);
                                    if (tmp < fs[i, j])
                                    {
                                        k = FindIndexOfNode(Closed, i * Xdim + j);
                                        tmpnode = Closed[k];
                                        tmpnode.g = curnode.g + curnode.costLeft;
                                        tmpnode.h = HeuristicFunction(Closed[k].Location, Target);
                                        tmpnode.f = tmpnode.g + tmpnode.h;
                                        fs[i, j] = tmpnode.f;
                                        tmpnode.parent = curnode.Number;
                                        Map[i, j].parent = curnode.Number;
                                        Closed.RemoveAt(k);
                                        Closed.Add(tmpnode);
                                    }
                                    break;
                                }
                            default:
                                {
                                    Console.WriteLine("Logikal error!");
                                    break;
                                }
                        }
                    }
                }
                if (curnode.Location.Y < Ydim - 1)
                {
                    i = curnode.Location.X;
                    j = curnode.Location.Y + 1;
                    if (Map[i, j].terrain != -1)
                    {
                        switch (States[i, j])
                        {
                            case 0:
                                {
                                    Map[i, j].g = curnode.g + curnode.costUp;
                                    Map[i, j].h = HeuristicFunction(Map[i, j].Location, Target);
                                    Map[i, j].f = Map[i, j].g + Map[i, j].h;
                                    Map[i, j].parent = curnode.Number;
                                    States[i, j] = 1;
                                    fs[i, j] = Map[i, j].f;
                                    Opened.Add(Map[i, j]);
                                    break;
                                }
                            case 1:
                                {
                                    tmp = curnode.g + curnode.costUp + HeuristicFunction(new Point(i, j), Target);
                                    if (tmp < fs[i, j])
                                    {
                                        k = FindIndexOfNode(Opened, i * Xdim + j);
                                        tmpnode = Opened[k];
                                        tmpnode.g = curnode.g + curnode.costUp;
                                        tmpnode.h = HeuristicFunction(Opened[k].Location, Target);
                                        tmpnode.f = tmpnode.g + tmpnode.h;
                                        fs[i, j] = tmpnode.f;
                                        tmpnode.parent = curnode.Number;
                                        Map[i, j].parent = curnode.Number;
                                        Opened.RemoveAt(k);
                                        Opened.Add(tmpnode);
                                    }
                                    break;
                                }
                            case 2:
                                {
                                    tmp = curnode.g + curnode.costUp + HeuristicFunction(new Point(i, j), Target);
                                    if (tmp < fs[i, j])
                                    {
                                        k = FindIndexOfNode(Closed, i * Xdim + j);
                                        tmpnode = Closed[k];
                                        tmpnode.g = curnode.g + curnode.costUp;
                                        tmpnode.h = HeuristicFunction(Closed[k].Location, Target);
                                        tmpnode.f = tmpnode.g + tmpnode.h;
                                        fs[i, j] = tmpnode.f;
                                        tmpnode.parent = curnode.Number;
                                        Map[i, j].parent = curnode.Number;
                                        Closed.RemoveAt(k);
                                        Closed.Add(tmpnode);
                                    }
                                    break;
                                }
                            default:
                                {
                                    Console.WriteLine("Logikal error!");
                                    break;
                                }
                        }
                    }
                }
                if (curnode.Location.Y > 0)
                {
                    i = curnode.Location.X;
                    j = curnode.Location.Y - 1;
                    if (Map[i, j].terrain != -1)
                    {
                        switch (States[i, j])
                        {
                            case 0:
                                {
                                    Map[i, j].g = curnode.g + curnode.costDown;
                                    Map[i, j].h = HeuristicFunction(Map[i, j].Location, Target);
                                    Map[i, j].f = Map[i, j].g + Map[i, j].h;
                                    Map[i, j].parent = curnode.Number;
                                    States[i, j] = 1;
                                    fs[i, j] = Map[i, j].f;
                                    Opened.Add(Map[i, j]);
                                    break;
                                }
                            case 1:
                                {
                                    tmp = curnode.g + curnode.costDown + HeuristicFunction(new Point(i, j), Target);
                                    if (tmp < fs[i, j])
                                    {
                                        k = FindIndexOfNode(Opened, i * Xdim + j);
                                        tmpnode = Opened[k];
                                        tmpnode.g = curnode.g + curnode.costDown;
                                        tmpnode.h = HeuristicFunction(Opened[k].Location, Target);
                                        tmpnode.f = tmpnode.g + tmpnode.h;
                                        fs[i, j] = tmpnode.f;
                                        tmpnode.parent = curnode.Number;
                                        Map[i, j].parent = curnode.Number;
                                        Opened.RemoveAt(k);
                                        Opened.Add(tmpnode);
                                    }
                                    break;
                                }
                            case 2:
                                {
                                    tmp = curnode.g + curnode.costDown + HeuristicFunction(new Point(i, j), Target);
                                    if (tmp < fs[i, j])
                                    {
                                        k = FindIndexOfNode(Closed, i * Xdim + j);
                                        tmpnode = Closed[k];
                                        tmpnode.g = curnode.g + curnode.costDown;
                                        tmpnode.h = HeuristicFunction(Closed[k].Location, Target);
                                        tmpnode.f = tmpnode.g + tmpnode.h;
                                        fs[i, j] = tmpnode.f;
                                        tmpnode.parent = curnode.Number;
                                        Map[i, j].parent = curnode.Number;
                                        Closed.RemoveAt(k);
                                        Closed.Add(tmpnode);
                                    }
                                    break;
                                }
                            default:
                                {
                                    Console.WriteLine("Logikal error!");
                                    break;
                                }
                        }
                    }
                }
                Closed.Add(curnode);
                States[curnode.Location.X, curnode.Location.Y] = 2;
                Opened.Sort(CompareNodesByFValues);
                curnode = Opened[Opened.Count - 1];
                Opened.RemoveAt(Opened.Count - 1);
            }
            ArrayList ReversePath = new ArrayList();
            ReversePath.Add(curnode);
            while (curnode.parent != -1)
            {
                j = curnode.parent % Xdim;
                i = (curnode.parent - (curnode.parent % Xdim)) / Xdim;
                curnode = Map[i, j];
                ReversePath.Add(curnode);
            }
            //
            //PrintCurNodes(Opened, Closed);
            //
            Point[] path = new Point[ReversePath.Count];
            for (i = 0; i < ReversePath.Count; i++)
            {
                path[i] = ((node)ReversePath[ReversePath.Count - 1 - i]).Location;
            }

            return path;
        }

        private void PrintCurNodes(List<node> Open, List<node> Close)
        {
            char[,] Map1 = new char[Xdim, Ydim];
            for (int i = 0; i < Xdim; i++)
            {
                for (int j = 0; j < Ydim; j++)
                {
                    if (Map[i, j].terrain == -1)
                    {
                        Map1[i, j] = 'X';
                    }
                    else
                    {
                        Map1[i, j] = ' ';
                    }
                }
            }
            for (int i = 0; i < Open.Count; i++)
            {
                Map1[Open[i].Location.X, Open[i].Location.Y] = 'P';
            }
            for (int i = 0; i < Close.Count; i++)
            {
                Map1[Close[i].Location.X, Close[i].Location.Y] = 'C';
            }
            for (int i = 0; i < Xdim; i++)
            {
                for (int j = 0; j < Ydim; j++)
                {
                    Console.Write("{0}", Map1[i, j]);
                }
                Console.WriteLine();
            }
            /*
            for (int i = 0; i < Open.Count; i++)
            {
                Console.WriteLine("Open{0}: X={1} Y={2} f={3} g={4} h={5} Number={6} Parent={7}", i, Open[i].Location.X, Open[i].Location.Y, Open[i].f, Open[i].g, Open[i].h, Open[i].Number, Open[i].parent);
            }
            for (int i = 0; i < Close.Count; i++)
            {
                Console.WriteLine("Close{0}: X={1} Y={2} f={3} g={4} h={5} Number={6} Parent={7}", i, Close[i].Location.X, Close[i].Location.Y, Close[i].f, Close[i].g, Close[i].h, Close[i].Number, Close[i].parent);
            }
            */
            Console.WriteLine();
        }

        private int GetStreamDirection(MyAI _player, Point p)
        {
            BloodStream stream = _player.Tissue.IsInStream(p.X, p.Y);
            if (stream != null)
            {
                return (int)stream.Direction;
            }
            return -1;
        }
    }
}