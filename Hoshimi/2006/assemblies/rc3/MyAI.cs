using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using VG.Common;
using System.Drawing;
using Pathfinder;
//using System.Diagnostics;

    #region MyDataStructures

    struct HoshimiPointInfo
    {
        public Point Location;
        public int Needle;
        public int Full;
        public int InPast;
        public int Peasants;
        public int Convoys;
    }

    struct AZNPointInfo
    {
        public Point Location;
    }

    struct BattlePointInfo
    {
        public Point Location;
        public int Covered;
    }

    struct AtackPointInfo
    {
        public Point Location;
        public int Need;
        public int Exist;
    }

    struct NavigationPointInfo
    {
        public Point Location;
        public int StartTurn;
        public int EndTurn;
        public bool Complete;
        public int Navigators;
        public VG.Common.NanoBotType BotType;
        public int Stock;
    }

    struct ScoreInfo
    {
        public int Score;
        public int Turn;
    }

    #endregion

namespace Anganar
{
	class MyAI : VG.Common.Player 
	{
        public AStar Pathfinder;
        public eAStar ePathfinder;

        #region ����������� ����������

        //������ �������.
        public Convoy[] Convoys = null;
        //������ �������������� AI
        private BodyGuard[] BodyGuards = null;
        //������ �������, ������� ���� ��������.
        public List<BattlePointInfo> BTargets = new List<BattlePointInfo>();
        //������ �����, ������� ���� ���������.
        public List<AtackPointInfo> ATargets = new List<AtackPointInfo>();
        //������ ������� �����, ������� �� ���������� ������
        public ArrayList MyHPs = new ArrayList();
        //������ ���� AZNPoints
        public AZNPointInfo[] AZNPoints = null;
        //������ ���� HoshimiPoints
        public HoshimiPointInfo[] HoshimiPoints = null;
        //������ ���� NavigationPoints
        public NavigationPointInfo[] NavigationPoints = null;
        //������ ���� ScoreMissions
        public ScoreInfo[] ScoreObjectives = null;
        //��������� �� �����? (������������ �� ������� AIAliveObjective)
        bool BattleExpected = false;
        //���������� �� �� ������� � Pierre`��?
        bool KillPierre = false;
        //����� HP, ������� ��� �� �������� ����� ��� ���������� ScoreObjectives
        int MinHPsToTake = 0;

        //������� ����� ����� ��� ���� ���������
        int NBPROTECTORTOBUILD = 0; //�������� ����, ��������� � ������ BTargets
        int NBATACKERTOBUILD = 0;   //������� ����, ��������� � ������ ATargets
        int NBCOLLECTORTOBUILD = 0; //�� ������ ������ �� ������������
        int NBCONTAINERTOBUILD = 0; //�� ������ ������ �� ������������
        int NBEXPLORERTOBUILD = 0;  //�� ������ ������ �� ������������
        int NBNAVIGATORTOBUILD = 0; //����� �� NavigationPoint`��
        int NBBODYGUARSTOBUILD;     //����� ������ � AI
        int OBSERVERSTOBUILD = 0;   //����� �� AI.
        int NBDOCTORSTOBUILD = 0;   //����������-"����������"
        int CDTB = 0;               //������ � ������ �������
        int CCTB = 0;               //������ � ������ �������

        //������� ���������� �� HP �� HP � AP.
        //�������������� ��������, ��� Pathfinder`�.
        int[,] HPtoHPdistances;
        int[,] HPtoAPdistances;
        //int[] used;
        //Stack<int> path;
        //Stack<int> BestPath;
        //int[] BestPath;

        //������ ��� ���������� NavigationObjectives
        public ConvoyWithCollector[] CollConvoys = null;
        int CDTB2 = 0;               //������ � ������ �������
        int CCTB2 = 0;               //������ � ������ �������
        public ConvoyWithBigContainer[] BigConvoys = null;
        int CDTB3 = 0;               //������ � ������ �������
        int CCTB3 = 0;               //������ � ������ �������

        int ProceededHP = 0;

        bool StrongMove = false;

        //����� HP, ���� � ������ ������ ��� AI
        int NextHPNum = -1;
        Point NextHP = new Point(-1, -1);

        #endregion

        public MyAI() { }

		public MyAI(string _name, int _id): base(_name, _id)
		{
			this.ChooseInjectionPointEvent += new ChooseInjectionPointEventHandler(MyAI_ChooseInjectionPointEvent);
			this.WhatToDoNextEvent += new WhatToDoNextEventHandler(MyAI_WhatToDoNextEvent);
        }

        #region ������� �������������� �� �����

        //���������� ���� ��� ������, � ������� �� ��������� ��� �� ���� protector
        public Point GetBattleTarget(Point CurLocation)
        {
            BattlePointInfo bp = new BattlePointInfo();
            for (int i = 0; i < BTargets.Count; i++)
            {
                if (BTargets[i].Covered == 0)
                {
                    bp.Location = BTargets[i].Location;
                    bp.Covered = 1;
                    BTargets.RemoveAt(i);
                    BTargets.Add(bp);
                    return bp.Location;
                }
            }
            return CurLocation;
        }

        public Point GetTargetToAtack(Point CurLocation)
        {
            AtackPointInfo ap = new AtackPointInfo();
            for (int i = 0; i < ATargets.Count; i++)
            {
                if (ATargets[i].Need > ATargets[i].Exist)
                {
                    ap = ATargets[i];
                    ap.Exist++;
                    ATargets.RemoveAt(i);
                    ATargets.Add(ap);
                    return ap.Location;
                }
            }
            return CurLocation;
        }

        //���������� ��������� AZNPoint
        public Point GetNearestAZNPoint(Point CurLocation, Point Destination)
        {
            double Distance;
            double Distance1 = 1000;
            double Distance2 = 1000;
            Point tmp1 = new Point(-1, -1);
            Point tmp2 = new Point(-1, -1);
            for (int i = 0; i < this.AZNPoints.Length; i++)
            {
                Distance = ManhattanDistance(AZNPoints[i].Location, CurLocation);
                if (Distance < Distance1)
                {
                    Distance1 = Distance;
                    tmp1 = AZNPoints[i].Location;
                }
            }
            for (int i = 0; i < this.AZNPoints.Length; i++)
            {
                Distance = ManhattanDistance(AZNPoints[i].Location, Destination);
                if (Distance < Distance2)
                {
                    Distance2 = Distance;
                    tmp2 = AZNPoints[i].Location;
                }
            }
            if ((Distance1 + ManhattanDistance(tmp1, Destination)) < (ManhattanDistance(CurLocation, tmp2) + Distance2))
            {
                return tmp1;
            }
            else
            {
                return tmp2;
            }
        }

        public Point GetNearestAZNPoint(int CurHP, int DestHP)
        {
            double Distance;
            double Distance1 = 1000;
            double Distance2 = 1000;
            int tmp1 = -1;
            int tmp2 = -1;
            for (int i = 0; i < this.AZNPoints.Length; i++)
            {
                Distance = HPtoAPdistances[CurHP, i];
                if (Distance < Distance1)
                {
                    Distance1 = Distance;
                    tmp1 = i;
                }
            }
            for (int i = 0; i < this.AZNPoints.Length; i++)
            {
                Distance = HPtoAPdistances[DestHP, i];
                if (Distance < Distance2)
                {
                    Distance2 = Distance;
                    tmp2 = i;
                }
            }
            if ((Distance1 + HPtoAPdistances[DestHP, tmp1]) < (HPtoAPdistances[CurHP, tmp2] + Distance2))
            {
                return AZNPoints[tmp1].Location;
            }
            else
            {
                return AZNPoints[tmp2].Location;
            }
        }

        //���������� ��������� HP, �� ������� �� ����� Needle (��� AI)
        public Point GetNearestUngettedHP(Point CurLocation)
        {
            double Distance;
            double MinDistance = 1000;
            Point tmp = new Point(-1, -1);
            int j = -1;
            int Number;
            for (int i = 0; i < MyHPs.Count; i++)
            {
                Number = (int)MyHPs[i];
                Distance = ManhattanDistance(HoshimiPoints[Number].Location, CurLocation);
                if ((Distance < MinDistance) && (HoshimiPoints[Number].Needle == 0))
                {
                    MinDistance = Distance;
                    j = i;
                    tmp = HoshimiPoints[Number].Location;
                }
            }
            if (j != -1)
            {
                return tmp;
            }
            else
            {
                return CurLocation;
            }
        }

        //���������� ��������� �� ������ HP, �� ������� �� ����� Needle (��� AI)
        public Point GetNextUngettedHP(Point CurLocation, ref int Num)
        {
            Point tmp = new Point(-1, -1);
            int Number;
            for (int i = 0; i < MyHPs.Count; i++)
            {
                Number = (int)MyHPs[i];
                if ((HoshimiPoints[Number].Needle == 0)&&(HoshimiPoints[Number].InPast == 0))
                {
                    Num = Number;
                    return HoshimiPoints[Number].Location;
                }
            }
            return CurLocation;
        }

        //���������� ��������� ������������� HP, � ������� �� ���������� �������.
        //���� ����� ���, �� ������ ��������� ������������� HP.
        //���� � ����� ���, �� CurLocation
        public Point GetNearestHPForConvoy(Point CurLocation, ref int Number)
        {
            int Distance;
            int MinDistance = 10000;
            Point tmp = new Point(-1, -1);
            int j = -1;
            int ntmp;
            for (int i = 0; i < MyHPs.Count; i++)
            {
                ntmp = (int)MyHPs[i];
                Distance = ManhattanDistance(HoshimiPoints[ntmp].Location, CurLocation);
                if ((Distance < MinDistance) && (HoshimiPoints[ntmp].Convoys == 0) && (HoshimiPoints[ntmp].Full == 0) && (HoshimiPoints[ntmp].Needle != 2))
                {
                    MinDistance = Distance;
                    j = ntmp;
                    tmp = HoshimiPoints[ntmp].Location;
                }
            }
            if (j != -1)
            {
                Number = j;
                return tmp;
            }

            for (int i = 0; i < MyHPs.Count; i++)
            {
                ntmp = (int)MyHPs[i];
                Distance = ManhattanDistance(HoshimiPoints[ntmp].Location, CurLocation);
                if ((Distance < MinDistance) && (HoshimiPoints[ntmp].Full == 0) && (HoshimiPoints[ntmp].Needle != 2))
                {
                    MinDistance = Distance;
                    j = ntmp;
                    tmp = HoshimiPoints[ntmp].Location;
                }
            }
            if (j != -1)
            {
                Number = j;
                return tmp;
            }
            else
            {
                Number = -1;
                return CurLocation;
            }
        }

