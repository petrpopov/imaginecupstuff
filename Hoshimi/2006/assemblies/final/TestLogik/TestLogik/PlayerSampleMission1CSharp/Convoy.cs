using System.Drawing;
using System.Collections.Generic;
using VG.Common;

namespace MIPT
{
    //��������� ��� ��������� ��������� ������.
    public enum ConvoyState
    {
        Deleted = -1,
        UnderConstruction = 0,
        Waiting = 1,
        Moving = 2,
        CollectingAZN = 3,
        TransferingAZN = 4,
        Fighting = 5
    }

    class Convoy
    {
        private Point Location;
        private Point APoint;
        private Point HPoint;
        private int HPNumber;
        private Point[] Path;
        private ConvoyState State;
        private int DefendersNumber;
        private int ContainersNumber;
        private int Stock;

        private ConvoyContainer[] MyContainer;
        private ConvoyDefender MyDefender;

        private bool NavMission;
        private bool AZNChosen;

        #region Properties
        public bool IsNavigating
        {
            get
            {
                return NavMission;
            }
            set
            {
                NavMission = value;
            }
        }

        public int HNumber
        {
            get
            {
                return HPNumber;
            }
            set
            {
                HPNumber = value;
            }
        }   

        public Point ConvoyLocation
        {
            get
            {
                return Location;
            }
            set
            {
                Location = value;
            }
        }

        public Point AZNPoint
        {
            get
            {
                return APoint;
            }
            set
            {
                APoint = value;
            }
        }

        public Point HoshimiPoint
        {
            get
            {
                return HPoint;
            }
            set
            {
                HPoint = value;
            }
        }

        public ConvoyState ConvoyState
        {
            get
            {
                return State;
            }
            set
            {
                State = value;
            }
        }

        public int Defenders
        {
            get
            {
                return DefendersNumber;
            }
            set
            {
                DefendersNumber = value;
            }
        }

        public int Containers
        {
            get
            {
                return ContainersNumber;
            }
            set
            {
                ContainersNumber = value;
            }
        }
        #endregion

        public Convoy(bool NM)
        {
            State = ConvoyState.UnderConstruction;
            DefendersNumber = 0;
            ContainersNumber = 0;
            Location = new Point(-1, -1);
            APoint = new Point(-1, -1);
            HPoint = new Point(-1, -1);
            HPNumber = -1;
            Path = null;
            MyContainer = new ConvoyContainer[2];
            MyContainer[0] = null;
            MyContainer[1] = null;
            MyDefender = null;
            NavMission = NM;
            AZNChosen = false;
        }

        //"�����������" ������ ���� � ������
        public void AddBot(NanoBot bot)
        {
            if (this.State != ConvoyState.UnderConstruction)
                return;
            if ((bot is ConvoyDefender) && (MyDefender == null))
            {
                MyDefender = (ConvoyDefender)bot;
                return;
            }
            if (bot is ConvoyContainer)
            {
                if (MyContainer[0] == null)
                {
                    MyContainer[0] = (ConvoyContainer)bot;
                    return;
                }
                if (MyContainer[1] == null)
                {
                    MyContainer[1] = (ConvoyContainer)bot;
                    return;
                }
            }
        }

        //����������� ������
        public void Delete()
        {
            if (MyDefender != null)
                MyDefender.ForceAutoDestruction();
            if (MyContainer[0] != null)
                MyContainer[0].ForceAutoDestruction();
            if (MyContainer[1] != null)
                MyContainer[1].ForceAutoDestruction();
            ContainersNumber = 0;
            DefendersNumber = 0;
            State = ConvoyState.UnderConstruction;
            MyDefender = null;
            MyContainer[0] = null;
            MyContainer[1] = null;
            HPNumber = -1;
        }


        //�������� �������. ��������� ����� ���������� ������
        public void Action(MyAI _player)
        {
            double Distance;
            double MinDistance;
            Point ShootAt = new Point();

            //���� ������ ��� ���, �� � ������ ������
            if (State == ConvoyState.UnderConstruction)
                return;

            //������������� ��� ��������: State, Location, Stock, etc
            SetState(_player);

            //���� ���� ���, ��������� ������� �����������
            if (MyDefender != null)
            {
                //���� � �������� ������������ ���-�� ����, ������������� ������
                if (_player.OtherNanoBotsInfo != null)
                    foreach (NanoBotInfo botEnemy in _player.OtherNanoBotsInfo)
                    {
                        if (botEnemy.PlayerID == 0)
                        {
                            Distance = _player.GeomDist(botEnemy.Location, MyDefender.Location);
                            if (Distance < MyDefender.DefenseDistance)
                            {
                                this.Stop();
                            }
                        }
                    }
                //���� ������ �����
                if (this.State == ConvoyState.Waiting)
                {
                    //���� ��������� ����
                    MinDistance = 1000;
                    if (_player.OtherNanoBotsInfo != null)
                        foreach (NanoBotInfo botEnemy in _player.OtherNanoBotsInfo)
                        {
                            if (botEnemy.PlayerID == 0)
                            {
                                Distance = _player.GeomDist(botEnemy.Location, MyDefender.Location);
                                if ((botEnemy.NanoBotType == NanoBotType.NanoAI) && (Distance < MyDefender.DefenseDistance))
                                {
                                    MinDistance = -1;
                                    ShootAt = botEnemy.Location;
                                }
                                if (Distance < MinDistance)
                                {
                                    MinDistance = Distance;
                                    ShootAt = botEnemy.Location;
                                }
                            }
                        }
                    //���� ��� ���������� ������, �������� � ��������� ��������
                    if (MinDistance < MyDefender.DefenseDistance)
                    {
                        MyDefender.DefendTo(ShootAt, 3);
                        return;
                    }
                }
            }

            //���� ������ �����
            if (State == ConvoyState.Waiting)
            {
                //���� �� �� � ����, �� ���������� � �� �����
                if (this.NeedGathering())
                {
                    this.Gather(_player);
                    return;
                }

                //���� �� �� AZNPoint
                if (this.Location == this.APoint)
                {
                    AZNChosen = false;
                    //���� ���������� ������, �� ��������� ��
                    if (this.Stock == 0)
                    {
                        this.FillContainers();
                        return;
                    }
                    //���� �� ������, �� ��� � HoshimiPoint
                    else
                    {
                        if (this.Location == this.HPoint)
                            return;
                        this.Path = _player.Pathfinder.FindPath(this.Location, this.HPoint);
                        this.Move();
                        return;
                    }
                }

                //���� �� �� HoshimiPoint
                if (this.Location == this.HPoint)
                {
                    //���� ��� �� ����� ���� NavPoint
                    if (NavMission)
                    {
                        //���� ����� �� ���� ��� ����������
                        if (_player.CurrentTurn < _player.NavigationPoints[HPNumber].StartTurn)
                        {
                            return;
                        }
                        else if (_player.CurrentTurn >= _player.NavigationPoints[HPNumber].StartTurn
                            && _player.CurrentTurn <= _player.NavigationPoints[HPNumber].EndTurn)
                        {
                            //
                            //��������, ��� ������ ���������
                            List<VG.Mission.BaseObjective> mission = _player.Mission.Objectives;
                            for (int i = 0; i < mission.Count; i++)
                            {
                                if (mission[i].ID == 1)
                                {
                                    //Navigation
                                    VG.Mission.NavigationObjective navObj = (VG.Mission.NavigationObjective)mission[i];
                                    for (int j = 0; j < navObj.NavPoints.Count; j++)
                                    {
                                        if (this.Location == navObj.NavPoints[j].Location && navObj.NavPoints[j].Reached == true)
                                            _player.NavigationPoints[HPNumber].Complete = true;
                                    }
                                }
                            }
                            //
                            //_player.NavigationPoints[HPNumber].Complete = true;
                            //����������� ���������� HP
                            this.HPoint = _player.GetNextHPForConvoy(this.Location, ref this.HPNumber, ref this.NavMission);
                            //���� ������, �� ��� �� AZN
                            if (this.Stock == 0)
                            {
                                this.APoint = _player.GetNearestAZNPoint(this.Location, this.HPoint);
                                AZNChosen = true;
                                this.Path = _player.Pathfinder.FindPath(this.Location, this.APoint);
                                this.Move();
                                return;
                            }
                            //����� ����� � HP
                            else
                            {
                                if (this.Location == this.HPoint)
                                    return;
                                this.Path = _player.Pathfinder.FindPath(this.Location, this.HPoint);
                                this.Move();
                                return;
                            }
                        }
                        else
                        {
                            //��������, ��� ������ ���������
                            _player.NavigationPoints[HPNumber].Complete = true;
                            //����������� ���������� HP
                            this.HPoint = _player.GetNextHPForConvoy(this.Location, ref this.HPNumber, ref this.NavMission);
                            //���� ������, �� ��� �� AZN
                            if (this.Stock == 0)
                            {
                                this.APoint = _player.GetNearestAZNPoint(this.Location, this.HPoint);
                                AZNChosen = true;
                                this.Path = _player.Pathfinder.FindPath(this.Location, this.APoint);
                                this.Move();
                                return;
                            }
                            //����� ����� � HP
                            else
                            {
                                if (this.Location == this.HPoint)
                                    return;
                                this.Path = _player.Pathfinder.FindPath(this.Location, this.HPoint);
                                this.Move();
                                return;
                            }
                        }
                    }

                    //���� ��� �������� HP, �� ���������, ��� �� ��
                    if (HPNumber >= 0)
                    {
                        //���� ���, �� ������������� �� �����
                        if (_player.HoshimiPoints[HPNumber].Needle == 2)
                        {
                            //this.HPoint = _player.GetNearestHPForConvoy(this.Location, ref this.HPNumber);
                            int fafa = HPNumber;
                            this.HPoint = _player.GetNextHPForConvoy(this.Location, ref this.HPNumber, ref this.NavMission);
                            //���� ������, �� ��� �� AZN
                            if (this.Stock == 0)
                            {
                                if (!IsNavigating)
                                {
                                    this.APoint = _player.GetNearestAZNPoint(fafa, HPNumber);
                                }
                                else
                                {
                                    this.APoint = _player.GetNearestAZNPoint(this.Location, this.HPoint);
                                }
                                AZNChosen = true;
                                this.Path = _player.Pathfinder.FindPath(this.Location, this.APoint);
                                this.Move();
                                return;
                            }
                            //����� ����� � HP
                            else
                            {
                                if (this.Location == this.HPoint)
                                    return;
                                this.Path = _player.Pathfinder.FindPath(this.Location, this.HPoint);
                                this.Move();
                                return;
                            }
                        }
                    }
                    //��������� ��� Needle`�
                    foreach (NanoBot bot in _player.NanoBots)
                    {
                        //���� ������� Needle �� ���� ����� � �� ������
                        if ((bot is Needle) && (bot.Stock == bot.ContainerCapacity) && (bot.Location == this.Location))
                        {
                            if (HPNumber >= 0)
                            {
                                //�� �������� ���� ����
                                _player.HoshimiPoints[HPNumber].Full = 1;
                                //����������� ����� HP
                                //this.HPoint = _player.GetNearestHPForConvoy(this.Location, ref this.HPNumber);
                                int fafa = HPNumber;
                                this.HPoint = _player.GetNextHPForConvoy(this.Location, ref this.HPNumber, ref this.NavMission);
                                //���� ������, �� ��� �� AZN
                                if (this.Stock == 0)
                                {
                                    if (!IsNavigating)
                                    {
                                        this.APoint = _player.GetNearestAZNPoint(fafa, HPNumber);
                                    }
                                    else
                                    {
                                        this.APoint = _player.GetNearestAZNPoint(this.Location, this.HPoint);
                                    }
                                    AZNChosen = true;
                                    this.Path = _player.Pathfinder.FindPath(this.Location, this.APoint);
                                    this.Move();
                                    return;
                                }
                                //����� ����� � HP
                                else
                                {
                                    if (this.Location == this.HPoint)
                                        return;
                                    this.Path = _player.Pathfinder.FindPath(this.Location, this.HPoint);
                                    this.Move();
                                    return;
                                }
                            }
                        }
                    }

                    if (HPNumber >= 0)
                    {
                        //���� ����� ��� Needle
                        if (_player.HoshimiPoints[HPNumber].Needle == 1)
                        {
                            //� Needle ��� �� ��������
                            //���� �� ��� ������������, �� ��� �� AZN
                            if (this.Stock == 0)
                            {
                                this.APoint = _player.GetNearestAZNPoint(HPNumber, HPNumber);
                                AZNChosen = true;
                                this.Path = _player.Pathfinder.FindPath(this.Location, this.APoint);
                                this.Move();
                                return;
                            }
                            //����� ������������
                            else
                            {
                                this.Transfert();
                                return;
                            }
                        }
                    }
                }

                //���� �� ����� ���
                //���� ������, �� �� AZN
                if (this.Stock == 0)
                {
                    if (!AZNChosen)
                    {
                        int fafa = -1;
                        for (int i = 0; i < _player.HoshimiPoints.Length; i++)
                        {
                            if (_player.HoshimiPoints[i].Location == this.Location)
                                fafa = i;
                        }
                        if ((!IsNavigating) && (fafa >= 0))
                        {
                            this.APoint = _player.GetNearestAZNPoint(fafa, HPNumber);
                        }
                        else
                        {
                            this.APoint = _player.GetNearestAZNPoint(this.Location, this.HPoint);
                        }
                        AZNChosen = true;
                    }
                    this.Path = _player.Pathfinder.FindPath(this.Location, this.APoint);
                    this.Move();
                    return;
                }
                //����� �� HP
                else
                {
                    if (this.Location == this.HPoint)
                        return;
                    this.Path = _player.Pathfinder.FindPath(this.Location, this.HPoint);
                    this.Move();
                    return;
                }
            }
        }

        //���������� AZN
        private void Transfert()
        {
            if (MyContainer[0] != null)
                MyContainer[0].TransferTo(this.Location, MyContainer[0].Stock / MyContainer[0].CollectTransfertSpeed);
            if (MyContainer[1] != null)
                MyContainer[1].TransferTo(this.Location, MyContainer[1].Stock / MyContainer[1].CollectTransfertSpeed);
        }