        //���������� ��������� ������������� HP, � ������� �� ���������� �������.
        //���� ����� ���, �� ������ ��������� ������������� HP.
        //���� � ����� ���, �� CurLocation
        public Point GetNextHPForConvoy(Point CurLocation, ref int Number, ref bool Navigating)
        {
            Point tmp = new Point(-1, -1);
            int ntmp;
            if (Navigating)
            {
                tmp = GetNextUndoneHealPointForConvoyWithContainer(CurLocation, ref Number);
                if (Number != -10)
                {
                    return tmp;
                }
                else
                {
                    Navigating = false;
                }
            }

            for (int i = 0; i < MyHPs.Count; i++)
            {
                ntmp = (int)MyHPs[i];
                if ((HoshimiPoints[ntmp].Convoys == 0) && (HoshimiPoints[ntmp].Full == 0) && (HoshimiPoints[ntmp].Needle == 1))
                {
                    Number = ntmp;
                    HoshimiPoints[ntmp].Convoys++;
                    return HoshimiPoints[ntmp].Location;
                }
            }
            for (int i = 0; i < MyHPs.Count; i++)
            {
                ntmp = (int)MyHPs[i];
                if ((HoshimiPoints[ntmp].Convoys == 0) && (HoshimiPoints[ntmp].Full == 0) && (HoshimiPoints[ntmp].Needle != 2) && (HoshimiPoints[ntmp].InPast == 0))
                {
                    Number = ntmp;
                    HoshimiPoints[ntmp].Convoys++;
                    return HoshimiPoints[ntmp].Location;
                }
            }
            for (int i = 0; i < MyHPs.Count; i++)
            {
                ntmp = (int)MyHPs[i];
                if ((HoshimiPoints[ntmp].Full == 0) && (HoshimiPoints[ntmp].Needle != 2) && (HoshimiPoints[ntmp].InPast == 0))
                {
                    Number = ntmp;
                    HoshimiPoints[ntmp].Convoys++;
                    return HoshimiPoints[ntmp].Location;
                }
            }
            Number = -1;
            return CurLocation;
        }

        public Point GetNextHPForConvoyWithCollector(Point CurLocation, ref int Number)
        {
            int MinNavs = 10000;
            int tmp = 0;
            Number = -10;
            for (int i = 0; i < NavigationPoints.Length; i++)
            {
                tmp = NavigationPoints[i].Navigators;
                if ((tmp < MinNavs) && (NavigationPoints[i].Complete == false)
                    && ((NavigationPoints[i].BotType == NanoBotType.NanoCollector)
                        && (NavigationPoints[i].Stock > 10)))
                {
                    MinNavs = NavigationPoints[i].Navigators;
                    Number = i;
                }
            }
            if (Number != -10)
            {
                NavigationPoints[Number].Navigators++;
                return NavigationPoints[Number].Location;
            }
            else
            {
                return CurLocation;
            }
        }

        public Point GetNextHPForConvoyWithBigContainer(Point CurLocation, ref int Number)
        {
            int MinNavs = 10000;
            int tmp = 0;
            Number = -10;
            for (int i = 0; i < NavigationPoints.Length; i++)
            {
                tmp = NavigationPoints[i].Navigators;
                if ((tmp < MinNavs) && (NavigationPoints[i].Complete == false)
                    && ((NavigationPoints[i].BotType == NanoBotType.NanoContainer || (NavigationPoints[i].BotType == NanoBotType.Unknown))
                        && (NavigationPoints[i].Stock > 50)))
                {
                    MinNavs = NavigationPoints[i].Navigators;
                    Number = i;
                }
            }
            if (Number != -10)
            {
                NavigationPoints[Number].Navigators++;
                return NavigationPoints[Number].Location;
            }
            else
            {
                return CurLocation;
            }
        }

        //���������� ��������� �� ������ ������������� NavigationPoint
        //���� ������ ���, �� CurLocation
        public Point GetNextUndoneNavPoint(Point CurLocation, ref int Number)
        {
            int MinNavs = 10000;
            int tmp = 0;
            Number = -10;
            for (int i = 0; i < NavigationPoints.Length; i++)
            {
                tmp = NavigationPoints[i].Navigators;
                if ((tmp < MinNavs) && (NavigationPoints[i].Complete == false) && ((NavigationPoints[i].BotType == NanoBotType.NanoExplorer) || ((NavigationPoints[i].BotType == NanoBotType.Unknown) && (NavigationPoints[i].Stock <= 0))))
                {
                    MinNavs = NavigationPoints[i].Navigators;
                    Number = i;
                }
            }
            if (Number != -10)
            {
                NavigationPoints[Number].Navigators++;
                return NavigationPoints[Number].Location;
            }
            else
            {
                NBNAVIGATORTOBUILD = 0;
                return CurLocation;
            }
        }

        //���������� ��������� �� ������ ������������� Navigation(Heal)Point
        //���� ������ ���, �� CurLocation
        public Point GetNextUndoneHealPoint(Point CurLocation, ref int Number)
        {
            int MinNavs = 10000;
            int tmp = 0;
            Number = -10;
            for (int i = 0; i < NavigationPoints.Length; i++)
            {
                tmp = NavigationPoints[i].Navigators;
                if ((tmp < MinNavs) && (NavigationPoints[i].Complete == false) 
                    && (((NavigationPoints[i].BotType == NanoBotType.NanoCollector) || (NavigationPoints[i].BotType == NanoBotType.Unknown))
                        && (NavigationPoints[i].Stock > 0) && (NavigationPoints[i].Stock <= 10)))
                {
                    MinNavs = NavigationPoints[i].Navigators;
                    Number = i;
                }
            }
            if (Number != -10)
            {
                NavigationPoints[Number].Navigators++;
                return NavigationPoints[Number].Location;
            }
            else
            {
                NBDOCTORSTOBUILD = 0;
                return CurLocation;
            }
        }

        public Point GetNextUndoneHealPointForConvoyWithContainer(Point CurLocation, ref int Number)
        {
            int tmp = 0;
            Number = -10;
            for (int i = 0; i < NavigationPoints.Length; i++)
            {
                tmp = NavigationPoints[i].Navigators;
                if ((tmp == 0) && (NavigationPoints[i].Complete == false)
                    && ((NavigationPoints[i].BotType == NanoBotType.NanoContainer)
                        || ((NavigationPoints[i].BotType == NanoBotType.Unknown) && (NavigationPoints[i].Stock > 0)))
                        && (NavigationPoints[i].Stock <= 50))
                {
                    Number = i;
                }
            }
            if (Number != -10)
            {
                NavigationPoints[Number].Navigators++;
                return NavigationPoints[Number].Location;
            }
            else
            {
                return CurLocation;
            }
        }

        //���������� ��������� ������������� HP
        //AHTUNG!!! ������������ ������ container`��� � collector`���!
        public Point GetNearestUnfilledHP(Point CurLocation, ref int Number)
        {
            double Distance;
            double MinDistance = 1000;
            Point tmp = new Point(-1, -1);
            int j = -1;
            int ntmp;
            for (int i = 0; i < MyHPs.Count; i++)
            {
                ntmp = (int)MyHPs[i];
                Distance = ManhattanDistance(HoshimiPoints[ntmp].Location, CurLocation);
                if ((Distance < MinDistance) && (HoshimiPoints[ntmp].Full == 0))
                {
                    MinDistance = Distance;
                    j = ntmp;
                    tmp = HoshimiPoints[ntmp].Location;
                }
            }
            if (j != -1)
            {
                Number = j;
                return tmp;
            }
            else
            {
                //����������!!!
                //����� ������ - _����_ HP ������� �� ����������!!!
                Number = -10;
                return CurLocation;
            }
        }

        //���������� ������������� HP, � ������� ���������� ���������� ���������� �����������
        //AHTUNG!!! ������������ ������ container`��� � collector`���!
        public Point GetNextHoshimiPoint(Point CurLocation, ref int Number)
        {
            int MinPeas = 10000;
            int tmp1 = 0;
            int tmp2 = 0;
            Number = 0;
            for (int i = 0; i < MyHPs.Count; i++)
            {
                tmp1 = (int)MyHPs[i];
                tmp2 = HoshimiPoints[tmp1].Peasants;
                if ((tmp2 < MinPeas) && (HoshimiPoints[tmp1].Full == 0))
                {
                    MinPeas = HoshimiPoints[tmp1].Peasants;
                    Number = tmp1;
                }
            }
            HoshimiPoints[Number].Peasants++;
            return HoshimiPoints[Number].Location;
        }

        #endregion

        #region ������� ����������� �����

        //���������, ������ �� ���� ��� ���.
        private bool NeedGathering()
        {
            for (int i = 0; i < BodyGuards.Length; i++)
            {
                if (BodyGuards[i] != null && BodyGuards[i].HitPoint > 0 && BodyGuards[i].Location != AI.Location)
                {
                    return true;
                }
            }
            return false;
        }

        //��������� ������, ���� �� �����-�� �������� ��-���� �����������.
        //������������ �� �����. ����� ������������ ������ � ������ �����,
        //���� ����� ����� ������ �������, � ����� ���.
        private void Gather()
        {
            AI.StopMoving();
            for (int i = 0; i < BodyGuards.Length; i++)
            {
                if (BodyGuards[i] != null && BodyGuards[i].HitPoint > 0)
                {
                    BodyGuards[i].StopMoving();
                    BodyGuards[i].MoveTo(Pathfinder.FindPath(BodyGuards[i].Location, AI.Location));
                }
            }
            StrongMove = true;
        }

        //���������� �������������� AI �� ��������� ����
        private void DirectBodyGuards(Point[] path)
        {
            for (int i = 0; i < BodyGuards.Length; i++)
            {
                if (BodyGuards[i] != null && BodyGuards[i].HitPoint > 0)
                {
                    BodyGuards[i].MoveTo(path);
                }
            }
        }

        //������������� ��������������� AI
        private void StopBodyGuards()
        {
            for (int i = 0; i < BodyGuards.Length; i++)
            {
                if (BodyGuards[i] != null && BodyGuards[i].HitPoint > 0)
                {
                    BodyGuards[i].StopMoving();
                }
            }
        }

        //"������������" ������ ������������� � ������� �������������� AI
        void RegisterBodyGuard(BodyGuard bg)
        {
            int tmp = -1;
            for (int i = 0; i < BodyGuards.Length; i++)
            {
                if (BodyGuards[i] == null && tmp == -1)
                    tmp = i;
            }
            if (tmp != -1)
                BodyGuards[tmp] = bg;
            bg.registered = true;
        }

        //���������� ����� ������, � ������ � ������� ���� ���������� ����
        public int GetConvoyNumber()
        {
            for (int i = 0; i < Convoys.Length; i++)
            {
                if (Convoys[i].ConvoyState == ConvoyState.UnderConstruction)
                {
                    return i;
                }
            }
            return -1;
        }

        public int GetConvoyNumberForConvoyWithCollector()
        {
            for (int i = 0; i < CollConvoys.Length; i++)
            {
                if (CollConvoys[i].ConvoyState == ConvoyState.UnderConstruction)
                {
                    return i;
                }
            }
            return -1;
        }

        public int GetConvoyNumberForConvoyWithBigContainer()
        {
            for (int i = 0; i < BigConvoys.Length; i++)
            {
                if (BigConvoys[i].ConvoyState == ConvoyState.UnderConstruction)
                {
                    return i;
                }
            }
            return -1;
        }

        #endregion

        #region ��������������� �������

        private bool CheckBlockers(bool STOPPED)
        {
            if (this.NanoBots.Count >= Utils.NbrMaxBots)
                return false;

            bool ip = false;
            bool ai = false;
            bool possible = true;
            Point target = new Point(-1, -1);
            Point localNextHP = new Point(-1, -1);
            int localNextHPNum = -1;

            localNextHP = GetNextUngettedHP(AI.Location, ref localNextHPNum);

            if (OtherNanoBotsInfo != null)
            {
                foreach (NanoBotInfo botEnemy in OtherNanoBotsInfo)
                {
                    if ((botEnemy.NanoBotType == NanoBotType.NanoAI) && (GeomDist(AI.Location, botEnemy.Location) <= Utils.BlockerStrength))
                    {
                        ai = true;
                        target = botEnemy.Location;
                    }
                }
            }
            if (OtherInjectionPointsInfo != null)
            {
                foreach (InjectionPointInfo ipi in OtherInjectionPointsInfo)
                {
                    if (GeomDist(AI.Location, ipi.Location) <= Utils.BlockerStrength)
                    {
                        ip = true;
                        target = ipi.Location;
                    }
                }
            }
            if ((!ai) && (!ip))
                return false;

            foreach (NanoBot bot in NanoBots)
            {
                if ((((bot is NanoNeedle) || (bot is NanoBlocker)) || (bot is NanoNeuroControler)) && (bot.Location == AI.Location))
                    possible = false;
                if ((bot is NanoBlocker) && (GeomDist(bot.Location, target) <= Utils.BlockerStrength))
                    return false;
            }
            if (OtherNanoBotsInfo != null)
            {
                foreach (NanoBotInfo bot in OtherNanoBotsInfo)
                {
                    if ((((bot.NanoBotType == NanoBotType.NanoNeedle) || (bot.NanoBotType == NanoBotType.NanoBlocker)) || (bot.NanoBotType == NanoBotType.NanoNeuroControler)) && (bot.Location == AI.Location))
                        possible = false;
                }
            }

            if (possible)
            {
                AI.StopMoving();
                if (!STOPPED)
                {
                    StopBodyGuards();
                }
                AI.Build(typeof(Blocker));
                if (!NeedGathering())
                    StrongMove = false;
                return true;
            }

            if (!STOPPED)
            {
                //���������, ��� �� ��� ����� ������������� ������ ���������
                for (int i = 0; i < BodyGuards.Length; i++)
                {
                    if (BodyGuards[i] != null)
                    {
                        if (BodyGuards[i].HitPoint > 0 && BodyGuards[i].State != NanoBotState.WaitingOrders)
                        {
                            //���� ������� �������������, ������� ���-�� ����� (���������),
                            //�� ��� ���, �� �����.
                            return false;
                        }
                    }
                }
                AI.StopMoving();
                StopBodyGuards();
                //������� ���� � ���� � ���� ������ ������� ����
                Point[] path = this.Pathfinder.FindPath(AI.Location, localNextHP);
                Point[] p = new Point[2];
                if (path.Length >= 2)
                {
                    p[0] = path[0];
                    p[1] = path[1];
                }
                else
                {
                    p[0] = AI.Location;
                    if (this.Tissue[AI.Location.X + 1, AI.Location.Y].AreaType != VG.Map.AreaEnum.Bone)
                    {
                        p[1] = new Point(AI.Location.X + 1, AI.Location.Y);
                    }
                    else if (this.Tissue[AI.Location.X - 1, AI.Location.Y].AreaType != VG.Map.AreaEnum.Bone)
                    {
                        p[1] = new Point(AI.Location.X - 1, AI.Location.Y);
                    }
                    else if (this.Tissue[AI.Location.X, AI.Location.Y + 1].AreaType != VG.Map.AreaEnum.Bone)
                    {
                        p[1] = new Point(AI.Location.X, AI.Location.Y + 1);
                    }
                    else if (this.Tissue[AI.Location.X, AI.Location.Y - 1].AreaType != VG.Map.AreaEnum.Bone)
                    {
                        p[1] = new Point(AI.Location.X, AI.Location.Y - 1);
                    }
                    else
                    {
                        return false;
                    }
                }
                DirectBodyGuards(p);
                AI.MoveTo(p);
                StrongMove = true;
                return true;
            }

            return false;
        }

        private void ProceedHP()
        {
            if (ProceededHP < MyHPs.Count)
            {
                int fafa = (int)MyHPs[ProceededHP];
                for (int i = 0; i < AZNPoints.Length; i++)
                {
                    ePathfinder.FindPath(HoshimiPoints[fafa].Location, AZNPoints[i].Location, ref HPtoAPdistances[fafa, i]);
                    HPtoAPdistances[fafa, i] = (int)HPtoAPdistances[fafa, i] * (StepCost(HoshimiPoints[fafa].Location) + StepCost(AZNPoints[i].Location)) / 2;
                }
                ProceededHP++;
            }
        }

        public Point GetTargetOnVector(Point start, Point end, int d)
        {
            Point ret = new Point();
            ret.X = start.X + d * (end.X - start.X) / (int)GeomDist(start, end);
            ret.Y = start.Y + d * (end.Y - start.Y) / (int)GeomDist(start, end);
            return ret;
        }

        //��������� ������� �������������� ���������� ����� 2 �������.
        public double GeomDist(Point p1, Point p2)
        {
            return Math.Sqrt(Math.Pow((double)(p1.X - p2.X), 2.0) + Math.Pow((double)(p1.Y - p2.Y), 2.0));
        }

        //��������� ManhattanDistance ����� ����� �������
        public int ManhattanDistance(Point p1, Point p2)
        {
            return (Math.Abs(p1.X - p2.X) + Math.Abs(p1.Y - p2.Y));
        }

        //���������� ��������� ���� � �������� ����� �����
        private int StepCost(Point p)
        {
            switch (this.Tissue[p.X, p.Y].AreaType)
            {
                case VG.Map.AreaEnum.LowDensity:
                    return 2;
                case VG.Map.AreaEnum.MediumDensity:
                    return 3;
                case VG.Map.AreaEnum.HighDensity:
                    return 4;
                default:
                    return -1;
            }
            /*
            if (this.Tissue[p.X, p.Y].AreaType == VG.Map.AreaEnum.LowDensity)
            {
                return 2;
            }
            else if (this.Tissue[p.X, p.Y].AreaType == VG.Map.AreaEnum.MediumDensity)
            {
                return 3;
            }
            else if (this.Tissue[p.X, p.Y].AreaType == VG.Map.AreaEnum.HighDensity)
            {
                return 4;
            }
            return -1;
            */
        }

        //��������� ����� ���� ����� �������
        private int EstimateMovementTime(Point start, Point end)
        {
            return (int)((StepCost(start) + StepCost(end)) * ManhattanDistance(start, end) / 2);
        }

        #region Estimation under construction...
        /*
        private int EstimateMovementTime(Point start, Point end)
        {
            VG.Map.BloodStream bs;
            int Stotal = 0;
            int Sgood = 0;
            int Sbad = 0;

            //������ ����������� ����
            int dxp = end.X - start.X;
            int dyp = end.Y - start.Y;

            int dxs;
            int dys;

            //���������� ��������������, � ������� ������ �������� stream`� (� "������")
            int R_width = Math.Abs(start.X - end.X);
            int R_heigth = Math.Abs(start.Y - end.Y);
            int R_x = Math.Min(start.X, end.X);
            int R_y = Math.Min(start.Y, end.Y);
            if (R_x >= R_heigth / 4)
            {
                R_x -= (int)(R_heigth / 4);
            }
            else
            {
                R_x = 0;
            }
            if (R_y >= R_width / 4)
            {
                R_y -= (int)(R_width / 4);
            }
            else
            {
                R_y = 0;
            }
            int Rwidth = (int)(R_width + R_heigth * 0.5);
            int Rheigth = (int)(R_heigth + R_width * 0.5);
            Rectangle rect = new Rectangle(R_x, R_y, Rwidth, Rheigth);

            int R_width = Math.Abs(start.X - end.X);
            int R_heigth = Math.Abs(start.Y - end.Y);
            int R_x = Math.Min(start.X, end.X);
            int R_y = Math.Min(start.Y, end.Y);
            Rectangle rect = new Rectangle(R_x, R_y, R_width, R_heigth);

            Stotal = rect.Width * rect.Height;

            for (int i = 0; i < this.Tissue.BloodStreams.Count; i++)
            {
                bs = this.Tissue.BloodStreams[i];
                if (rect.Contains(bs.Rectangle))
                {
                    //���� ������ ����������� ������
                    if (bs.Direction == VG.Map.BloodStreamDirection.NorthSouth)
                    {
                        dxs = 0;
                        dys = 1;
                    }
                    else if (bs.Direction == VG.Map.BloodStreamDirection.SouthNorth)
                    {
                        dxs = 0;
                        dys = -1;
                    }
                    else if (bs.Direction == VG.Map.BloodStreamDirection.WestEst)
                    {
                        dxs = 1;
                        dys = 0;
                    }
                    else if (bs.Direction == VG.Map.BloodStreamDirection.EstWest)
                    {
                        dxs = -1;
                        dys = 0;
                    }
                    else
                    {
                        dxs = 0;
                        dys = 0;
                    }

                    //���� ����� ��������
                    if (dxp * dxs + dyp * dys >= 0)
                    {
                        Sgood = Sgood + bs.Width * bs.Height;
                    }
                    else
                    {
                        Sbad = Sbad + bs.Width * bs.Height;
                    }
                }
            }
            return (int)(((StepCost(start) + StepCost(end)) * 0.5) * ManhattanDistance(start, end) * ((0.5 * Sgood + 2 * Sbad + (Stotal - Sgood - Sbad)) / Stotal));
        }
        */
        