        //���������
        private void Move()
        {
            if (MyDefender != null)
            {
                MyDefender.StopMoving();
                MyDefender.MoveTo(this.Path);
            }
            if (MyContainer[0] != null)
            {
                MyContainer[0].StopMoving();
                MyContainer[0].MoveTo(this.Path);
            }
            if (MyContainer[1] != null)
            {
                MyContainer[1].StopMoving();
                MyContainer[1].MoveTo(this.Path);
            }
        }

        //��������� AZN
        private void FillContainers()
        {
            if (MyContainer[0] != null)
                MyContainer[0].CollectFrom(this.Location, MyContainer[0].ContainerCapacity / MyContainer[0].CollectTransfertSpeed);
            if (MyContainer[1] != null)
                MyContainer[1].CollectFrom(this.Location, MyContainer[1].ContainerCapacity / MyContainer[1].CollectTransfertSpeed);
        }

        //���������� ������� ��������� ������
        private void SetState(MyAI _player)
        {
            //��������, ���� �� ����
            if (MyDefender != null && MyDefender.HitPoint <= 0)
                MyDefender = null;
            if (MyContainer[0] != null && MyContainer[0].HitPoint <= 0)
                MyContainer[0] = null;
            if (MyContainer[1] != null && MyContainer[1].HitPoint <= 0)
                MyContainer[1] = null;

            //���� ����� ���, �� ������ ���������
            if (MyDefender == null && MyContainer[0] == null && MyContainer[1] == null)
            {
                this.State = ConvoyState.UnderConstruction;
                this.MyDefender = null;
                this.MyContainer[0] = null;
                this.MyContainer[1] = null;
                this.DefendersNumber = 0;
                this.ContainersNumber = 0;
                this.HPNumber = -1;
                return;
            }

            //��������� ������ ��� Stock � Location
            this.Stock = 0;
            if (MyDefender != null)
            {
                this.Location = MyDefender.Location;
            }
            if (MyContainer[0] != null)
            {
                this.Location = MyContainer[0].Location;
                this.Stock += MyContainer[0].Stock;
            }
            if (MyContainer[1] != null)
            {
                this.Location = MyContainer[1].Location;
                this.Stock += MyContainer[1].Stock;
            }

            //���� HP �� ��������, �� ����������
            if (this.HPNumber == -1)
            {
                //this.HPoint = _player.GetNearestHPForConvoy(this.Location, ref HPNumber);

                this.HPoint = _player.GetNextHPForConvoy(this.Location, ref this.HPNumber, ref this.NavMission);
            }

            //���� HP ����� ����������� ��� �������� ��� ��� ������, ����������� �����
            if (HPNumber >= 0 && !IsNavigating)
            {
                if ((_player.HoshimiPoints[HPNumber].Needle == 2) || (_player.HoshimiPoints[HPNumber].Full == 1) || (_player.HoshimiPoints[HPNumber].InPast == 1 && _player.HoshimiPoints[HPNumber].Needle != 1))
                {
                    //this.HPoint = _player.GetNearestHPForConvoy(this.Location, ref HPNumber);
                    this.HPoint = _player.GetNextHPForConvoy(this.Location, ref this.HPNumber, ref this.NavMission);
                    this.Stop();
                }
            }

            //���� Defender � ���, �� ������ � ������ � ���
            if (MyDefender != null && MyDefender.State == NanoBotState.Defending)
            {
                this.State = ConvoyState.Fighting;
                return;
            }

            //������-������ ���������� ��-�� ����, ��� ������, ��� ���, � ��� deadlink
            if (MyDefender != null)
            {
                if (MyContainer[0] != null && MyContainer[1] != null)
                {
                    if ((MyDefender.State == NanoBotState.WaitingOrders)
                        && (MyContainer[0].State == NanoBotState.WaitingOrders)
                        && (MyContainer[1].State == NanoBotState.WaitingOrders))
                    {
                        this.State = ConvoyState.Waiting;
                    }
                    if ((MyDefender.State == NanoBotState.Moving)
                        || (MyContainer[0].State == NanoBotState.Moving)
                        || (MyContainer[1].State == NanoBotState.Moving))
                    {
                        this.State = ConvoyState.Moving;
                    }
                    if ((MyContainer[0].State == NanoBotState.Collecting)
                        || (MyContainer[1].State == NanoBotState.Collecting))
                    {
                        this.State = ConvoyState.CollectingAZN;
                    }
                    if ((MyContainer[0].State == NanoBotState.TransferingStock)
                        || (MyContainer[1].State == NanoBotState.TransferingStock))
                    {
                        this.State = ConvoyState.TransferingAZN;
                    }
                }
                else if (MyContainer[0] != null && MyContainer[1] == null)
                {
                    if ((MyDefender.State == NanoBotState.WaitingOrders)
                        && (MyContainer[0].State == NanoBotState.WaitingOrders))
                    {
                        this.State = ConvoyState.Waiting;
                    }
                    if ((MyDefender.State == NanoBotState.Moving)
                        || (MyContainer[0].State == NanoBotState.Moving))
                    {
                        this.State = ConvoyState.Moving;
                    }
                    if (MyContainer[0].State == NanoBotState.Collecting)
                    {
                        this.State = ConvoyState.CollectingAZN;
                    }
                    if (MyContainer[0].State == NanoBotState.TransferingStock)
                    {
                        this.State = ConvoyState.TransferingAZN;
                    }
                }
                else if (MyContainer[0] == null && MyContainer[1] != null)
                {
                    if ((MyDefender.State == NanoBotState.WaitingOrders)
                        && (MyContainer[1].State == NanoBotState.WaitingOrders))
                    {
                        this.State = ConvoyState.Waiting;
                    }
                    if ((MyDefender.State == NanoBotState.Moving)
                        || (MyContainer[1].State == NanoBotState.Moving))
                    {
                        this.State = ConvoyState.Moving;
                    }
                    if (MyContainer[1].State == NanoBotState.Collecting)
                    {
                        this.State = ConvoyState.CollectingAZN;
                    }
                    if (MyContainer[1].State == NanoBotState.TransferingStock)
                    {
                        this.State = ConvoyState.TransferingAZN;
                    }
                }
                else if (MyContainer[0] == null && MyContainer[1] == null)
                {
                    if (MyDefender.State == NanoBotState.WaitingOrders)
                    {
                        this.State = ConvoyState.Waiting;
                    }
                    if (MyDefender.State == NanoBotState.Moving)
                    {
                        this.State = ConvoyState.Moving;
                    }
                }
            }
            else
            {
                if (MyContainer[0] != null && MyContainer[1] != null)
                {
                    if ((MyContainer[0].State == NanoBotState.WaitingOrders)
                        && (MyContainer[1].State == NanoBotState.WaitingOrders))
                    {
                        this.State = ConvoyState.Waiting;
                    }
                    if ((MyContainer[0].State == NanoBotState.Moving)
                        || (MyContainer[1].State == NanoBotState.Moving))
                    {
                        this.State = ConvoyState.Moving;
                    }
                    if ((MyContainer[0].State == NanoBotState.Collecting)
                        || (MyContainer[1].State == NanoBotState.Collecting))
                    {
                        this.State = ConvoyState.CollectingAZN;
                    }
                    if ((MyContainer[0].State == NanoBotState.TransferingStock)
                        || (MyContainer[1].State == NanoBotState.TransferingStock))
                    {
                        this.State = ConvoyState.TransferingAZN;
                    }
                }
                else if (MyContainer[0] != null && MyContainer[1] == null)
                {
                    if (MyContainer[0].State == NanoBotState.WaitingOrders)
                    {
                        this.State = ConvoyState.Waiting;
                    }
                    if (MyContainer[0].State == NanoBotState.Moving)
                    {
                        this.State = ConvoyState.Moving;
                    }
                    if (MyContainer[0].State == NanoBotState.Collecting)
                    {
                        this.State = ConvoyState.CollectingAZN;
                    }
                    if (MyContainer[0].State == NanoBotState.TransferingStock)
                    {
                        this.State = ConvoyState.TransferingAZN;
                    }
                }
                else if (MyContainer[0] == null && MyContainer[1] != null)
                {
                    if (MyContainer[1].State == NanoBotState.WaitingOrders)
                    {
                        this.State = ConvoyState.Waiting;
                    }
                    if (MyContainer[1].State == NanoBotState.Moving)
                    {
                        this.State = ConvoyState.Moving;
                    }
                    if (MyContainer[1].State == NanoBotState.Collecting)
                    {
                        this.State = ConvoyState.CollectingAZN;
                    }
                    if (MyContainer[1].State == NanoBotState.TransferingStock)
                    {
                        this.State = ConvoyState.TransferingAZN;
                    }
                }
                else if (MyContainer[0] == null && MyContainer[1] == null)
                {
                    this.State = ConvoyState.UnderConstruction;
                    this.MyDefender = null;
                    this.MyContainer[0] = null;
                    this.MyContainer[1] = null;
                    this.ContainersNumber = 0;
                    this.DefendersNumber = 0;
                    this.HPNumber = -1;
                }
            }
        }

        //������������� ������
        private void Stop()
        {
            if (MyDefender != null)
            {
                MyDefender.StopMoving();
            }
            if (MyContainer[0] != null)
            {
                MyContainer[0].StopMoving();
            }
            if (MyContainer[1] != null)
            {
                MyContainer[1].StopMoving();
            }
            this.State = ConvoyState.Waiting;
        }

        //���������, ������ �� ���� ��� ���.
        private bool NeedGathering()
        {
            bool res = false;
            if(MyDefender != null)
                if(MyDefender.Location != this.Location)
                    res = true;
            if(MyContainer[0] != null)
                if(MyContainer[0].Location != this.Location)
                    res = true;
            if (MyContainer[1] != null)
                if (MyContainer[1].Location != this.Location)
                    res = true;
            return res;
        }

        //��������� ������, ���� �� �����-�� �������� ��-���� �����������.
        //������������ �� �����. ����� ������������ ������ � ������ �����,
        //���� ����� ����� ������ �������, � ����� ���.
        private void Gather(MyAI _player)
        {
            if (MyDefender != null)
            {
                MyDefender.StopMoving();
                MyDefender.MoveTo(_player.Pathfinder.FindPath(MyDefender.Location, this.Location));
            }
            if (MyContainer[0] != null)
            {
                MyContainer[0].StopMoving();
                MyContainer[0].MoveTo(_player.Pathfinder.FindPath(MyContainer[0].Location, this.Location));
            }
            if (MyContainer[1] != null)
            {
                MyContainer[1].StopMoving();
                MyContainer[1].MoveTo(_player.Pathfinder.FindPath(MyContainer[1].Location, this.Location));
            }
        }
    }

    class ConvoyWithCollector
    {
        private Point Location;
        private Point APoint;
        private Point HPoint;
        private int HPNumber;
        private Point[] Path;
        private ConvoyState State;
        private int DefendersNumber;
        private int ContainersNumber;
        private int Stock;

        private ConvoyCollector MyContainer;
        private ConvoyDefender2 MyDefender;

        #region Properties
        public int HNumber
        {
            get
            {
                return HPNumber;
            }
            set
            {
                HPNumber = value;
            }
        }

        public Point ConvoyLocation
        {
            get
            {
                return Location;
            }
            set
            {
                Location = value;
            }
        }

        public Point AZNPoint
        {
            get
            {
                return APoint;
            }
            set
            {
                APoint = value;
            }
        }

        public Point HoshimiPoint
        {
            get
            {
                return HPoint;
            }
            set
            {
                HPoint = value;
            }
        }

        public ConvoyState ConvoyState
        {
            get
            {
                return State;
            }
            set
            {
                State = value;
            }
        }

        public int Defenders
        {
            get
            {
                return DefendersNumber;
            }
            set
            {
                DefendersNumber = value;
            }
        }

        public int Containers
        {
            get
            {
                return ContainersNumber;
            }
            set
            {
                ContainersNumber = value;
            }
        }
        #endregion

        public ConvoyWithCollector()
        {
            State = ConvoyState.UnderConstruction;
            DefendersNumber = 0;
            ContainersNumber = 0;
            Location = new Point(-1, -1);
            APoint = new Point(-1, -1);
            HPoint = new Point(-1, -1);
            HPNumber = -1;
            Path = null;
            MyContainer = null;
            MyDefender = null;
        }

        //"�����������" ������ ���� � ������
        public void AddBot(NanoBot bot)
        {
            if (this.State != ConvoyState.UnderConstruction)
                return;
            if ((bot is ConvoyDefender2) && (MyDefender == null))
            {
                MyDefender = (ConvoyDefender2)bot;
                return;
            }
            if (bot is ConvoyCollector)
            {
                if (MyContainer == null)
                {
                    MyContainer = (ConvoyCollector)bot;
                    return;
                }
            }
        }

        //����������� ������
        public void Delete()
        {
            if (MyDefender != null)
                MyDefender.ForceAutoDestruction();
            if (MyContainer != null)
                MyContainer.ForceAutoDestruction();
            ContainersNumber = 0;
            DefendersNumber = 0;
            State = ConvoyState.UnderConstruction;
            MyDefender = null;
            MyContainer = null;
            HPNumber = -1;
        }