        //��������� ����� �������� �� start �� end
        //!!! ��������������, ��� ����� �� � ������
        /*
        private int EstimateMovementTime(Point start, Point end)
        {
            int PathTime;
            //��������� ����������� - ManhattanDistance ��� �������
            int BestPathTime = (int)((StepCost(start) + StepCost(end)) * ManhattanDistance(start, end) / 2);

            int tmp;
            int tmp1;

            int dxs;
            int dys;
            VG.Map.BloodStream bs;
            Point Point1 = new Point();
            Point Point2 = new Point();
            Point tmpP = new Point();

            //������ ����������� ����
            int dxp = end.X - start.X;
            int dyp = end.Y - start.Y;

            //���������� ��������������, � ������� ������ �������� stream`� (� "������")
            int R_width = Math.Abs(start.X - end.X);
            int R_heigth = Math.Abs(start.Y - end.Y);
            int R_x = Math.Min(start.X, end.X);
            int R_y = Math.Min(start.Y, end.Y);
            if (R_x >= R_heigth / 4)
            {
                R_x -= (int)(R_heigth / 4);
            }
            else
            {
                R_x = 0;
            }
            if (R_y >= R_width / 4)
            {
                R_y -= (int)(R_width / 4);
            }
            else
            {
                R_y = 0;
            }
            int Rwidth = (int)(R_width + R_heigth * 0.5);
            int Rheigth = (int)(R_heigth + R_width * 0.5);
            Rectangle rect = new Rectangle(R_x, R_y, Rwidth, Rheigth);

            //��������� ��� stream`�. ���� "����� ��������"
            for (int i = 0; i < this.Tissue.BloodStreams.Count; i++)
            {
                bs = this.Tissue.BloodStreams[i];
                //���� ����� ������������ � ����� ���������������
                if (rect.Contains(bs.Location)
                    || rect.Contains(bs.Location.X + bs.Width, bs.Location.Y)
                    || rect.Contains(bs.Location.X, bs.Location.Y + bs.Height)
                    || rect.Contains(bs.Location.X + bs.Width, bs.Location.Y + bs.Height))
                {
                    //���� ������ ����������� ������
                    if (bs.Direction == VG.Map.BloodStreamDirection.NorthSouth)
                    {
                        dxs = 0;
                        dys = 1;
                    }
                    else if (bs.Direction == VG.Map.BloodStreamDirection.SouthNorth)
                    {
                        dxs = 0;
                        dys = -1;
                    }
                    else if (bs.Direction == VG.Map.BloodStreamDirection.WestEst)
                    {
                        dxs = 1;
                        dys = 0;
                    }
                    else if (bs.Direction == VG.Map.BloodStreamDirection.EstWest)
                    {
                        dxs = -1;
                        dys = 0;
                    }
                    else
                    {
                        dxs = 0;
                        dys = 0;
                    }

                    //���� ����� ��������
                    if (dxp * dxs + dyp * dys >= 0)
                    {
                        //���� ���� �� ������ �� ������
                        //������� �� ���������
                        if ((start.X >= bs.Location.X) && (start.X <= bs.Location.X + bs.Width))
                        {
                            if (start.Y >= bs.Location.Y)
                            {
                                Point1.X = start.X;
                                Point1.Y = bs.Location.Y + bs.Height;
                            }
                            else
                            {
                                Point1.X = start.X;
                                Point1.Y = bs.Location.Y;
                            }
                        }
                        //������� �� �����������
                        else if ((start.Y >= bs.Location.Y) && (start.Y <= bs.Location.Y + bs.Height))
                        {
                            if (start.X >= bs.Location.X)
                            {
                                Point1.Y = start.Y;
                                Point1.X = bs.Location.X + bs.Width;
                            }
                            else
                            {
                                Point1.Y = start.Y;
                                Point1.X = bs.Location.X;
                            }
                        }
                        //������� �� �����
                        else
                        {
                            tmpP = bs.Location;
                            Point1 = tmpP;
                            tmp = ManhattanDistance(start, tmpP);
                            tmpP.X += bs.Width;
                            if ((tmp1 = ManhattanDistance(start, tmpP)) < tmp)
                            {
                                Point1 = tmpP;
                                tmp = tmp1;
                            }
                            tmpP.Y += bs.Height;
                            if ((tmp1 = ManhattanDistance(start, tmpP)) < tmp)
                            {
                                Point1 = tmpP;
                                tmp = tmp1;
                            }
                            tmpP.X -= bs.Width;
                            if ((tmp1 = ManhattanDistance(start, tmpP)) < tmp)
                            {
                                Point1 = tmpP;
                            }
                        }

                        //���� ���� �� ������ �� ������
                        //������� �� ���������
                        if ((end.X >= bs.Location.X) && (end.X <= bs.Location.X + bs.Width))
                        {
                            if (end.Y >= bs.Location.Y)
                            {
                                Point2.X = end.X;
                                Point2.Y = bs.Location.Y + bs.Height;
                            }
                            else
                            {
                                Point2.X = end.X;
                                Point2.Y = bs.Location.Y;
                            }
                        }
                        //������� �� �����������
                        else if ((end.Y >= bs.Location.Y) && (end.Y <= bs.Location.Y + bs.Height))
                        {
                            if (end.X >= bs.Location.X)
                            {
                                Point2.Y = end.Y;
                                Point2.X = bs.Location.X + bs.Width;
                            }
                            else
                            {
                                Point2.Y = end.Y;
                                Point2.X = bs.Location.X;
                            }
                        }
                        //������� �� �����
                        else
                        {
                            tmpP = bs.Location;
                            Point2 = tmpP;
                            tmp = ManhattanDistance(end, tmpP);
                            tmpP.X += bs.Width;
                            if ((tmp1 = ManhattanDistance(end, tmpP)) < tmp)
                            {
                                Point2 = tmpP;
                                tmp = tmp1;
                            }
                            tmpP.Y += bs.Height;
                            if ((tmp1 = ManhattanDistance(end, tmpP)) < tmp)
                            {
                                Point2 = tmpP;
                                tmp = tmp1;
                            }
                            tmpP.X -= bs.Width;
                            if ((tmp1 = ManhattanDistance(end, tmpP)) < tmp)
                            {
                                Point2 = tmpP;
                            }
                        }
                        PathTime = (int)(((StepCost(start) + StepCost(Point1)) * (ManhattanDistance(start, Point1) / 2)))
                            + (int)((StepCost(Point1) + StepCost(Point2)) / 4) * ManhattanDistance(Point1, Point2)
                            + (int)(((StepCost(Point2) + StepCost(end)) * (ManhattanDistance(Point2, end) / 2)));
                        if (PathTime < BestPathTime)
                            BestPathTime = PathTime;
                    }
                }
            }

            return BestPathTime;
        }
        */
        #endregion

        #endregion

        #region ������� ��������� ������ � ����������� �� up-to-date

        //C�������� � ������������� ��� ������ �� ��������������� ��������
        private void ReadAllMissions()
        {
            List<VG.Mission.BaseObjective> mission = this.Mission.Objectives;
            int count = 0;
            int tmp = 0;

            //��������� ��� ������
            for (int i = 0; i < mission.Count; i++)
            {
                if (mission[i].ID == 0)
                {
                    //AI Alive
                    BattleExpected = true;
                }
                else if (mission[i].ID == 1)
                {
                    //Navigation
                    VG.Mission.NavigationObjective navObj = (VG.Mission.NavigationObjective)mission[i];
                    if (NavigationPoints == null)
                    {
                        tmp = 0;
                        NavigationPoints = new NavigationPointInfo[navObj.NavPoints.Count];
                    }
                    else
                    {
                        NavigationPointInfo[] fafa = NavigationPoints;
                        tmp = fafa.Length;
                        NavigationPoints = new NavigationPointInfo[fafa.Length + navObj.NavPoints.Count];
                        fafa.CopyTo(NavigationPoints, 0);
                    }
                    for (int j = 0; j < navObj.NavPoints.Count; j++)
                    {
                        NavigationPoints[tmp + j].Location = navObj.NavPoints[j].Location;
                        NavigationPoints[tmp + j].StartTurn = navObj.NavPoints[j].StartTurn;
                        NavigationPoints[tmp + j].EndTurn = navObj.NavPoints[j].EndTurn;
                        NavigationPoints[tmp + j].Navigators = 0;
                        NavigationPoints[tmp + j].Complete = false;
                        NavigationPoints[tmp + j].BotType = navObj.NanoBotType;
                        NavigationPoints[tmp + j].Stock = navObj.NavPoints[j].Stock;
                    }
                }
                else if (mission[i].ID == 2)
                {
                    //NeuroControler
                }
                else if (mission[i].ID == 3)
                {
                    //Score
                    count++;
                }
            }
            if (count != 0)
            {
                tmp = 0;
                ScoreObjectives = new ScoreInfo[count];
                for (int i = 0; i < mission.Count; i++)
                {
                    //Score
                    if (mission[i].ID == 3)
                    {
                        ScoreObjectives[tmp].Score = ((VG.Mission.ScoreObjective)mission[i]).Score;
                        ScoreObjectives[tmp].Turn = ((VG.Mission.ScoreObjective)mission[i]).ScoreTurn;
                        tmp++;
                    }
                }
            }
        }

        //��������� ��� HP � AP
        private void ReadHPsAndAPs()
        {
            //��������� ��� HP � AP
            VG.Map.EntityCollection Hps = this.Tissue.get_EntitiesByType(VG.Map.EntityEnum.HoshimiPoint);
            HoshimiPoints = new HoshimiPointInfo[Hps.Count];
            for (int i = 0; i < Hps.Count; i++)
            {
                HoshimiPoints[i].Location.X = Hps[i].X;
                HoshimiPoints[i].Location.Y = Hps[i].Y;
                HoshimiPoints[i].Full = 0;
                HoshimiPoints[i].Needle = 0;
                HoshimiPoints[i].InPast = 0;
                HoshimiPoints[i].Peasants = 0;
            }
            VG.Map.EntityCollection Aps = this.Tissue.get_EntitiesByType(VG.Map.EntityEnum.AZN);
            AZNPoints = new AZNPointInfo[Aps.Count];
            for (int i = 0; i < Aps.Count; i++)
            {
                AZNPoints[i].Location.X = Aps[i].X;
                AZNPoints[i].Location.Y = Aps[i].Y;
            }
        }

        //��������� ��� ������ �����
        void UpdateData()
        {
            int i = 0;
            int flag = 0;
            BattlePointInfo bp = new BattlePointInfo();
            AtackPointInfo ap = new AtackPointInfo();
            //���������� ������ �� ���� HoshimiPoints
            for (i = 0; i < HoshimiPoints.Length; i++)
            {
                HoshimiPoints[i].Convoys = 0;
                HoshimiPoints[i].Peasants = 0;
                HoshimiPoints[i].Full = 0;
                if (HoshimiPoints[i].Needle == 1)
                {
                    HoshimiPoints[i].Needle = 0;
                }
            }
            //���������� ������ �� ���� NavigationPoints
            if (NavigationPoints != null)
            {
                for (i = 0; i < NavigationPoints.Length; i++)
                {
                    NavigationPoints[i].Navigators = 0;
                }
            }
            //���������� ������ �� ���� BTargets
            for (i = 0; i < BTargets.Count; i++)
            {
                bp = BTargets[i];
                bp.Covered = 0;
                BTargets.RemoveAt(i);
                BTargets.Insert(i, bp);
            }
            //���������� ������ �� ���� ATargets
            for (i = 0; i < ATargets.Count; i++)
            {
                ap = ATargets[i];
                ap.Exist = 0;
                ATargets.RemoveAt(i);
                ATargets.Insert(i, ap);
            }
            //���������� ������ �� ���� �������
            for (i = 0; i < Convoys.Length; i++)
            {
                if (Convoys[i].HNumber != -1)
                {
                    if (Convoys[i].IsNavigating)
                    {
                        NavigationPoints[Convoys[i].HNumber].Navigators++;
                    }
                    else
                    {
                        HoshimiPoints[Convoys[i].HNumber].Convoys++;
                    }
                }
                Convoys[i].Containers = 0;
                Convoys[i].Defenders = 0;
            }
            //���������� ������ �� ���� �������
            if (CollConvoys != null)
            {
                for (i = 0; i < CollConvoys.Length; i++)
                {
                    if (CollConvoys[i].HNumber >= 0)
                        NavigationPoints[CollConvoys[i].HNumber].Navigators++;

                    CollConvoys[i].Containers = 0;
                    CollConvoys[i].Defenders = 0;
                }
            }
            //���������� ������ �� ���� �������
            if (BigConvoys != null)
            {
                for (i = 0; i < BigConvoys.Length; i++)
                {
                    if (BigConvoys[i].HNumber >= 0)
                        NavigationPoints[BigConvoys[i].HNumber].Navigators++;

                    BigConvoys[i].Containers = 0;
                    BigConvoys[i].Defenders = 0;
                }
            }

            //�������� ����� Needle`�
            if (OtherNanoBotsInfo != null)
            {
                foreach (NanoBotInfo bot in OtherNanoBotsInfo)
                {
                    if ((bot.NanoBotType == NanoBotType.NanoNeedle) || (bot.NanoBotType == NanoBotType.NanoBlocker))
                    {
                        for (i = 0; i < HoshimiPoints.Length; i++)
                        {
                            if (bot.Location == HoshimiPoints[i].Location)
                            {
                                HoshimiPoints[i].Needle = 2;
                            }
                        }
                    }
                }
            }

            //�������� ������ ��:
            foreach(NanoBot bot in NanoBots)
            {
                //NavigationPoints
                if((bot is Navigator)&&(((Navigator)bot).NPNumber >= 0))
                {
                    NavigationPoints[((Navigator)bot).NPNumber].Navigators++;
                }
                if ((bot is Doctor) && (((Doctor)bot).NPNumber >= 0))
                {
                    NavigationPoints[((Doctor)bot).NPNumber].Navigators++;
                }
                //HoshimiPoints (collectors & containers)
                if ((bot is Container)&&(((Container)bot).HPNumber >= 0))
                {
                    HoshimiPoints[((Container)bot).HPNumber].Peasants++;
                }
                //HoshimiPoints (Needles)
                if ((bot is Needle) && (((Needle)bot).HPNumber >= 0))
                {
                    HoshimiPoints[((Needle)bot).HPNumber].Needle = 1;
                    HoshimiPoints[((Needle)bot).HPNumber].InPast = 1;
                    if (bot.Stock == bot.ContainerCapacity)
                    {
                        HoshimiPoints[((Needle)bot).HPNumber].Full = 1;
                    }
                }
                //BTargets
                if ((bot is Protector) && (((Protector)bot).Target.X != -1))
                {
                    for (i = 0; i < BTargets.Count; i++)
                    {
                        if (BTargets[i].Location == ((Protector)bot).Target)
                        {
                            bp = BTargets[i];
                            bp.Covered = 1;
                            BTargets.RemoveAt(i);
                            BTargets.Insert(i, bp);
                        }
                    }
                }
                //ATargets
                if ((bot is Atacker) && (((Atacker)bot).Target.X != -1))
                {
                    flag = 0;
                    for (i = 0; i < ATargets.Count; i++)
                    {
                        if (ATargets[i].Location == ((Atacker)bot).Target)
                        {
                            ap = ATargets[i];
                            ap.Exist++;
                            ATargets.RemoveAt(i);
                            ATargets.Insert(i, ap);
                            flag = 1;
                        }
                        //���� ��� ����� �� ����, �� ��� ����������, � � ���� ������ �� ������
                        if (ATargets[i].Location == ((Atacker)bot).Location)
                        {
                            ATargets.RemoveAt(i);
                        }
                    }
                    //���� � ���� ���� ����, �� � ��� � ������, �� � ���� ��� ����
                    if (flag == 0)
                    {
                        ((Atacker)bot).Target.X = -1;
                        ((Atacker)bot).Target.Y = -1;
                        ((Atacker)bot).StopMoving();
                    }
                }
                //�������
                if (bot is ConvoyContainer)
                {
                    if (((ConvoyContainer)bot).ConvoyNumber == -1)
                    {
                        ((ConvoyContainer)bot).SetConvoyNumber(this);
                    }
                    else
                    {
                        Convoys[((ConvoyContainer)bot).ConvoyNumber].Containers++;
                    }
                }
                //� ��� ��� �������
                if (bot is ConvoyDefender)
                {
                    if (((ConvoyDefender)bot).ConvoyNumber == -1)
                    {
                        ((ConvoyDefender)bot).SetConvoyNumber(this);
                    }
                    else
                    {
                        Convoys[((ConvoyDefender)bot).ConvoyNumber].Defenders++;
                    }
                }

                //�������
                if (bot is ConvoyCollector)
                {
                    if (((ConvoyCollector)bot).ConvoyNumber == -1)
                    {
                        ((ConvoyCollector)bot).SetConvoyNumber(this);
                    }
                    else
                    {
                        CollConvoys[((ConvoyCollector)bot).ConvoyNumber].Containers++;
                    }
                }
                //� ��� ��� �������
                if (bot is ConvoyDefender2)
                {
                    if (((ConvoyDefender2)bot).ConvoyNumber == -1)
                    {
                        ((ConvoyDefender2)bot).SetConvoyNumber(this);
                    }
                    else
                    {
                        CollConvoys[((ConvoyDefender2)bot).ConvoyNumber].Defenders++;
                    }
                }

                //�������
                if (bot is BigConvoyContainer)
                {
                    if (((BigConvoyContainer)bot).ConvoyNumber == -1)
                    {
                        ((BigConvoyContainer)bot).SetConvoyNumber(this);
                    }
                    else
                    {
                        BigConvoys[((BigConvoyContainer)bot).ConvoyNumber].Containers++;
                    }
                }
                //� ��� ��� �������
                if (bot is ConvoyDefender3)
                {
                    if (((ConvoyDefender3)bot).ConvoyNumber == -1)
                    {
                        ((ConvoyDefender3)bot).SetConvoyNumber(this);
                    }
                    else
                    {
                        BigConvoys[((ConvoyDefender3)bot).ConvoyNumber].Defenders++;
                    }
                }
            }
        }