        //�������� �������. ��������� ����� ���������� ������
        public void Action(MyAI _player)
        {
            double Distance;
            double MinDistance;
            Point ShootAt = new Point();

            //���� ������ ��� ���, �� � ������ ������
            if (State == ConvoyState.UnderConstruction)
                return;

            //������������� ��� ��������: State, Location, Stock, etc
            SetState(_player);

            //���� ���� ���, ��������� ������� �����������
            if (MyDefender != null)
            {
                //���� � �������� ������������ ���-�� ����, ������������� ������
                if (_player.OtherNanoBotsInfo != null)
                    foreach (NanoBotInfo botEnemy in _player.OtherNanoBotsInfo)
                    {
                        if (botEnemy.PlayerID == 0)
                        {
                            Distance = _player.GeomDist(botEnemy.Location, MyDefender.Location);
                            if (Distance < MyDefender.DefenseDistance)
                            {
                                this.Stop();
                            }
                        }
                    }
                //���� ������ �����
                if (this.State == ConvoyState.Waiting)
                {
                    //���� ��������� ����
                    MinDistance = 1000;
                    if (_player.OtherNanoBotsInfo != null)
                        foreach (NanoBotInfo botEnemy in _player.OtherNanoBotsInfo)
                        {
                            if (botEnemy.PlayerID == 0)
                            {
                                Distance = _player.GeomDist(botEnemy.Location, MyDefender.Location);
                                if ((botEnemy.NanoBotType == NanoBotType.NanoAI) && (Distance < MyDefender.DefenseDistance))
                                {
                                    MinDistance = -1;
                                    ShootAt = botEnemy.Location;
                                }
                                if (Distance < MinDistance)
                                {
                                    MinDistance = Distance;
                                    ShootAt = botEnemy.Location;
                                }
                            }
                        }
                    //���� ��� ���������� ������, �������� � ��������� ��������
                    if (MinDistance < MyDefender.DefenseDistance)
                    {
                        MyDefender.DefendTo(ShootAt, 3);
                        return;
                    }
                }
            }

            //���� ������ �����
            if (State == ConvoyState.Waiting)
            {
                //���� �� �� � ����, �� ���������� � �� �����
                if (this.NeedGathering())
                {
                    this.Gather(_player);
                    return;
                }

                //���� �� �� AZNPoint
                if (this.Location == this.APoint)
                {
                    //���� ���������� ������, �� ��������� ��
                    if (this.Stock == 0)
                    {
                        this.FillContainers();
                        return;
                    }
                    //���� �� ������, �� ��� � HoshimiPoint
                    else
                    {
                        if (this.Location == this.HPoint)
                            return;
                        this.Path = _player.Pathfinder.FindPath(this.Location, this.HPoint);
                        this.Move();
                        return;
                    }
                }

                //���� �� �� NavPoint
                if (this.Location == this.HPoint)
                {
                    //
                    //���� ����� �� ���� ��� ����������
                    if (_player.CurrentTurn < _player.NavigationPoints[HPNumber].StartTurn)
                    {
                        return;
                    }
                    else if (_player.CurrentTurn >= _player.NavigationPoints[HPNumber].StartTurn
                        && _player.CurrentTurn <= _player.NavigationPoints[HPNumber].EndTurn)
                    {
                        //
                        //��������, ��� ������ ���������
                        List<VG.Mission.BaseObjective> mission = _player.Mission.Objectives;
                        for (int i = 0; i < mission.Count; i++)
                        {
                            if (mission[i].ID == 1)
                            {
                                //Navigation
                                VG.Mission.NavigationObjective navObj = (VG.Mission.NavigationObjective)mission[i];
                                for (int j = 0; j < navObj.NavPoints.Count; j++)
                                {
                                    if (this.Location == navObj.NavPoints[j].Location && navObj.NavPoints[j].Reached == true)
                                        _player.NavigationPoints[HPNumber].Complete = true;
                                }
                            }
                        }
                        //
                        //_player.NavigationPoints[HPNumber].Complete = true;
                        //����������� ���������� HP
                        this.HPoint = _player.GetNextHPForConvoyWithCollector(this.Location, ref this.HPNumber);
                        if (HPNumber == -10)
                        {
                            this.Delete();
                            this.State = ConvoyState.Deleted;
                            return;
                        }
                        //���� ������, �� ��� �� AZN
                        if (this.Stock == 0)
                        {
                            this.APoint = _player.GetNearestAZNPoint(this.Location, this.HPoint);
                            this.Path = _player.Pathfinder.FindPath(this.Location, this.APoint);
                            this.Move();
                            return;
                        }
                        //����� ����� � HP
                        else
                        {
                            if (this.Location == this.HPoint)
                                return;
                            this.Path = _player.Pathfinder.FindPath(this.Location, this.HPoint);
                            this.Move();
                            return;
                        }
                    }
                    else
                    {
                        //��������, ��� ������ ���������
                        _player.NavigationPoints[HPNumber].Complete = true;
                        //����������� ���������� HP
                        this.HPoint = _player.GetNextHPForConvoyWithCollector(this.Location, ref this.HPNumber);
                        if (HPNumber == -10)
                        {
                            this.Delete();
                            this.State = ConvoyState.Deleted;
                            return;
                        }
                        //���� ������, �� ��� �� AZN
                        if (this.Stock == 0)
                        {
                            this.APoint = _player.GetNearestAZNPoint(this.Location, this.HPoint);
                            this.Path = _player.Pathfinder.FindPath(this.Location, this.APoint);
                            this.Move();
                            return;
                        }
                        //����� ����� � HP
                        else
                        {
                            if (this.Location == this.HPoint)
                                return;
                            this.Path = _player.Pathfinder.FindPath(this.Location, this.HPoint);
                            this.Move();
                            return;
                        }
                    }
                    //
                    /*
                    //���� ����� �� ���� ��� ����������
                    if (_player.CurrentTurn > _player.NavigationPoints[HPNumber].StartTurn)
                    {
                        //��������, ��� ������ ���������
                        _player.NavigationPoints[HPNumber].Complete = true;
                        //����������� ����� HP
                        this.HPoint = _player.GetNextHPForConvoyWithCollector(this.Location, ref this.HPNumber);
                        if (HPNumber == -10)
                        {
                            this.Delete();
                            this.State = ConvoyState.Deleted;
                            return;
                        }
                        //���� ������, �� ��� �� AZN
                        if (this.Stock == 0)
                        {
                            this.APoint = _player.GetNearestAZNPoint(this.Location, this.HPoint);
                            this.Path = _player.Pathfinder.FindPath(this.Location, this.APoint);
                            this.Move();
                            return;
                        }
                        //����� ����� � HP
                        else
                        {
                            if (this.Location == this.HPoint)
                                return;
                            this.Path = _player.Pathfinder.FindPath(this.Location, this.HPoint);
                            this.Move();
                            return;
                        }
                    }
                    */
                }

                //���� �� ����� ���
                //���� ������, �� �� AZN
                if (this.Stock == 0)
                {
                    this.APoint = _player.GetNearestAZNPoint(this.Location, this.HPoint);
                    this.Path = _player.Pathfinder.FindPath(this.Location, this.APoint);
                    this.Move();
                    return;
                }
                //����� �� HP
                else
                {
                    if (this.Location == this.HPoint)
                        return;
                    this.Path = _player.Pathfinder.FindPath(this.Location, this.HPoint);
                    this.Move();
                    return;
                }
            }
        }

        //���������� AZN
        private void Transfert()
        {
            if (MyContainer != null)
                MyContainer.TransferTo(this.Location, MyContainer.Stock / MyContainer.CollectTransfertSpeed);
        }