        #region ������� ��� ���������� ������ �� ������
        
        //������� ������ � ��� ������, ���� ������ ��������� ����� ����
        //����� ����� ������������ UpdateData - ������� ������

        //��������� ������ �� HoshimiPoints
        void UpdateHoshimiPointsData()
        {
            int i = 0;
            //���������� ������ �� ���� HoshimiPoints
            for (i = 0; i < HoshimiPoints.Length; i++)
            {
                HoshimiPoints[i].Convoys = 0;
                HoshimiPoints[i].Peasants = 0;
                HoshimiPoints[i].Full = 0;
                if (HoshimiPoints[i].Needle == 1)
                {
                    HoshimiPoints[i].Needle = 0;
                }
            }

            //�������� ����� Needle`�
            if (OtherNanoBotsInfo != null)
            {
                foreach (NanoBotInfo bot in OtherNanoBotsInfo)
                {
                    if (bot.NanoBotType == NanoBotType.NanoNeedle)
                    {
                        for (i = 0; i < HoshimiPoints.Length; i++)
                        {
                            if (bot.Location == HoshimiPoints[i].Location)
                            {
                                HoshimiPoints[i].Needle = 2;
                            }
                        }
                    }
                }
            }

            //�������� ������ ��:
            foreach (NanoBot bot in NanoBots)
            {
                //HoshimiPoints (collectors & containers)
                if ((bot is Container) && (((Container)bot).HPNumber >= 0))
                {
                    HoshimiPoints[((Container)bot).HPNumber].Peasants++;
                }
                //HoshimiPoints (Needles)
                if ((bot is Needle) && (((Needle)bot).HPNumber >= 0))
                {
                    HoshimiPoints[((Needle)bot).HPNumber].Needle = 1;
                    HoshimiPoints[((Needle)bot).HPNumber].InPast = 1;
                    if (bot.Stock == bot.ContainerCapacity)
                    {
                        HoshimiPoints[((Needle)bot).HPNumber].Full = 1;
                    }
                }
            }
            //�������
            for (i = 0; i < Convoys.Length; i++)
            {
                if (Convoys[i].HNumber != -1)
                {
                    HoshimiPoints[Convoys[i].HNumber].Convoys++;
                }
            }
        }

        //��������� ������ �� NavigationPoints
        void UpdateNavigationPointsData()
        {
            int i = 0;
            //���������� ������ �� ���� NavigationPoints
            for (i = 0; i < NavigationPoints.Length; i++)
            {
                NavigationPoints[i].Navigators = 0;
            }

            //�������� ������ ��:
            foreach (NanoBot bot in NanoBots)
            {
                //NavigationPoints
                if ((bot is Navigator) && (((Navigator)bot).NPNumber >= 0))
                {
                    NavigationPoints[((Navigator)bot).NPNumber].Navigators++;
                }
                if ((bot is Doctor) && (((Doctor)bot).NPNumber >= 0))
                {
                    NavigationPoints[((Doctor)bot).NPNumber].Navigators++;
                }
            }
        }

        //��������� ������ �� BTargets
        void UpdateBTargetsData()
        {
            int i = 0;
            BattlePointInfo bp = new BattlePointInfo();
            //���������� ������ �� ���� BTargets
            for (i = 0; i < BTargets.Count; i++)
            {
                bp = BTargets[i];
                bp.Covered = 0;
                BTargets.RemoveAt(i);
                BTargets.Insert(i, bp);
            }

            //�������� ������ ��:
            foreach (NanoBot bot in NanoBots)
            {
                //BTargets
                if ((bot is Protector) && (((Protector)bot).Target.X != -1))
                {
                    for (i = 0; i < BTargets.Count; i++)
                    {
                        if (BTargets[i].Location == ((Protector)bot).Target)
                        {
                            bp = BTargets[i];
                            bp.Covered = 1;
                            BTargets.RemoveAt(i);
                            BTargets.Insert(i, bp);
                        }
                    }
                }
            }
        }

        //��������� ������ �� BTargets
        void UpdateATargetsData()
        {
            int flag = 0;
            int i = 0;
            AtackPointInfo ap = new AtackPointInfo();
            //���������� ������ �� ���� BTargets
            for (i = 0; i < ATargets.Count; i++)
            {
                ap = ATargets[i];
                ap.Exist = 0;
                ATargets.RemoveAt(i);
                ATargets.Insert(i, ap);
            }

            //�������� ������ ��:
            foreach (NanoBot bot in NanoBots)
            {
                //ATargets
                if ((bot is Atacker) && (((Atacker)bot).Target.X != -1))
                {
                    flag = 0;
                    for (i = 0; i < ATargets.Count; i++)
                    {
                        if (ATargets[i].Location == ((Atacker)bot).Target)
                        {
                            ap = ATargets[i];
                            ap.Exist++;
                            ATargets.RemoveAt(i);
                            ATargets.Insert(i, ap);
                            flag = 1;
                        }
                        //���� ��� ����� �� ����, �� ��� ����������, � � ���� ������ �� ������
                        if (ATargets[i].Location == ((Atacker)bot).Location)
                        {
                            ATargets.RemoveAt(i);
                        }
                    }
                    //���� � ���� ���� ����, �� � ��� � ������, �� � ���� ��� ����
                    if (flag == 0)
                    {
                        ((Atacker)bot).Target.X = -1;
                        ((Atacker)bot).Target.Y = -1;
                        ((Atacker)bot).StopMoving();
                    }
                }
            }
        }

        //��������� ������ �� ������� �������
        void UpdateConvoysData()
        {
            int i = 0;
            //���������� ������ �� ���� �������
            for (i = 0; i < Convoys.Length; i++)
            {
                Convoys[i].Containers = 0;
                Convoys[i].Defenders = 0;
            }

            //�������� ������ ��:
            foreach (NanoBot bot in NanoBots)
            {
                //�������
                if (bot is ConvoyContainer)
                {
                    if (((ConvoyContainer)bot).ConvoyNumber == -1)
                    {
                        ((ConvoyContainer)bot).SetConvoyNumber(this);
                    }
                    else
                    {
                        Convoys[((ConvoyContainer)bot).ConvoyNumber].Containers++;
                    }
                }
                //� ��� ��� �������
                if (bot is ConvoyDefender)
                {
                    if (((ConvoyDefender)bot).ConvoyNumber == -1)
                    {
                        ((ConvoyDefender)bot).SetConvoyNumber(this);
                    }
                    else
                    {
                        Convoys[((ConvoyDefender)bot).ConvoyNumber].Defenders++;
                    }
                }
            }
        }

        #endregion

        //�������� �� �������������, ����������� � �������������� �������
        //AHTUNG!!! ����� ������� UpdateConvoysStates MUST ������� UpdateConvoysData ��� UpdateData
        void UpdateConvoysStates()
        {
            int i;
            int tmp;
            for (i = 0; i < Convoys.Length; i++)
            {
                //���� ������ ��� ������, �� ������ ����������� � �� ���, ������ �� ����.
                if ((Convoys[i].ConvoyState != ConvoyState.UnderConstruction)
                    && (Convoys[i].Containers == 0))
                {
                    //�������� ���������
                    Convoys[i].Delete();
                }

                //������ ������, ��������� ��� � ��������� ����������
                if ((Convoys[i].ConvoyState == ConvoyState.UnderConstruction)
                    && (Convoys[i].Containers == 2) && (Convoys[i].Defenders == 1))
                {
                    Convoys[i].ConvoyState = ConvoyState.Waiting;
                }
            }

            //����, ���� �� ��� ����������� ������
            tmp = -1;
            for (i = 0; i < Convoys.Length; i++)
            {
                if (Convoys[i].ConvoyState == ConvoyState.UnderConstruction)
                {
                    tmp = i;
                    break;
                }
            }
            //���� ����, �� �������, ������� ��� �������� ������ ���� ����������� � ����������
            if (tmp != -1)
            {
                CCTB = 2 - Convoys[tmp].Containers;
                CDTB = 1 - Convoys[tmp].Defenders;
            }
            //���� ���, �� �� ������� �� ����
            else
            {
                CCTB = 0;
                CDTB = 0;
            }
        }

        void UpdateCollConvoysStates()
        {
            int i;
            int tmp;
            for (i = 0; i < CollConvoys.Length; i++)
            {
                //���� ������ ��� ������, �� ������ ����������� � �� ���, ������ �� ����.
                if ((CollConvoys[i].ConvoyState != ConvoyState.UnderConstruction)
                    && (CollConvoys[i].ConvoyState != ConvoyState.Deleted)
                    && (CollConvoys[i].Containers == 0))
                {
                    //�������� ���������
                    CollConvoys[i].Delete();
                }

                //������ ������, ��������� ��� � ��������� ����������
                if ((CollConvoys[i].ConvoyState == ConvoyState.UnderConstruction)
                    && (CollConvoys[i].Containers == 1) && (CollConvoys[i].Defenders == 1))
                {
                    CollConvoys[i].ConvoyState = ConvoyState.Waiting;
                }
            }

            //����, ���� �� ��� ����������� ������
            tmp = -1;
            for (i = 0; i < CollConvoys.Length; i++)
            {
                if (CollConvoys[i].ConvoyState == ConvoyState.UnderConstruction)
                {
                    tmp = i;
                    break;
                }
            }
            //���� ����, �� �������, ������� ��� �������� ������ ���� ����������� � ����������
            if (tmp != -1)
            {
                CCTB2 = 1 - CollConvoys[tmp].Containers;
                CDTB2 = 1 - CollConvoys[tmp].Defenders;
            }
            //���� ���, �� �� ������� �� ����
            else
            {
                CCTB2 = 0;
                CDTB2 = 0;
            }
        }

        void UpdateBigConvoysStates()
        {
            int i;
            int tmp;
            for (i = 0; i < BigConvoys.Length; i++)
            {
                //���� ������ ��� ������, �� ������ ����������� � �� ���, ������ �� ����.
                if ((BigConvoys[i].ConvoyState != ConvoyState.UnderConstruction)
                    && (BigConvoys[i].ConvoyState != ConvoyState.Deleted)
                    && (BigConvoys[i].Containers == 0))
                {
                    //�������� ���������
                    BigConvoys[i].Delete();
                }

                //������ ������, ��������� ��� � ��������� ����������
                if ((BigConvoys[i].ConvoyState == ConvoyState.UnderConstruction)
                    && (BigConvoys[i].Containers == 1) && (BigConvoys[i].Defenders == 1))
                {
                    BigConvoys[i].ConvoyState = ConvoyState.Waiting;
                }
            }

            //����, ���� �� ��� ����������� ������
            tmp = -1;
            for (i = 0; i < BigConvoys.Length; i++)
            {
                if (BigConvoys[i].ConvoyState == ConvoyState.UnderConstruction)
                {
                    tmp = i;
                    break;
                }
            }
            //���� ����, �� �������, ������� ��� �������� ������ ���� ����������� � ����������
            if (tmp != -1)
            {
                CCTB3 = 1 - BigConvoys[tmp].Containers;
                CDTB3 = 1 - BigConvoys[tmp].Defenders;
            }
            //���� ���, �� �� ������� �� ����
            else
            {
                CCTB3 = 0;
                CDTB3 = 0;
            }
        }


        #endregion

        #region ������� ����������� ����� ������
        /*
        private void ScanList(int LastPoint, int CurLength, ref int BestNum, ref int TurnsLeft)
        {
            int i,j;
            int tmp;
            if (path.Count == 0)
                for (i = 0; i < HoshimiPoints.Length; i++)
                    used[i] = 0;

            for (i = 0; i < HoshimiPoints.Length; i++)
            {
                if (path.Count == 0)
                    LastPoint = i;
                if (used[i] == 0)
                {
                    if (CurLength + HPtoHPdistances[LastPoint, i] < 1500)//AND!!!
                    {
                        path.Push(i);
                        used[i] = 1;
                        ScanList(i, CurLength + HPtoHPdistances[LastPoint, i], ref BestNum, ref TurnsLeft);
                    }
                    else
                    {
                        if (path.Count > BestNum)
                        {
                            BestNum = path.Count;
                            TurnsLeft = 1500 - CurLength;
                            BestPath = new int[path.Count];
                            path.CopyTo(BestPath, 0);
                        }
                        else if (path.Count == BestNum && (1500 - CurLength) > TurnsLeft)
                        {
                            TurnsLeft = 1500 - CurLength;
                            BestPath = new int[path.Count];
                            path.CopyTo(BestPath, 0);
                        }
                        tmp = path.Pop();
                        used[tmp] = 0;
                        return;
                    }
                }
            }
            if (path.Count > BestNum)
            {
                BestNum = path.Count;
                TurnsLeft = 1500 - CurLength;
                BestPath = new int[path.Count];
                path.CopyTo(BestPath, 0);
            }
            else if (path.Count == BestNum && (1500 - CurLength) > TurnsLeft)
            {
                TurnsLeft = 1500 - CurLength;
                BestPath = new int[path.Count];
                path.CopyTo(BestPath, 0);
            }
            if (path.Count > 0)
            {
                tmp = path.Pop();
                used[tmp] = 0;
            }
            return;
        }
        */
        private Point FinallyChooseInjectionPoint(Point FirstIP, Point FirstHP, Point FirstNP)
        {
            //???
            bool inCircle = false;
            bool in_I_H_Rect = false;
            bool in_I_N_Rect = false;
            bool PierreIsNear;
            int minX1;
            int maxX1;
            int minY1;
            int maxY1;
            int minX2;
            int maxX2;
            int minY2;
            int maxY2;
            //int i;
            double Dist = GeomDist(FirstIP, PierreTeamInjectionPoint);
            inCircle = (Dist < 30);

            minX1 = Math.Min(FirstIP.X, FirstHP.X) - 15;
            maxX1 = Math.Max(FirstIP.X, FirstHP.X) + 15;
            minY1 = Math.Min(FirstIP.Y, FirstHP.Y) - 15;
            maxY1 = Math.Max(FirstIP.Y, FirstHP.Y) + 15;

            in_I_H_Rect = ((PierreTeamInjectionPoint.X < maxX1) && (PierreTeamInjectionPoint.X > minX1)
                && (PierreTeamInjectionPoint.Y < maxY1) && (PierreTeamInjectionPoint.Y > minY1));

            minX2 = Math.Min(FirstIP.X, FirstNP.X) - 15;
            maxX2 = Math.Max(FirstIP.X, FirstNP.X) + 15;
            minY2 = Math.Min(FirstIP.Y, FirstNP.Y) - 15;
            maxY2 = Math.Max(FirstIP.Y, FirstNP.Y) + 15;

            in_I_N_Rect = ((PierreTeamInjectionPoint.X < maxX2) && (PierreTeamInjectionPoint.X > minX2)
                && (PierreTeamInjectionPoint.Y < maxY2) && (PierreTeamInjectionPoint.Y > minY2));

            PierreIsNear = (inCircle || in_I_H_Rect || in_I_N_Rect);
            if (PierreIsNear)
            {
                KillPierre = true;
                /*
                //! Hardcoding!!!
                if ((PierreTeamInjectionPoint.X>FirstIP.X)&&(PierreTeamInjectionPoint.X<FirstNP.X)//!
                    &&(PierreTeamInjectionPoint.Y>FirstNP.Y))//!
                {//!
                    double sin = (FirstIP.Y - PierreTeamInjectionPoint.Y) / Dist;
                    double cos = (FirstIP.X - PierreTeamInjectionPoint.X) / Dist;
                    Point p = new Point();
                    i = 17;
                    while (true)
                    {
                        i++;
                        p.X = (int)(PierreTeamInjectionPoint.X + i * cos);
                        p.Y = (int)(PierreTeamInjectionPoint.Y + i * sin);
                        if (this.Tissue[p.X, p.Y].AreaType != VG.Map.AreaEnum.Bone)
                        {
                            return p;
                        }
                    }
                }//!
                */
            }
            return FirstIP;
        }