        //���������
        private void Move()
        {
            if (MyDefender != null)
            {
                MyDefender.StopMoving();
                MyDefender.MoveTo(this.Path);
            }
            if (MyContainer != null)
            {
                MyContainer.StopMoving();
                MyContainer.MoveTo(this.Path);
            }
        }

        //��������� AZN
        private void FillContainers()
        {
            if (MyContainer != null)
                MyContainer.CollectFrom(this.Location, MyContainer.ContainerCapacity / MyContainer.CollectTransfertSpeed);
        }

        //���������� ������� ��������� ������
        private void SetState(MyAI _player)
        {
            if (State == ConvoyState.Deleted)
                return;

            //��������, ���� �� ����
            if (MyDefender != null && MyDefender.HitPoint <= 0)
                MyDefender = null;
            if (MyContainer != null && MyContainer.HitPoint <= 0)
                MyContainer = null;

            //���� ����� ���, �� ������ ���������
            if (MyDefender == null && MyContainer == null)
            {
                this.State = ConvoyState.UnderConstruction;
                this.MyDefender = null;
                this.MyContainer = null;
                this.DefendersNumber = 0;
                this.ContainersNumber = 0;
                this.HPNumber = -1;
                return;
            }

            //��������� ������ ��� Stock � Location
            this.Stock = 0;
            if (MyDefender != null)
            {
                this.Location = MyDefender.Location;
            }
            if (MyContainer != null)
            {
                this.Location = MyContainer.Location;
                this.Stock += MyContainer.Stock;
            }

            //���� HP �� ��������, �� ����������
            if (this.HPNumber == -1)
            {
                //this.HPoint = _player.GetNearestHPForConvoy(this.Location, ref HPNumber);

                this.HPoint = _player.GetNextHPForConvoyWithCollector(this.Location, ref this.HPNumber);
                if (HPNumber == -10)
                {
                    this.Delete();
                    this.State = ConvoyState.Deleted;
                    return;
                }
            }

            if (HPNumber >= 0)
            {
                if (_player.NavigationPoints[HPNumber].Complete == true)
                {
                    this.HPoint = _player.GetNextHPForConvoyWithCollector(this.Location, ref this.HPNumber);
                    if (HPNumber == -10)
                    {
                        this.Delete();
                        this.State = ConvoyState.Deleted;
                        return;
                    }
                    this.Stop();
                }
            }

            //���� Defender � ���, �� ������ � ������ � ���
            if (MyDefender != null && MyDefender.State == NanoBotState.Defending)
            {
                this.State = ConvoyState.Fighting;
                return;
            }

            //������-������ ���������� ��-�� ����, ��� ������, ��� ���, � ��� deadlink
            if (MyDefender != null)
            {
                if (MyContainer != null)
                {
                    if ((MyDefender.State == NanoBotState.WaitingOrders)
                        && (MyContainer.State == NanoBotState.WaitingOrders))
                    {
                        this.State = ConvoyState.Waiting;
                    }
                    if ((MyDefender.State == NanoBotState.Moving)
                        || (MyContainer.State == NanoBotState.Moving))
                    {
                        this.State = ConvoyState.Moving;
                    }
                    if ((MyContainer.State == NanoBotState.Collecting))
                    {
                        this.State = ConvoyState.CollectingAZN;
                    }
                    if ((MyContainer.State == NanoBotState.TransferingStock))
                    {
                        this.State = ConvoyState.TransferingAZN;
                    }
                }
            }
            else
            {
                if (MyContainer != null)
                {
                    if ((MyContainer.State == NanoBotState.WaitingOrders))
                    {
                        this.State = ConvoyState.Waiting;
                    }
                    if ((MyContainer.State == NanoBotState.Moving))
                    {
                        this.State = ConvoyState.Moving;
                    }
                    if ((MyContainer.State == NanoBotState.Collecting))
                    {
                        this.State = ConvoyState.CollectingAZN;
                    }
                    if ((MyContainer.State == NanoBotState.TransferingStock))
                    {
                        this.State = ConvoyState.TransferingAZN;
                    }
                }
                else if (MyContainer == null)
                {
                    this.State = ConvoyState.UnderConstruction;
                    this.MyDefender = null;
                    this.MyContainer = null;
                    this.ContainersNumber = 0;
                    this.DefendersNumber = 0;
                    this.HPNumber = -1;
                }
            }
        }

        //������������� ������
        private void Stop()
        {
            if (MyDefender != null)
            {
                MyDefender.StopMoving();
            }
            if (MyContainer != null)
            {
                MyContainer.StopMoving();
            }
            this.State = ConvoyState.Waiting;
        }

        //���������, ������ �� ���� ��� ���.
        private bool NeedGathering()
        {
            bool res = false;
            if (MyDefender != null)
                if (MyDefender.Location != this.Location)
                    res = true;
            if (MyContainer != null)
                if (MyContainer.Location != this.Location)
                    res = true;
            return res;
        }

        //��������� ������, ���� �� �����-�� �������� ��-���� �����������.
        //������������ �� �����. ����� ������������ ������ � ������ �����,
        //���� ����� ����� ������ �������, � ����� ���.
        private void Gather(MyAI _player)
        {
            if (MyDefender != null)
            {
                MyDefender.StopMoving();
                MyDefender.MoveTo(_player.Pathfinder.FindPath(MyDefender.Location, this.Location));
            }
            if (MyContainer != null)
            {
                MyContainer.StopMoving();
                MyContainer.MoveTo(_player.Pathfinder.FindPath(MyContainer.Location, this.Location));
            }
        }
    }

    class ConvoyWithBigContainer
    {
        private Point Location;
        private Point APoint;
        private Point HPoint;
        private int HPNumber;
        private Point[] Path;
        private ConvoyState State;
        private int DefendersNumber;
        private int ContainersNumber;
        private int Stock;

        private BigConvoyContainer MyContainer;
        private ConvoyDefender3 MyDefender;

        #region Properties
        public int HNumber
        {
            get
            {
                return HPNumber;
            }
            set
            {
                HPNumber = value;
            }
        }

        public Point ConvoyLocation
        {
            get
            {
                return Location;
            }
            set
            {
                Location = value;
            }
        }

        public Point AZNPoint
        {
            get
            {
                return APoint;
            }
            set
            {
                APoint = value;
            }
        }

        public Point HoshimiPoint
        {
            get
            {
                return HPoint;
            }
            set
            {
                HPoint = value;
            }
        }

        public ConvoyState ConvoyState
        {
            get
            {
                return State;
            }
            set
            {
                State = value;
            }
        }

        public int Defenders
        {
            get
            {
                return DefendersNumber;
            }
            set
            {
                DefendersNumber = value;
            }
        }

        public int Containers
        {
            get
            {
                return ContainersNumber;
            }
            set
            {
                ContainersNumber = value;
            }
        }
        #endregion

        public ConvoyWithBigContainer()
        {
            State = ConvoyState.UnderConstruction;
            DefendersNumber = 0;
            ContainersNumber = 0;
            Location = new Point(-1, -1);
            APoint = new Point(-1, -1);
            HPoint = new Point(-1, -1);
            HPNumber = -1;
            Path = null;
            MyContainer = null;
            MyDefender = null;
        }

        //"�����������" ������ ���� � ������
        public void AddBot(NanoBot bot)
        {
            if (this.State != ConvoyState.UnderConstruction)
                return;
            if ((bot is ConvoyDefender3) && (MyDefender == null))
            {
                MyDefender = (ConvoyDefender3)bot;
                return;
            }
            if (bot is BigConvoyContainer)
            {
                if (MyContainer == null)
                {
                    MyContainer = (BigConvoyContainer)bot;
                    return;
                }
            }
        }

        //����������� ������
        public void Delete()
        {
            if (MyDefender != null)
                MyDefender.ForceAutoDestruction();
            if (MyContainer != null)
                MyContainer.ForceAutoDestruction();
            ContainersNumber = 0;
            DefendersNumber = 0;
            State = ConvoyState.UnderConstruction;
            MyDefender = null;
            MyContainer = null;
            HPNumber = -1;
        }


        //�������� �������. ��������� ����� ���������� ������
        public void Action(MyAI _player)
        {
            double Distance;
            double MinDistance;
            Point ShootAt = new Point();

            //���� ������ ��� ���, �� � ������ ������
            if (State == ConvoyState.UnderConstruction)
                return;

            //������������� ��� ��������: State, Location, Stock, etc
            SetState(_player);

            //���� ���� ���, ��������� ������� �����������
            if (MyDefender != null)
            {
                //���� � �������� ������������ ���-�� ����, ������������� ������
                if (_player.OtherNanoBotsInfo != null)
                    foreach (NanoBotInfo botEnemy in _player.OtherNanoBotsInfo)
                    {
                        if (botEnemy.PlayerID == 0)
                        {
                            Distance = _player.GeomDist(botEnemy.Location, MyDefender.Location);
                            if (Distance < MyDefender.DefenseDistance)
                            {
                                this.Stop();
                            }
                        }
                    }
                //���� ������ �����
                if (this.State == ConvoyState.Waiting)
                {
                    //���� ��������� ����
                    MinDistance = 1000;
                    if (_player.OtherNanoBotsInfo != null)
                        foreach (NanoBotInfo botEnemy in _player.OtherNanoBotsInfo)
                        {
                            if (botEnemy.PlayerID == 0)
                            {
                                Distance = _player.GeomDist(botEnemy.Location, MyDefender.Location);
                                if ((botEnemy.NanoBotType == NanoBotType.NanoAI) && (Distance < MyDefender.DefenseDistance))
                                {
                                    MinDistance = -1;
                                    ShootAt = botEnemy.Location;
                                }
                                if (Distance < MinDistance)
                                {
                                    MinDistance = Distance;
                                    ShootAt = botEnemy.Location;
                                }
                            }
                        }
                    //���� ��� ���������� ������, �������� � ��������� ��������
                    if (MinDistance < MyDefender.DefenseDistance)
                    {
                        MyDefender.DefendTo(ShootAt, 3);
                        return;
                    }
                }
            }

            //���� ������ �����
            if (State == ConvoyState.Waiting)
            {
                //���� �� �� � ����, �� ���������� � �� �����
                if (this.NeedGathering())
                {
                    this.Gather(_player);
                    return;
                }

                //���� �� �� AZNPoint
                if (this.Location == this.APoint)
                {
                    //���� ���������� ������, �� ��������� ��
                    if (this.Stock == 0)
                    {
                        this.FillContainers();
                        return;
                    }
                    //���� �� ������, �� ��� � HoshimiPoint
                    else
                    {
                        if (this.Location == this.HPoint)
                            return;
                        this.Path = _player.Pathfinder.FindPath(this.Location, this.HPoint);
                        this.Move();
                        return;
                    }
                }

                //���� �� �� NavPoint
                if (this.Location == this.HPoint)
                {
                    //
                    //���� ����� �� ���� ��� ����������
                    if (_player.CurrentTurn < _player.NavigationPoints[HPNumber].StartTurn)
                    {
                        return;
                    }
                    else if (_player.CurrentTurn >= _player.NavigationPoints[HPNumber].StartTurn
                        && _player.CurrentTurn <= _player.NavigationPoints[HPNumber].EndTurn)
                    {
                        //
                        //��������, ��� ������ ���������
                        List<VG.Mission.BaseObjective> mission = _player.Mission.Objectives;
                        for (int i = 0; i < mission.Count; i++)
                        {
                            if (mission[i].ID == 1)
                            {
                                //Navigation
                                VG.Mission.NavigationObjective navObj = (VG.Mission.NavigationObjective)mission[i];
                                for (int j = 0; j < navObj.NavPoints.Count; j++)
                                {
                                    if (this.Location == navObj.NavPoints[j].Location && navObj.NavPoints[j].Reached == true)
                                        _player.NavigationPoints[HPNumber].Complete = true;
                                }
                            }
                        }
                        //
                        //_player.NavigationPoints[HPNumber].Complete = true;
                        //����������� ����� HP
                        this.HPoint = _player.GetNextHPForConvoyWithBigContainer(this.Location, ref this.HPNumber);
                        if (HPNumber == -10)
                        {
                            this.Delete();
                            this.State = ConvoyState.Deleted;
                            return;
                        }
                        //���� ������, �� ��� �� AZN
                        if (this.Stock == 0)
                        {
                            this.APoint = _player.GetNearestAZNPoint(this.Location, this.HPoint);
                            this.Path = _player.Pathfinder.FindPath(this.Location, this.APoint);
                            this.Move();
                            return;
                        }
                        //����� ����� � HP
                        else
                        {
                            if (this.Location == this.HPoint)
                                return;
                            this.Path = _player.Pathfinder.FindPath(this.Location, this.HPoint);
                            this.Move();
                            return;
                        }
                    }
                    else
                    {
                        //��������, ��� ������ ���������
                        _player.NavigationPoints[HPNumber].Complete = true;
                        //����������� ����� HP
                        this.HPoint = _player.GetNextHPForConvoyWithBigContainer(this.Location, ref this.HPNumber);
                        if (HPNumber == -10)
                        {
                            this.Delete();
                            this.State = ConvoyState.Deleted;
                            return;
                        }
                        //���� ������, �� ��� �� AZN
                        if (this.Stock == 0)
                        {
                            this.APoint = _player.GetNearestAZNPoint(this.Location, this.HPoint);
                            this.Path = _player.Pathfinder.FindPath(this.Location, this.APoint);
                            this.Move();
                            return;
                        }
                        //����� ����� � HP
                        else
                        {
                            if (this.Location == this.HPoint)
                                return;
                            this.Path = _player.Pathfinder.FindPath(this.Location, this.HPoint);
                            this.Move();
                            return;
                        }
                    }
                    //
                    /*
                    //���� ����� �� ���� ��� ����������
                    if (_player.CurrentTurn > _player.NavigationPoints[HPNumber].StartTurn)
                    {
                        //��������, ��� ������ ���������
                        _player.NavigationPoints[HPNumber].Complete = true;
                        //����������� ����� HP
                        this.HPoint = _player.GetNextHPForConvoyWithBigContainer(this.Location, ref this.HPNumber);
                        if (HPNumber == -10)
                        {
                            this.Delete();
                            this.State = ConvoyState.Deleted;
                            return;
                        }
                        //���� ������, �� ��� �� AZN
                        if (this.Stock == 0)
                        {
                            this.APoint = _player.GetNearestAZNPoint(this.Location, this.HPoint);
                            this.Path = _player.Pathfinder.FindPath(this.Location, this.APoint);
                            this.Move();
                            return;
                        }
                        //����� ����� � HP
                        else
                        {
                            if (this.Location == this.HPoint)
                                return;
                            this.Path = _player.Pathfinder.FindPath(this.Location, this.HPoint);
                            this.Move();
                            return;
                        }
                    }
                    */
                }

                //���� �� ����� ���
                //���� ������, �� �� AZN
                if (this.Stock == 0)
                {
                    this.APoint = _player.GetNearestAZNPoint(this.Location, this.HPoint);
                    this.Path = _player.Pathfinder.FindPath(this.Location, this.APoint);
                    this.Move();
                    return;
                }
                //����� �� HP
                else
                {
                    if (this.Location == this.HPoint)
                        return;
                    this.Path = _player.Pathfinder.FindPath(this.Location, this.HPoint);
                    this.Move();
                    return;
                }
            }
        }