        private void AnalizeMap(ref int HPnum, ref int APnum)
        {
            eAStar PathFinder = new eAStar(this.Tissue);
            //used = new int[HoshimiPoints.Length];
            //path = new Stack<int>();
            //BestPath = new Stack<int>();
            //int Num = 0;
            //int Time = 0;
            HPtoHPdistances = new int[HoshimiPoints.Length, HoshimiPoints.Length];
            HPtoAPdistances = new int[HoshimiPoints.Length, AZNPoints.Length];
            for (int i = 0; i < HoshimiPoints.Length; i++)
            {
                HPtoHPdistances[i, i] = 0;
                for (int j = 0; j < i; j++)
                {
                    PathFinder.FindPath(HoshimiPoints[i].Location, HoshimiPoints[j].Location, ref HPtoHPdistances[i, j]);
                    HPtoHPdistances[i, j] = (int)HPtoHPdistances[i, j] * (StepCost(HoshimiPoints[i].Location) + StepCost(HoshimiPoints[j].Location)) / 2;
                    HPtoHPdistances[j, i] = HPtoHPdistances[i, j];
                }
                for (int j = 0; j < AZNPoints.Length; j++)
                {
                    //PathFinder.FindPath(HoshimiPoints[i].Location, AZNPoints[j].Location, ref HPtoAPdistances[i, j]);
                    //HPtoAPdistances[i, j] = (int)HPtoAPdistances[i, j] * (StepCost(HoshimiPoints[i].Location) + StepCost(AZNPoints[j].Location)) / 2;
                    HPtoAPdistances[i, j] = EstimateMovementTime(HoshimiPoints[i].Location, AZNPoints[j].Location);
                }
            }
            //ScanList(-1, 0, ref Num, ref Time);

            List<int>[] Path = new List<int>[HoshimiPoints.Length];
            int[] HPN = new int[HoshimiPoints.Length];
            int[] fafa = new int[HoshimiPoints.Length];
            int[] value = new int[HoshimiPoints.Length];
            int sum = 0;
            int last = 0;
            int tmp = 0;
            int tmpNum1 = 0;
            int tmpNum2 = 0;
            int tmpDist3 = 0;
            int tmpDist4 = 0;
            double alpha = 1;

            for (int i = 0; i < HoshimiPoints.Length; i++)
            {
                value[i] = 0;
            }

            //������� �������� ������������ �����
            if (NavigationPoints != null)
            {
                for (int i = 0; i < HoshimiPoints.Length; i++)
                {
                    for (int j = 0; j < NavigationPoints.Length; j++)
                    {
                        ePathfinder.FindPath(HoshimiPoints[i].Location, NavigationPoints[j].Location, ref tmp);
                        if (tmp + 100 > NavigationPoints[j].EndTurn)
                        {
                            value[i] = -1;
                        }
                    }
                }
            }

            //��������� "��������" ����� ������
            for (int i = 0; i < HoshimiPoints.Length; i++)
            {
                if (value[i] != -1)
                {
                    //��������� ����� ����������� �������
                    Path[i] = new List<int>();
                    Path[i].Clear();
                    for (int j = 0; j < fafa.Length; j++)
                    {
                        fafa[j] = 0;
                    }
                    Path[i].Add(i);
                    HPN[i] = 1;
                    fafa[i] = 1;
                    sum = 0;
                    last = i;
                    //���� ���� ���� (����� - ����-� �������� �������� � AZN
                    while (sum < 1500 * (1 + alpha))
                    {
                        //���� HP, ��������� � ������� HP
                        tmp = 10000;
                        tmpNum1 = -1;
                        for (int j = 0; j < HoshimiPoints.Length; j++)
                        {
                            if ((HPtoHPdistances[last, j] < tmp) && (fafa[j] == 0))
                            {
                                tmp = HPtoHPdistances[last, j];
                                tmpNum1 = j;
                            }
                        }
                        tmpDist3 = tmp;

                        //���� AP, ��������� � ������� HP
                        tmp = 10000;
                        tmpNum2 = -1;
                        for (int j = 0; j < AZNPoints.Length; j++)
                        {
                            if (HPtoAPdistances[last, j] < tmp)
                            {
                                tmp = HPtoAPdistances[last, j];
                                tmpNum2 = j;
                            }
                        }
                        tmpDist4 = tmp;

                        //���� ���-�� �����������, �� ����
                        if ((tmpNum1 == -1) || (tmpNum2 == -1))
                            break;

                        //��������� ����� � ������
                        sum = sum + tmpDist3 + (int)(tmpDist4 * alpha);
                        if (sum < 1500 * (1 + alpha))
                        {
                            fafa[tmpNum1] = 1;
                            HPN[i]++;
                            Path[i].Add(tmpNum1);
                            last = tmpNum1;
                        }
                    }

                    //���� AZN, ��������� � ������
                    tmpDist3 = 10000;
                    for (int j = 0; j < AZNPoints.Length; j++)
                    {
                        if (HPtoAPdistances[i, j] < tmpDist3)
                        {
                            tmpDist3 = HPtoAPdistances[i, j];
                        }
                    }
                    //��������� "��������"
                    value[i] = HPN[i] * 1000 - tmpDist3 * 10 + (int)(1500 * (1 + alpha) - sum);
                }
            }
            /*
            MyHPs.Add(0);
            MyHPs.Add(1);
            MyHPs.Add(2);
            HPnum = 0;
            APnum = 0;
            */

            //������� ������ ����� ������
            tmp = -1;
            tmpNum1 = -1;
            for (int i = 0; i < HoshimiPoints.Length; i++)
            {
                if (value[i] > tmp)
                {
                    tmp = value[i];
                    tmpNum1 = i;
                }
            }
            HPnum = tmpNum1;

            //������� ��������� ����� � AZN
            tmpDist3 = 10000;
            tmpNum1 = -1;
            for (int j = 0; j < AZNPoints.Length; j++)
            {
                if (HPtoAPdistances[HPnum, j] < tmpDist3)
                {
                    tmpDist3 = HPtoAPdistances[HPnum, j];
                    tmpNum1 = j;
                }
            }
            APnum = tmpNum1;
            /*
            for (int i = 0; i < Path[HPnum].Count; i++)
            {
                MyHPs.Add(Path[HPnum][i]);
            }
            */
            //��������� ����� ����������� �������
            for (int j = 0; j < fafa.Length; j++)
            {
                fafa[j] = 0;
            }
            MyHPs.Add(HPnum);
            fafa[HPnum] = 1;
            sum = 0;
            last = HPnum;
            //���� ���� ����
            while (sum < 1500)
            {
                //���� HP, ��������� � ������� HP
                tmp = 10000;
                tmpNum1 = -1;
                for (int j = 0; j < HoshimiPoints.Length; j++)
                {
                    if ((HPtoHPdistances[last, j] < tmp) && (fafa[j] == 0))
                    {
                        tmp = HPtoHPdistances[last, j];
                        tmpNum1 = j;
                    }
                }
                tmpDist3 = tmp;

                //���� ���-�� �����������, �� ����
                if (tmpNum1 == -1)
                    break;

                //��������� ����� � ������
                sum = sum + tmpDist3;
                if (sum < 1500)
                {
                    fafa[tmpNum1] = 1;
                    MyHPs.Add(tmpNum1);
                    last = OptimizePath();
                }
            }
        }

        //������������ MyHPs. ���������� ����� ����� ��������� �����.
        int OptimizePath()
        {
            bool dirty = true;
            int[] Path = (int[])MyHPs.ToArray(typeof(int));
            int i, j, tmp;
            int oldD, newD;

            while (dirty)
            {
                dirty = false;
                for (i = Path.Length - 1; i >= 2; i--)
                {
                    for (j = i - 1; j >= 1; j--)
                    {
                        oldD = HPtoHPdistances[Path[j - 1], Path[j]];
                        newD = HPtoHPdistances[Path[j - 1], Path[i]];

                        if (j != i - 1)
                        {
                            oldD += HPtoHPdistances[Path[i - 1], Path[i]];
                            oldD += HPtoHPdistances[Path[j], Path[j + 1]];
                            newD += HPtoHPdistances[Path[i], Path[j + 1]];
                            newD += HPtoHPdistances[Path[i - 1], Path[j]];
                        }
                        else
                        {
                            oldD += HPtoHPdistances[Path[i - 1], Path[i]];
                            newD += HPtoHPdistances[Path[i], Path[i - 1]];
                        }

                        if (i != Path.Length - 1)
                        {
                            oldD += HPtoHPdistances[Path[i], Path[i + 1]];
                            newD += HPtoHPdistances[Path[j], Path[i + 1]];
                        }


                        if (newD < oldD)
                        {
                            tmp = Path[i];
                            Path[i] = Path[j];
                            Path[j] = tmp;
                            dirty = true;
                            break;
                        }
                    }
                    if (dirty)
                        break;
                }
            }

            MyHPs.Clear();
            MyHPs.InsertRange(0, Path);
            return Path[Path.Length - 1];
        }

        #endregion

        void MyAI_WhatToDoNextEvent()
		{
            //string str = "Turn " + this.CurrentTurn.ToString() + "\n";
            //Debugger.Log(2, "local", str);
            //�������� ����� �����
            int nbProtector = 0;
            int nbAtacker = 0;
			int nbCollector = 0;
			int nbContainer = 0;
			int nbNeedle = 0;
            int nbExplorer = 0;
            int nbNavigator = 0;
            int nbBodyGuards = 0;
            int nbObservers = 0;
            int nbDoctors = 0;
            //���� �� ������� ������
            bool BuildConvoy = false;

            //��� �� ������ ������������ �� ��������������
            bool STOPPED = false;

            bool Gathering = false;

            NBPROTECTORTOBUILD = BTargets.Count;
            //if (KillPierre)
            //{
            //    NBATACKERTOBUILD = 4;
            //}
            //NBCONTAINERTOBUILD = MyHPs.Count;
            //if (KillPierre)
            //{
            //    NBNAVIGATORTOBUILD = NavigationPoints.Length * 2;
            //}
            //else
            //{
            //if (NavigationPoints != null)
            //{
            //   NBNAVIGATORTOBUILD = NavigationPoints.Length;
            //}
            //else
            //{
            //    NBNAVIGATORTOBUILD = 0;
            //}
            //}

            ProceedHP();
            //��������� ��� ������
            this.UpdateData();
            this.UpdateConvoysStates();
            if (CollConvoys != null)
            {
                this.UpdateCollConvoysStates();
                foreach (ConvoyWithCollector c in this.CollConvoys)
                {
                    if (c.ConvoyState == ConvoyState.UnderConstruction)
                    {
                        BuildConvoy = true;
                    }
                    else
                    {
                        c.Action(this);
                    }
                }
            }
            if (BigConvoys != null)
            {
                this.UpdateBigConvoysStates();
                foreach (ConvoyWithBigContainer c in this.BigConvoys)
                {
                    if (c.ConvoyState == ConvoyState.UnderConstruction)
                    {
                        BuildConvoy = true;
                    }
                    else
                    {
                        c.Action(this);
                    }
                }
            }

            //����� ��������. ������ �������, ���� �� ����-�� �� ��� �����������.
            foreach (Convoy c in this.Convoys)
            {
                if (c.ConvoyState == ConvoyState.UnderConstruction)
                {
                    BuildConvoy = true;
                }
                else
                {
                    c.Action(this);
                }
            }

            //����� ���������� ������
			foreach (NanoBot bot in this.NanoBots)
			{
				if (bot is Protector)
				{
					nbProtector++;
					((Protector)bot).Action(this);
                    
				}
                if (bot is Atacker)
                {
                    nbAtacker++;
                    ((Atacker)bot).Action(this);

                }
				if (bot is Collector)
				{
					nbCollector++;
					((Collector)bot).Action(this);
				}
				if (bot is Container)
				{
					nbContainer++;
					((Container)bot).Action(this);
				}
                if (bot is Explorer)
                {
                    nbExplorer++;
                    ((Explorer)bot).Action(this);
                }
                if (bot is Navigator)
                {
                    nbNavigator++;
                    ((Navigator)bot).Action(this);
                }
                if (bot is Doctor)
                {
                    nbDoctors++;
                    ((Doctor)bot).Action(this);
                }
				if (bot is Needle)
				{
					nbNeedle++;
                    ((Needle)bot).Action(this);
				}
                if (bot is BodyGuard)
                {
                    if (((BodyGuard)bot).registered == false)
                    {
                        RegisterBodyGuard((BodyGuard)bot);
                    }
                    nbBodyGuards++;
                    if (((BodyGuard)bot).Alarm(this))
                    {
                        STOPPED = true;
                    }
                }
                if (bot is Observer)
                {
                    nbObservers++;
                    ((Observer)bot).Action(this, NextHPNum);
                }
                if (bot is Blocker)
                {
                    ((Blocker)bot).Action(this);
                }
			}

            if (!STOPPED && StrongMove)
            {
                if (AI.State == NanoBotState.Moving)
                    return;
                for (int i = 0; i < BodyGuards.Length; i++)
                {
                    if (BodyGuards[i] != null && BodyGuards[i].HitPoint > 0 && BodyGuards[i].State == NanoBotState.Moving)
                        Gathering = true;
                }
            }

            if (CheckBlockers(STOPPED))
                return;

            if (this.NeedGathering())
            {
                if (!Gathering)
                {
                    this.Gather();
                }
                STOPPED = true;
            }

            if (!STOPPED)
                StrongMove = false;

            //���� ���� ������� ����� ��� ������ ��� ��� ������ "������" �� ��������������,
            if ((nbCollector < NBCOLLECTORTOBUILD) || (nbContainer < NBCONTAINERTOBUILD) 
                || (nbExplorer < NBEXPLORERTOBUILD) || (nbNavigator < NBNAVIGATORTOBUILD) 
                || (nbProtector < NBPROTECTORTOBUILD) || (nbAtacker < NBATACKERTOBUILD)
                || (nbObservers < OBSERVERSTOBUILD) || (nbDoctors < NBDOCTORSTOBUILD)
                || BuildConvoy || STOPPED)
            {
                //�� ���������������.
                AI.StopMoving();
                //���� �� ������������ ����, �� ������������� ��������������
                if (!STOPPED)
                {
                    StopBodyGuards();
                }
            }

            //���� ��� � �������� �����
            if (NextHPNum >= 0)
            {
                //� ��� ������
                if (HoshimiPoints[NextHPNum].Needle == 2)
                {
                    //�� ���������������
                    AI.StopMoving();
                    StopBodyGuards();
                }
            }

			if (this.AI.State == NanoBotState.WaitingOrders)
			 {
                //������� ��������� HP
                NextHP = GetNextUngettedHP(AI.Location, ref NextHPNum);
                //���� ����� �� ������� �����, �� ������ Needle
                if (NextHP == AI.Location)
                {
                    bool can = true;
                    foreach (NanoBot bot in NanoBots)
                    {
                        if ((((bot is NanoNeedle) || (bot is NanoBlocker)) || (bot is NanoNeuroControler)) && (bot.Location == AI.Location))
                        {
                            can = false;
                            for (int i = 0; i < HoshimiPoints.Length; i++)
                            {
                                if (HoshimiPoints[i].Location == AI.Location)
                                {
                                    HoshimiPoints[i].Needle = 2;
                                }
                            }
                        }
                    }
                    if (can)
                    {
                        if (this.NanoBots.Count < Utils.NbrMaxBots)
                        {
                            for (int i = 0; i < HoshimiPoints.Length; i++)
                            {
                                if (HoshimiPoints[i].Location == AI.Location)
                                {
                                    HoshimiPoints[i].Needle = 1;
                                }
                            }
                            AI.Build(typeof(Needle));
                            return;
                        }
                    }
                }

                //������ �����
                if (this.NanoBots.Count < Utils.NbrMaxBots)
                {
                    //NanoAtacker
                    if (nbAtacker < NBATACKERTOBUILD)
                    {
                        AI.Build(typeof(Atacker));
                        return;
                    }
                    //NanoNavigator
                    if (nbNavigator < NBNAVIGATORTOBUILD)
                    {
                        AI.Build(typeof(Navigator));
                        return;
                    }
                    //NanoDoctor
                    if (nbDoctors < NBDOCTORSTOBUILD)
                    {
                        AI.Build(typeof(Doctor));
                        return;
                    }
                    //NanoProtector
                    if (nbProtector < NBPROTECTORTOBUILD)
                    {
                        AI.Build(typeof(Protector));
                        return;
                    }
                    //NanoContainer
                    if (nbContainer < NBCONTAINERTOBUILD)
                    {
                        AI.Build(typeof(Container));
                        return;
                    }
                    //ConvoyDefender
                    if (CDTB3 > 0)
                    {
                        AI.Build(typeof(ConvoyDefender3));
                        return;
                    }
                    //ConvoyContainer
                    if (CCTB3 > 0)
                    {
                        AI.Build(typeof(BigConvoyContainer));
                        return;
                    }
                    //ConvoyDefender
                    if (CDTB2 > 0)
                    {
                        AI.Build(typeof(ConvoyDefender2));
                        return;
                    }
                    //ConvoyContainer
                    if (CCTB2 > 0)
                    {
                        AI.Build(typeof(ConvoyCollector));
                        return;
                    }
                    //ConvoyDefender
                    if (CDTB > 0)
                    {
                        AI.Build(typeof(ConvoyDefender));
                        return;
                    }
                    //ConvoyContainer
                    if (CCTB > 0)
                    {
                        AI.Build(typeof(ConvoyContainer));
                        return;
                    }
                    //NanoCollector
                    if (nbCollector < NBCOLLECTORTOBUILD)
                    {
                        AI.Build(typeof(Collector));
                        return;
                    }
                    //NanoExplorer
                    if (nbExplorer < NBEXPLORERTOBUILD)
                    {
                        AI.Build(typeof(Explorer));
                        return;
                    }
                    //Bodyguard
                    if (NBBODYGUARSTOBUILD > 0)
                    {
                        NBBODYGUARSTOBUILD--;
                        AI.Build(typeof(BodyGuard));
                        return;
                    }
                    //Observer
                    if (nbObservers < OBSERVERSTOBUILD)
                    {
                        AI.Build(typeof(Observer));
                        return;
                    }
                }


                //�������� AI

                //���� �� �� ������� ����� � ������� ������ �� ����
                if (!STOPPED)
                {
                    //���������, ��� �� ��� ����� ������������� ������ ���������
                    for (int i = 0; i < BodyGuards.Length; i++)
                    {
                        if (BodyGuards[i] != null)
                        {
                            if (BodyGuards[i].HitPoint != 0 && BodyGuards[i].State != NanoBotState.WaitingOrders)
                            {
                                //���� ������� �������������, ������� ���-�� ����� (���������),
                                //�� ��� ���, �� �����.
                                return;
                            }
                        }
                    }
                    //������� ���� � ���� � ���� ������ ������� ����
                    Point[] path = this.Pathfinder.FindPath(AI.Location, NextHP);
                    DirectBodyGuards(path);
                    AI.MoveTo(path);
                    return;
                }
			}
		}
        