        //���������� AZN
        private void Transfert()
        {
            if (MyContainer != null)
                MyContainer.TransferTo(this.Location, MyContainer.Stock / MyContainer.CollectTransfertSpeed);
        }

        //���������
        private void Move()
        {
            if (MyDefender != null)
            {
                MyDefender.StopMoving();
                MyDefender.MoveTo(this.Path);
            }
            if (MyContainer != null)
            {
                MyContainer.StopMoving();
                MyContainer.MoveTo(this.Path);
            }
        }

        //��������� AZN
        private void FillContainers()
        {
            if (MyContainer != null)
                MyContainer.CollectFrom(this.Location, MyContainer.ContainerCapacity / MyContainer.CollectTransfertSpeed);
        }

        //���������� ������� ��������� ������
        private void SetState(MyAI _player)
        {
            if (State == ConvoyState.Deleted)
                return;

            //��������, ���� �� ����
            if (MyDefender != null && MyDefender.HitPoint <= 0)
                MyDefender = null;
            if (MyContainer != null && MyContainer.HitPoint <= 0)
                MyContainer = null;

            //���� ����� ���, �� ������ ���������
            if (MyDefender == null && MyContainer == null)
            {
                this.State = ConvoyState.UnderConstruction;
                this.MyDefender = null;
                this.MyContainer = null;
                this.DefendersNumber = 0;
                this.ContainersNumber = 0;
                this.HPNumber = -1;
                return;
            }

            //��������� ������ ��� Stock � Location
            this.Stock = 0;
            if (MyDefender != null)
            {
                this.Location = MyDefender.Location;
            }
            if (MyContainer != null)
            {
                this.Location = MyContainer.Location;
                this.Stock += MyContainer.Stock;
            }

            //���� HP �� ��������, �� ����������
            if (this.HPNumber == -1)
            {
                //this.HPoint = _player.GetNearestHPForConvoy(this.Location, ref HPNumber);

                this.HPoint = _player.GetNextHPForConvoyWithBigContainer(this.Location, ref this.HPNumber);
                if (HPNumber == -10)
                {
                    this.Delete();
                    this.State = ConvoyState.Deleted;
                    return;
                }
            }

            if (this.HPNumber >= 0)
            {
                if (_player.NavigationPoints[HPNumber].Complete == true)
                {
                    this.HPoint = _player.GetNextHPForConvoyWithBigContainer(this.Location, ref this.HPNumber);
                    if (HPNumber == -10)
                    {
                        this.Delete();
                        this.State = ConvoyState.Deleted;
                        return;
                    }
                    this.Stop();
                }
            }

            //���� Defender � ���, �� ������ � ������ � ���
            if (MyDefender != null && MyDefender.State == NanoBotState.Defending)
            {
                this.State = ConvoyState.Fighting;
                return;
            }

            //������-������ ���������� ��-�� ����, ��� ������, ��� ���, � ��� deadlink
            if (MyDefender != null)
            {
                if (MyContainer != null)
                {
                    if ((MyDefender.State == NanoBotState.WaitingOrders)
                        && (MyContainer.State == NanoBotState.WaitingOrders))
                    {
                        this.State = ConvoyState.Waiting;
                    }
                    if ((MyDefender.State == NanoBotState.Moving)
                        || (MyContainer.State == NanoBotState.Moving))
                    {
                        this.State = ConvoyState.Moving;
                    }
                    if ((MyContainer.State == NanoBotState.Collecting))
                    {
                        this.State = ConvoyState.CollectingAZN;
                    }
                    if ((MyContainer.State == NanoBotState.TransferingStock))
                    {
                        this.State = ConvoyState.TransferingAZN;
                    }
                }
            }
            else
            {
                if (MyContainer != null)
                {
                    if ((MyContainer.State == NanoBotState.WaitingOrders))
                    {
                        this.State = ConvoyState.Waiting;
                    }
                    if ((MyContainer.State == NanoBotState.Moving))
                    {
                        this.State = ConvoyState.Moving;
                    }
                    if ((MyContainer.State == NanoBotState.Collecting))
                    {
                        this.State = ConvoyState.CollectingAZN;
                    }
                    if ((MyContainer.State == NanoBotState.TransferingStock))
                    {
                        this.State = ConvoyState.TransferingAZN;
                    }
                }
                else if (MyContainer == null)
                {
                    this.State = ConvoyState.UnderConstruction;
                    this.MyDefender = null;
                    this.MyContainer = null;
                    this.ContainersNumber = 0;
                    this.DefendersNumber = 0;
                    this.HPNumber = -1;
                }
            }
        }

        //������������� ������
        private void Stop()
        {
            if (MyDefender != null)
            {
                MyDefender.StopMoving();
            }
            if (MyContainer != null)
            {
                MyContainer.StopMoving();
            }
            this.State = ConvoyState.Waiting;
        }

        //���������, ������ �� ���� ��� ���.
        private bool NeedGathering()
        {
            bool res = false;
            if (MyDefender != null)
                if (MyDefender.Location != this.Location)
                    res = true;
            if (MyContainer != null)
                if (MyContainer.Location != this.Location)
                    res = true;
            return res;
        }

        //��������� ������, ���� �� �����-�� �������� ��-���� �����������.
        //������������ �� �����. ����� ������������ ������ � ������ �����,
        //���� ����� ����� ������ �������, � ����� ���.
        private void Gather(MyAI _player)
        {
            if (MyDefender != null)
            {
                MyDefender.StopMoving();
                MyDefender.MoveTo(_player.Pathfinder.FindPath(MyDefender.Location, this.Location));
            }
            if (MyContainer != null)
            {
                MyContainer.StopMoving();
                MyContainer.MoveTo(_player.Pathfinder.FindPath(MyContainer.Location, this.Location));
            }
        }
    }
}