		void MyAI_ChooseInjectionPointEvent()
		{
            int HealConvoysWithContainers = 0;
            int BigHealConvoys = 0;
            int HealConvoysWithCollectors = 0;
            int count = 0;
            int tmp = 0;
            int tmp1 = 0;
            int FHPnum = -1;
            int FAPnum = -1;
            Point p = new Point();
            NavigationPointInfo FirstNavPoint = new NavigationPointInfo();
            ScoreInfo FirstScoreObj = new ScoreInfo();
            BattlePointInfo bp = new BattlePointInfo();
            AtackPointInfo ap = new AtackPointInfo();

            Pathfinder = new AStar(this.Tissue);
            ePathfinder = new eAStar(this.Tissue, new Point(PierreTeamInjectionPoint.X - 12, PierreTeamInjectionPoint.Y - 12), 24);

            //��������� ������
            ReadAllMissions();

            //������� HP � AP
            ReadHPsAndAPs();

            //���������� ����� ������ � ������� HP, �� ������� ������ �����
            AnalizeMap(ref FHPnum, ref FAPnum);

            //������� ����� ������ � ����� ������� ScoreMission`�
            if (ScoreObjectives != null)
            {
                count = 0;
                tmp = 10000;
                tmp1 = 0;
                for (int i = 0; i < ScoreObjectives.Length; i++)
                {
                    if (ScoreObjectives[i].Score > count)
                    {
                        count = ScoreObjectives[i].Score;
                    }
                    if (ScoreObjectives[i].Turn < tmp)
                    {
                        tmp = ScoreObjectives[i].Turn;
                        tmp1 = i;
                    }
                }

                FirstScoreObj = ScoreObjectives[tmp1];
                MinHPsToTake = (int)(count / 220) + 1;
            }
            else
            {
                FirstScoreObj.Score = 0;
                FirstScoreObj.Turn = 2000;
                MinHPsToTake = 0;
            }

            //������� ����� ������ NavigationMission
            if (NavigationPoints != null)
            {
                count = 10000;
                tmp = -1;
                for (int i = 0; i < NavigationPoints.Length; i++)
                {
                    if (NavigationPoints[i].EndTurn < count)
                    {
                        count = NavigationPoints[i].EndTurn;
                        tmp = i;
                    }
                }
                FirstNavPoint = NavigationPoints[tmp];
            }
            else
            {
                FirstNavPoint.Location = HoshimiPoints[FHPnum].Location;
            }

            //�������� � ����� ������ �� Pierre`�
            this.InjectionPointWanted = FinallyChooseInjectionPoint(HoshimiPoints[FHPnum].Location, AZNPoints[FAPnum].Location, FirstNavPoint.Location);

            if (KillPierre)
            {
                ap.Location = PierreTeamInjectionPoint;
                ap.Need = 5;
                ap.Exist = 0;
                ATargets.Add(ap);
            }

            //���� �������� HP � AP
            /*
            if (BattleExpected)
            {
                //��� ���������� ������������ ����� HP ���������� �����
                for (int i = 0; i < MinHPsToTake; i++)
                {
                    p = HoshimiPoints[(int)MyHPs[i]].Location;
                    bp.Location = p;
                    bp.Covered = 0;
                    BTargets.Add(bp);
                }

                //��� ������ AP ���������� �����
                p = AZNPoints[FAPnum].Location;
                bp.Location = p;
                bp.Covered = 0;
                BTargets.Add(bp);
            }
            */

            //���������, ������� � ����� ����� ��� ����
            NBPROTECTORTOBUILD = BTargets.Count;
            OBSERVERSTOBUILD = 1;
            NBBODYGUARSTOBUILD = 4;
            BodyGuards = new BodyGuard[NBBODYGUARSTOBUILD];
            for (int i = 0; i < BodyGuards.Length; i++)
            {
                BodyGuards[i] = null;
            }
            //if (KillPierre)
            //{
            //   NBNAVIGATORTOBUILD = NavigationPoints.Length * 2;
            //    NBATACKERTOBUILD = 4;
            //}
            //else
            //{
            NBATACKERTOBUILD = 0;

            if (NavigationPoints != null)
            {
                for (int i = 0; i < NavigationPoints.Length; i++)
                {
                    if ((NavigationPoints[i].BotType == NanoBotType.NanoExplorer) 
                        || ((NavigationPoints[i].BotType == NanoBotType.Unknown) && (NavigationPoints[i].Stock <= 0)))
                    {
                        NBNAVIGATORTOBUILD++;
                    }
                    else if ((NavigationPoints[i].BotType == NanoBotType.NanoCollector)
                        && (NavigationPoints[i].Stock <= 0))
                    {
                        NBATACKERTOBUILD++;
                        AtackPointInfo api = new AtackPointInfo();
                        api.Location = NavigationPoints[i].Location;
                        api.Need = 1;
                        api.Exist = 0;
                        ATargets.Add(api);
                    }
                    else if (((NavigationPoints[i].BotType == NanoBotType.NanoCollector) || (NavigationPoints[i].BotType == NanoBotType.Unknown))
                        && (NavigationPoints[i].Stock > 0) && (NavigationPoints[i].Stock <= 10))
                    {
                        NBDOCTORSTOBUILD++;
                    }
                    else if (((NavigationPoints[i].BotType == NanoBotType.NanoContainer)
                        || ((NavigationPoints[i].BotType == NanoBotType.Unknown) && (NavigationPoints[i].Stock > 0)))
                        && (NavigationPoints[i].Stock <= 50))
                    {
                        HealConvoysWithContainers++;
                    }
                    else if ((NavigationPoints[i].BotType == NanoBotType.NanoContainer || (NavigationPoints[i].BotType == NanoBotType.Unknown))
                        && (NavigationPoints[i].Stock > 50))
                    {
                        BigHealConvoys++;
                    }
                    else if ((NavigationPoints[i].BotType == NanoBotType.NanoCollector)
                        && (NavigationPoints[i].Stock > 10))
                    {
                        HealConvoysWithCollectors++;
                    }
                }
            }
            else
            {
                NBNAVIGATORTOBUILD = 0;
            }
            //}

            if (HealConvoysWithCollectors > 0)
            {
                CollConvoys = new ConvoyWithCollector[HealConvoysWithCollectors];
                for (int i = 0; i < CollConvoys.Length; i++)
                {
                    CollConvoys[i] = new ConvoyWithCollector();
                }
            }

            if (BigHealConvoys > 0)
            {
                BigConvoys = new ConvoyWithBigContainer[BigHealConvoys];
                for (int i = 0; i < BigConvoys.Length; i++)
                {
                    BigConvoys[i] = new ConvoyWithBigContainer();
                }
            }

            //����� ������� - ������� �� ����, ������� �� � �������� ����� ��������� � ������� ��� �� ����
            int NotAllocatedBots = Utils.NbrMaxBots - 2 - NBBODYGUARSTOBUILD - NBATACKERTOBUILD - NBPROTECTORTOBUILD - MyHPs.Count;
            int ConvoysNumber = Math.Min((NotAllocatedBots - (NotAllocatedBots % 3)) / 3, MyHPs.Count + HealConvoysWithContainers);
            ConvoysNumber = Math.Max(ConvoysNumber, 3);
            Convoys = new Convoy[ConvoysNumber];
            for (int i = 0; i < Convoys.Length; i++)
            {
                Convoys[i] = new Convoy(false);
            }
            for (int i = 0; i < HealConvoysWithContainers; i++)
            {
                Convoys[i].IsNavigating = true;
            }
        }
    }
}
