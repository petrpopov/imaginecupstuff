using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using VG.Common;
//using System.Diagnostics;

namespace Anganar
{
    //����� � ��������������, ���� ���.
    #region Needle
    [Characteristics(ContainerCapacity = 100,
     CollectTransfertSpeed = 0,
     Scan = 5,
     MaxDamage = 5,
     DefenseDistance = 10,
     Constitution = 30)]
    class Needle : NanoNeedle
    {
        public Needle() { }

        public int HPNumber = -1;
        double Distance;
        double MinDistance;
        Point ShootAt = new Point();

        public void Action(MyAI _player)
        {
            //����������, �� ����� HP �� �����
            if (HPNumber == -1)
            {
                for (int i = 0; i < _player.HoshimiPoints.Length; i++)
                {
                    if (_player.HoshimiPoints[i].Location == this.Location)
                    {
                        HPNumber = i;
                    }
                }
            }
            //���������, ��� �����
            #region old
            /*
             *  MinDistance = 1000;
            if (_player.OtherNanoBotsInfo != null)
            {
                foreach (NanoBotInfo botEnemy in _player.OtherNanoBotsInfo)
                {
                    if (botEnemy.PlayerID == 0)
                    {
                        Distance = _player.GeomDist(botEnemy.Location, this.Location);
                        if (Distance < MinDistance)
                        {
                            MinDistance = Distance;
                            ShootAt = botEnemy.Location;
                        }
                    }
                }
            }
            //���� ���-�� ���������� ������ - ��������
            if (MinDistance < DefenseDistance)
            {
             this.DefendTo(ShootAt, 4);      
              return;
            }
             * */
            #endregion
            #region new
            MinDistance = 1000;
            if (_player.OtherNanoBotsInfo != null)
            {
                foreach (NanoBotInfo botEnemy in _player.OtherNanoBotsInfo)
                {
                    if (botEnemy.PlayerID == 0)
                    {
                        Distance = _player.GeomDist(botEnemy.Location, this.Location);
                        Distance += Utils.DefenseLength;
                        if (Distance < MinDistance)
                        {
                            MinDistance = Distance;
                            ShootAt = botEnemy.Location;
                        }
                    }
                }
            }
            //���� ���-�� ���������� ������ - ��������
            if (MinDistance < DefenseDistance+Utils.DefenseLength)
            {
                if (MinDistance < DefenseDistance)
                {
                    this.DefendTo(ShootAt, 3);
                    return;
                }
                else
                {
                    if (MinDistance >= DefenseDistance && MinDistance < DefenseDistance + Utils.DefenseLength)
                    {
                        Point p = _player.GetTargetOnVector(this.Location, ShootAt, DefenseDistance);
                        this.DefendTo(p, 3);
                        return;
                    }
                }
                
            }
            #endregion
        }

    }
    #endregion

    //����� �� NavigationPoint`��
    #region Navigator
    [Characteristics(ContainerCapacity = 0,
       CollectTransfertSpeed = 0,
       Scan = 20,
       MaxDamage = 0,
       DefenseDistance = 0,
       Constitution = 20)]
    class Navigator : VG.Common.NanoExplorer
    {
        public Navigator() { }

        public int NPNumber = -1;
        public Point NPoint = new Point(-1, -1);

        public void Action(MyAI _player)
        {
            //NPNumber == -10 ��������, ��� ��� NavigationObjectives ��� ���������
            if (this.NPNumber == -10)
            {
                this.ForceAutoDestruction();
                return;
            }

            //NPNumber == -1 ��������, ��� ��������� ������ ��������, � ��� ���� ��������� ����
            if (this.NPNumber == -1)
            {
                //this.NPoint = _player.GetNearestUndoneNavPoint(this.Location, ref this.NPNumber);
                this.NPoint = _player.GetNextUndoneNavPoint(this.Location, ref this.NPNumber);
                if (this.NPNumber == -10)
                {
                    this.ForceAutoDestruction();
                    return;
                }
            }

            //���� ����� ������������, ��� ����, � ������� ���, ��� ���-�� ��������, �� ����������� ����� � ���������������.
            if (_player.NavigationPoints[NPNumber].Complete == true)
            {
                //this.NPoint = _player.GetNearestUndoneNavPoint(this.Location, ref this.NPNumber);
                this.NPoint = _player.GetNextUndoneNavPoint(this.Location, ref this.NPNumber);
                if (this.NPNumber == -10)
                {
                    this.ForceAutoDestruction();
                    return;
                }
                this.StopMoving();
            }

            //���� �� ����� �� ���� � ������ �����, �� �������� � ��� ����������� � ����������� �����.
            if ((this.Location == this.NPoint)
                    && (_player.CurrentTurn > _player.NavigationPoints[NPNumber].StartTurn)
                    && (_player.CurrentTurn < _player.NavigationPoints[NPNumber].EndTurn))
            {
                _player.NavigationPoints[NPNumber].Complete = true;
                this.NPoint = _player.GetNextUndoneNavPoint(this.Location, ref this.NPNumber);
                if (this.NPNumber == -10)
                {
                    this.ForceAutoDestruction();
                    return;
                }
            }

            //���� �� �����, ������ �� �� ����� ������� ����, �� ��� � ��� �����.
            if (this.State == NanoBotState.WaitingOrders)
            {
                if (this.Location != this.NPoint)
                {
                    //MoveTo(NPoint);
                    MoveTo(_player.ePathfinder.FindPath(this.Location, NPoint));
                    return;
                }
            }
        }
    }
    #endregion

    //�������� ����, ��������� � ������ BTargets
    #region Protector
    [Characteristics(ContainerCapacity = 0,
    CollectTransfertSpeed = 0,
    Scan = 5,
    MaxDamage = 5,
    DefenseDistance = 12,
    Constitution = 28)]
    class Protector : NanoCollector 
    {
        public Protector() { }

        public Point Target = new Point(-1, -1);
        Point ShootAt = new Point(-1, -1);
        double Distance;
        double MinDistance;

        public void Action(MyAI  _player)
        {
            //���� ���� ��� ���, �� ����������� �.
            if (this.Target.X == -1)
            {
                this.Target = _player.GetBattleTarget(this.Location);
            }

            //��������� ������ �����������. ���� ���-�� ���������� ������, ���������������.
            if (_player.OtherNanoBotsInfo != null)
            {
                foreach (NanoBotInfo botEnemy in _player.OtherNanoBotsInfo)
                {
                    if (botEnemy.PlayerID == 0)
                    {
                        Distance = _player.GeomDist(botEnemy.Location, this.Location);
                        if (Distance < this.DefenseDistance)
                        {
                            this.StopMoving();
                        }
                    }
                }
            }

			if (this.State == NanoBotState.WaitingOrders)
			{
                //���� ��������� ����
                MinDistance = 1000;
                if (_player.OtherNanoBotsInfo != null)
                {
                    foreach (NanoBotInfo botEnemy in _player.OtherNanoBotsInfo)
                    {
                        if (botEnemy.PlayerID == 0)
                        {
                            this.Distance = _player.GeomDist(botEnemy.Location, this.Location);
                            //���� ��������� AI ���������� ������, �� �� ���������� ���������� �����������!
                            if ((botEnemy.NanoBotType == NanoBotType.NanoAI) && (Distance < this.DefenseDistance))
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
                }
                //���� ��������� ������, �� �������� � ����.
                if (MinDistance < this.DefenseDistance)
                {
                    this.DefendTo(ShootAt, 3);
                    return;
                }
                //���� �� ����� �� ���� - ��� � ���.
                if (this.Target != this.Location)
                {
                    //string str = "Defender[" + this.ID.ToString() +"] X: " + this.Location.X.ToString() + " Y: " + this.Location.Y.ToString() + "\n";
                    //Debugger.Log(2, "Local", str);
                    MoveTo(_player.Pathfinder.FindPath(this.Location, Target));
                    return;
                }
			}
        }
    }
    #endregion

    //������� ����, ��������� � ������ ATargets
    #region Atacker
    [Characteristics(ContainerCapacity = 0,
    CollectTransfertSpeed = 0,
    Scan = 5,
    MaxDamage = 5,
    DefenseDistance = 12,
    Constitution = 28)]
    class Atacker : NanoCollector
    {
        public Atacker() { }

        public Point Target = new Point(-1, -1);
        Point ShootAt = new Point(-1, -1);
        double Distance;
        double MinDistance;

        public void Action(MyAI _player)
        {
            //���� ���� ��� ���, �� ����������� �.
            if (this.Target.X == -1)
            {
                this.Target = _player.GetTargetToAtack(this.Location);
            }

            //��������� ������ �����������. ���� ���-�� ���������� ������, ���������������.
            if (_player.OtherNanoBotsInfo != null)
            {
                foreach (NanoBotInfo botEnemy in _player.OtherNanoBotsInfo)
                {
                    if (botEnemy.PlayerID == 0)
                    {
                        Distance = _player.GeomDist(botEnemy.Location, this.Location);
                        if (Distance < this.DefenseDistance)
                        {
                            this.StopMoving();
                        }
                    }
                }
            }

            if (this.State == NanoBotState.WaitingOrders)
            {
                //���� ��������� ����
                MinDistance = 1000;
                if (_player.OtherNanoBotsInfo != null)
                {
                    foreach (NanoBotInfo botEnemy in _player.OtherNanoBotsInfo)
                    {
                        if (botEnemy.PlayerID == 0)
                        {
                            this.Distance = _player.GeomDist(botEnemy.Location, this.Location);
                            //���� ��������� AI ���������� ������, �� �� ���������� ���������� �����������!
                            if ((botEnemy.NanoBotType == NanoBotType.NanoAI) && (Distance < this.DefenseDistance))
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
                }
                //���� ��������� ������, �� �������� � ����.
                if (MinDistance < this.DefenseDistance)
                {
                    this.DefendTo(ShootAt, 3);
                    return;
                }

                //��� � ����.
                if (this.Target != this.Location)
                {
                    //string str = "Atacker[" + this.ID.ToString() +"] X: " + this.Location.X.ToString() + " Y: " + this.Location.Y.ToString() + "\n";
                    //Debugger.Log(2, "Local", str);
                    //MoveTo(Target);
                    MoveTo(_player.Pathfinder.FindPath(this.Location, Target));
                    return;
                }
            }
        }
    }
    #endregion

    //������ � ������ �������
    #region ConvoyContainer
    [Characteristics(ContainerCapacity = 50,
     CollectTransfertSpeed = 5,
     Scan = 0,
     MaxDamage = 0,
     DefenseDistance = 0,
     Constitution = 15)]
    class ConvoyContainer : VG.Common.NanoContainer
    {
        /*
         * �� ���������� �������������� �� ������� "Convoy", � ������� ������ ��������� ������.
         */
        public ConvoyContainer()
        {
            ConvoyNumber = -1;
        }

        public int ConvoyNumber;

        //��� ������� �������� ����� ��� ����������� ���������� � ������
        public void SetConvoyNumber(MyAI _player)
        {
            ConvoyNumber = _player.GetConvoyNumber();
            if (ConvoyNumber != -1)
            {
                _player.Convoys[ConvoyNumber].Containers++;
                _player.Convoys[ConvoyNumber].AddBot(this);
            }
            //string str = "Container belongs to convoy " + this.ConvoyNumber.ToString() + "\n";
            //Debugger.Log(2, "Local", str);
        }
    }
    #endregion

    //������� � ������ �������
    #region ConvoyDefender
    [Characteristics(ContainerCapacity = 0,
    CollectTransfertSpeed = 0,
    Scan = 5,
    MaxDamage = 5,
    DefenseDistance = 12,
    Constitution = 28)]
    class ConvoyDefender : NanoCollector
    {
        /*
         * �� ���������� �������������� �� ������� "Convoy", � ������� ������ �������� ������.
         */
        public ConvoyDefender()
        {
            ConvoyNumber = -1;
        }

        public int ConvoyNumber;

        //��� ������� �������� ����� ��� ����������� ��������� � ������
        public void SetConvoyNumber(MyAI _player)
        {
            ConvoyNumber = _player.GetConvoyNumber();
            if (ConvoyNumber != -1)
            {
                _player.Convoys[ConvoyNumber].Defenders++;
                _player.Convoys[ConvoyNumber].AddBot(this);
            }
            //string str = "Defender belongs to convoy " + this.ConvoyNumber.ToString() + "\n";
            //Debugger.Log(2, "Local", str);
        }
    }
    #endregion

    //������� � ������ �������
    #region ConvoyDefender2
    [Characteristics(ContainerCapacity = 0,
    CollectTransfertSpeed = 0,
    Scan = 5,
    MaxDamage = 5,
    DefenseDistance = 12,
    Constitution = 28)]
    class ConvoyDefender2 : NanoCollector
    {
        /*
         * �� ���������� �������������� �� ������� "Convoy", � ������� ������ �������� ������.
         */
        public ConvoyDefender2()
        {
            ConvoyNumber = -1;
        }

        public int ConvoyNumber;

        //��� ������� �������� ����� ��� ����������� ��������� � ������
        public void SetConvoyNumber(MyAI _player)
        {
            ConvoyNumber = _player.GetConvoyNumberForConvoyWithCollector();
            if (ConvoyNumber != -1)
            {
                _player.CollConvoys[ConvoyNumber].Defenders++;
                _player.CollConvoys[ConvoyNumber].AddBot(this);
            }
            //string str = "Defender belongs to convoy " + this.ConvoyNumber.ToString() + "\n";
            //Debugger.Log(2, "Local", str);
        }
    }
    #endregion

    //������ � ������ �������
    #region ConvoyCollector
    [Characteristics(ContainerCapacity = 20,
     CollectTransfertSpeed = 5,
     Scan = 0,
     MaxDamage = 0,
     DefenseDistance = 0,
     Constitution = 25)]
    class ConvoyCollector : VG.Common.NanoCollector
    {
        /*
         * �� ���������� �������������� �� ������� "Convoy", � ������� ������ ��������� ������.
         */
        public ConvoyCollector()
        {
            ConvoyNumber = -1;
        }

        public int ConvoyNumber;

        //��� ������� �������� ����� ��� ����������� ���������� � ������
        public void SetConvoyNumber(MyAI _player)
        {
            ConvoyNumber = _player.GetConvoyNumberForConvoyWithCollector();
            if (ConvoyNumber != -1)
            {
                _player.CollConvoys[ConvoyNumber].Containers++;
                _player.CollConvoys[ConvoyNumber].AddBot(this);
            }
            //string str = "Container belongs to convoy " + this.ConvoyNumber.ToString() + "\n";
            //Debugger.Log(2, "Local", str);
        }
    }
    #endregion

    //������ � ������ �������
    #region BigConvoyContainer
    [Characteristics(ContainerCapacity = 60,
     CollectTransfertSpeed = 5,
     Scan = 0,
     MaxDamage = 0,
     DefenseDistance = 0,
     Constitution = 5)]
    class BigConvoyContainer : VG.Common.NanoContainer
    {
        /*
         * �� ���������� �������������� �� ������� "Convoy", � ������� ������ ��������� ������.
         */
        public BigConvoyContainer()
        {
            ConvoyNumber = -1;
        }

        public int ConvoyNumber;

        //��� ������� �������� ����� ��� ����������� ���������� � ������
        public void SetConvoyNumber(MyAI _player)
        {
            ConvoyNumber = _player.GetConvoyNumberForConvoyWithBigContainer();
            if (ConvoyNumber != -1)
            {
                _player.BigConvoys[ConvoyNumber].Containers++;
                _player.BigConvoys[ConvoyNumber].AddBot(this);
            }
            //string str = "Container belongs to convoy " + this.ConvoyNumber.ToString() + "\n";
            //Debugger.Log(2, "Local", str);
        }
    }
    #endregion

    //������� � ������ �������
    #region ConvoyDefender3
    [Characteristics(ContainerCapacity = 0,
    CollectTransfertSpeed = 0,
    Scan = 5,
    MaxDamage = 5,
    DefenseDistance = 12,
    Constitution = 28)]
    class ConvoyDefender3 : NanoCollector
    {
        /*
         * �� ���������� �������������� �� ������� "Convoy", � ������� ������ �������� ������.
         */
        public ConvoyDefender3()
        {
            ConvoyNumber = -1;
        }

        public int ConvoyNumber;

        //��� ������� �������� ����� ��� ����������� ��������� � ������
        public void SetConvoyNumber(MyAI _player)
        {
            ConvoyNumber = _player.GetConvoyNumberForConvoyWithBigContainer();
            if (ConvoyNumber != -1)
            {
                _player.BigConvoys[ConvoyNumber].Defenders++;
                _player.BigConvoys[ConvoyNumber].AddBot(this);
            }
            //string str = "Defender belongs to convoy " + this.ConvoyNumber.ToString() + "\n";
            //Debugger.Log(2, "Local", str);
        }
    }
    #endregion

    //����� ������ � AI
    #region BodyGuard
    [Characteristics(ContainerCapacity = 0,
    CollectTransfertSpeed = 0,
    Scan = 5,
    MaxDamage = 5,
    DefenseDistance = 12,
    Constitution = 28)]
    class BodyGuard : NanoCollector
    {
        public BodyGuard()
        {
            registered = false;
        }

        public bool registered;

        //�������� �� ������ ���������� � ��������.
        //���������� ����� �� ������ "������ �� AI ���������?"
        public bool Alarm(MyAI _player)
        {
            bool ahtung = false;
            double Distance;
            double MinDistance;
            Point ShootAt = new Point();

            //���� � �������� ������������ ���-�� ����, ���������������
            if (_player.OtherNanoBotsInfo != null)
            {
                foreach (NanoBotInfo botEnemy in _player.OtherNanoBotsInfo)
                {
                    if (botEnemy.PlayerID == 0)
                    {
                        Distance = _player.GeomDist(botEnemy.Location, this.Location);
                        if (Distance <= this.DefenseDistance)
                        {
                            this.StopMoving();
                            //� ����������, ��� ���� ������������ AI �� ���������.
                            ahtung = true;
                        }
                    }
                }
            }
            
            //���� �����
            if (this.State == NanoBotState.WaitingOrders)
            {
                //���� ��������� ����
                MinDistance = 1000;
                if (_player.OtherNanoBotsInfo != null)
                {
                    foreach (NanoBotInfo botEnemy in _player.OtherNanoBotsInfo)
                    {
                        if (botEnemy.PlayerID == 0)
                        {
                            Distance = _player.GeomDist(botEnemy.Location, this.Location);
                            if ((botEnemy.NanoBotType == NanoBotType.NanoAI) && (Distance < this.DefenseDistance))
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
                }
                //���� ��� ���������� ������, �������� � ��������� ��������
                if (MinDistance <= this.DefenseDistance)
                {
                    this.DefendTo(ShootAt, 3);
                    return ahtung;
                }
            }
            return ahtung;
        }
    }
    #endregion

    //����� �� AI. ���� ��� ��, ��� �� ������ �����, �� �����, �� ���� �����.
    #region Observer
    [Characteristics(ContainerCapacity = 0,
       CollectTransfertSpeed = 0,
       Scan = 30,
       MaxDamage = 0,
       DefenseDistance = 0,
       Constitution = 10)]
    class Observer : VG.Common.NanoExplorer
    {
        public Observer() { }

        public void Action(MyAI _player)
        {
            this.MoveTo(_player.AI.Location);
        }
    }
    #endregion

    #region Doctor
    [Characteristics(ContainerCapacity = 10,
     CollectTransfertSpeed = 1,
     Scan = 2,
     MaxDamage = 5,
     DefenseDistance = 12,
     Constitution = 20)]
    class Doctor : VG.Common.NanoCollector
    {
        public Doctor() { }

        public int NPNumber = -1;
        public Point NPoint = new Point(-1, -1);
        public Point APoint = new Point(-1, -1);

        Point ShootAt = new Point(-1, -1);
        double Distance;
        double MinDistance;
        bool Fire = false;

        public void Action(MyAI _player)
        {
            Fire = false;
            //��������� ������ �����������. ���� ���-�� ���������� ������, ���������������.
            if (_player.OtherNanoBotsInfo != null)
            {
                foreach (NanoBotInfo botEnemy in _player.OtherNanoBotsInfo)
                {
                    if (botEnemy.PlayerID == 0)
                    {
                        Distance = _player.GeomDist(botEnemy.Location, this.Location);
                        if (Distance < this.DefenseDistance)
                        {
                            this.StopMoving();
                            Fire = true;
                        }
                    }
                }
            }

            //NPNumber == -1 ��������, ��� ��������� ������ ��������, � ��� ���� ��������� ����
            if (this.NPNumber == -1)
            {
                this.NPoint = _player.GetNextUndoneHealPoint(this.Location, ref this.NPNumber);
                if (this.NPNumber == -10)
                {
                    this.ForceAutoDestruction();
                    return;
                }
            }
        
            if (NPNumber >= 0)
            {
                //���� ����� ������������, ��� ����, � ������� ���, ��� ���-�� ��������, �� ����������� ����� � ���������������.
                if (_player.NavigationPoints[NPNumber].Complete == true)
                {
                    this.NPoint = _player.GetNextUndoneHealPoint(this.Location, ref this.NPNumber);
                    if (this.NPNumber == -10)
                    {
                        this.ForceAutoDestruction();
                        return;
                    }
                    this.StopMoving();
                }
            }

            if (NPNumber >= 0)
            {
                //���� �� ����� �� ���� � ������ �����, �� �������� � ��� ����������� � ����������� �����.
                if ((this.Location == this.NPoint)
                        && (_player.CurrentTurn > _player.NavigationPoints[NPNumber].StartTurn)
                        && (_player.CurrentTurn < _player.NavigationPoints[NPNumber].EndTurn)
                        && (this.Stock >= _player.NavigationPoints[NPNumber].Stock))
                {
                    _player.NavigationPoints[NPNumber].Complete = true;
                    this.NPoint = _player.GetNextUndoneHealPoint(this.Location, ref this.NPNumber);
                    if (this.NPNumber == -10)
                    {
                        this.ForceAutoDestruction();
                        return;
                    }
                }
            }

            if (this.State == NanoBotState.WaitingOrders)
            {
                if (Fire)
                {
                    //���� ��������� ����
                    MinDistance = 1000;
                    if (_player.OtherNanoBotsInfo != null)
                    {
                        foreach (NanoBotInfo botEnemy in _player.OtherNanoBotsInfo)
                        {
                            if (botEnemy.PlayerID == 0)
                            {
                                this.Distance = _player.GeomDist(botEnemy.Location, this.Location);
                                //���� ��������� AI ���������� ������, �� �� ���������� ���������� �����������!
                                if ((botEnemy.NanoBotType == NanoBotType.NanoAI) && (Distance < this.DefenseDistance))
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
                    }
                    //���� ��������� ������, �� �������� � ����.
                    if (MinDistance < this.DefenseDistance)
                    {
                        this.DefendTo(ShootAt, 3);
                        return;
                    }
                }

                //���� ��� NP ��������, �� �����
                if (this.NPNumber == -10)
                {
                    this.ForceAutoDestruction();
                    return;
                }

                //���� �� �� AZNPoint � ����� ����������, �� ��������������
                if (this.Location == this.APoint)
                {
                    if (this.Stock == 0)
                    {
                        CollectFrom(Location, this.ContainerCapacity / this.CollectTransfertSpeed);
                        return;
                    }
                }
                //���� ������, �� ������� AZN � ��� �� ���
                if (Stock == 0)
                {
                    this.APoint = _player.GetNearestAZNPoint(this.Location, this.NPoint);
                    MoveTo(_player.Pathfinder.FindPath(this.Location, this.APoint));
                    return;
                }
                else
                {
                    MoveTo(_player.Pathfinder.FindPath(this.Location, this.NPoint));
                    return;
                }
            }
        }
    }
    #endregion

    //�� ������ ������ �� ������������
    #region Explorer
    [Characteristics(ContainerCapacity = 0,
       CollectTransfertSpeed = 0,
       Scan = 30,
       MaxDamage = 0,
       DefenseDistance = 0,
       Constitution = 10)]
    class Explorer : VG.Common.NanoExplorer
    {
        public Explorer() { }

        public int NPNumber = -1;
        public Point NPoint = new Point(-1, -1);

        public void Action(MyAI _player)
        {
            if (this.NPNumber == -1)
            {
                this.NPoint = _player.GetNextUndoneNavPoint(this.Location, ref this.NPNumber);
            }

            if (_player.NavigationPoints[NPNumber].Complete == true)
            {
                this.NPoint = _player.GetNextUndoneNavPoint(this.Location, ref this.NPNumber);
                this.StopMoving();
            }

            if ((this.Location == this.NPoint)
                    && (_player.CurrentTurn > _player.NavigationPoints[NPNumber].StartTurn)
                    && (_player.CurrentTurn < _player.NavigationPoints[NPNumber].EndTurn))
            {
                _player.NavigationPoints[NPNumber].Complete = true;
            }

            if (this.State == NanoBotState.WaitingOrders)
            {
                if (this.Location != this.NPoint)
                {
                    this.MoveTo(NPoint);
                    //this.MoveTo(_player.ePathfinder.FindPath(this.Location,this.NPoint));
                    return;
                }
            }
        }
    }
    #endregion

    //�� ������ ������ �� ������������
    #region Blocker

    [Characteristics(ContainerCapacity = 0,
     CollectTransfertSpeed = 0,
     Scan = 10,
     MaxDamage = 0,
     DefenseDistance = 0,
     Constitution = 80)]
    class Blocker : NanoBlocker
    {
        public Blocker() { }

        bool active = false;

        public void Action(MyAI _player)
        {
            active = false;
            if (_player.OtherNanoBotsInfo != null)
            {
                foreach (NanoBotInfo botEnemy in _player.OtherNanoBotsInfo)
                {
                    if (!((botEnemy.NanoBotType == NanoBotType.NanoBlocker) || (botEnemy.NanoBotType == NanoBotType.NanoNeedle) || (botEnemy.NanoBotType == NanoBotType.NanoNeuroControler)))
                    {
                        if (_player.GeomDist(this.Location, botEnemy.Location) <= Utils.BlockerStrength)
                            active = true;
                    }
                }
            }
            if (_player.OtherInjectionPointsInfo != null)
            {
                foreach (InjectionPointInfo ip in _player.OtherInjectionPointsInfo)
                {
                    if (_player.GeomDist(this.Location, ip.Location) <= Utils.BlockerStrength)
                        active = true;
                }
            }
            if (!active)
                this.ForceAutoDestruction();
        }
    }
    #endregion

    //�� ������ ������ �� ������������
    #region Collector
    [Characteristics(ContainerCapacity = 20,
     CollectTransfertSpeed = 5,
     Scan = 0,
     MaxDamage = 0,
     DefenseDistance = 0,
     Constitution = 25)]
    class Collector : VG.Common.NanoCollector
    {
        public Collector() { }
        public int HPNumber = -1;
        public Point HPoint = new Point(-1, -1);
        public Point APoint = new Point(-1, -1);

        public void Action(MyAI _player)
        {
            if (this.HPNumber == -1)
            {
                this.HPoint = _player.GetNextHoshimiPoint(this.Location, ref this.HPNumber);
            }

            if (this.HPNumber == -10)
            {
                this.HPoint = _player.GetNearestUnfilledHP(this.Location, ref this.HPNumber);
            }

            if (HPNumber >= 0)
            {
                if (_player.HoshimiPoints[HPNumber].Full == 1)
                {
                    this.HPoint = _player.GetNearestUnfilledHP(this.Location, ref this.HPNumber);
                    this.StopMoving();
                }
            }

            if (this.State == NanoBotState.WaitingOrders)
            {
                if (this.Location == this.APoint)
                {
                    if (this.Stock == 0)
                    {
                        //Collect
                        CollectFrom(Location, this.ContainerCapacity / this.CollectTransfertSpeed);
                    }
                    else
                    {
                        //Go Hoshimi point
                        //MoveTo(this.HPoint);
                        MoveTo(_player.Pathfinder.FindPath(this.Location, this.HPoint));
                    }
                }
                if (this.Location == this.HPoint)
                {
                    foreach (NanoBot bot in _player.NanoBots)
                    {
                        if ((bot is Needle) && (bot.Stock == bot.ContainerCapacity) && (bot.Location == this.Location))
                        {
                            if (HPNumber >= 0)
                            {
                                _player.HoshimiPoints[HPNumber].Full = 1;
                                this.HPoint = _player.GetNearestUnfilledHP(this.Location, ref this.HPNumber);
                                //�������� ����������� "����������"!!!
                                if (Stock == 0)
                                {
                                    this.APoint = _player.GetNearestAZNPoint(this.Location, this.HPoint);
                                    //MoveTo(this.APoint);
                                    MoveTo(_player.Pathfinder.FindPath(this.Location, this.APoint));
                                }
                                else
                                {
                                    //this.MoveTo(this.HPoint);
                                    MoveTo(_player.Pathfinder.FindPath(this.Location, this.HPoint));
                                }
                            }
                        }
                    }
                    if (Stock == 0)
                    {
                        //return to azn
                        this.APoint = _player.GetNearestAZNPoint(this.Location, this.HPoint);
                        //MoveTo(this.APoint);
                        MoveTo(_player.Pathfinder.FindPath(this.Location, this.APoint));
                    }
                    else
                    {
                        //transfert
                        TransferTo(Location, this.Stock / this.CollectTransfertSpeed);
                    }
                }
                //�������� ����������� "����������"!!!
                if (Stock == 0)
                {
                    this.APoint = _player.GetNearestAZNPoint(this.Location, this.HPoint);
                    //this.MoveTo(this.APoint);
                    MoveTo(_player.Pathfinder.FindPath(this.Location, this.APoint));
                }
                else
                {
                    //this.HPoint = _player.GetNearestUnfilledHP(this.Location, ref this.HPNumber);
                    //this.MoveTo(this.HPoint);
                    MoveTo(_player.Pathfinder.FindPath(this.Location, this.HPoint));
                }
            }
        }
    }
    #endregion

    //�� ������ ������ �� ������������
    #region Container
    [Characteristics(ContainerCapacity = 50,
     CollectTransfertSpeed = 5,
     Scan = 0,
     MaxDamage = 0,
     DefenseDistance = 0,
     Constitution = 15)]
    class Container : VG.Common.NanoContainer
    {
        public Container()
        {
        }

        public int HPNumber = -1;
        public Point HPoint = new Point(-1, -1);
        public Point APoint = new Point(-1, -1);

        public void Action(MyAI _player)
        {
            if (this.HPNumber == -1)
            {
                this.HPoint = _player.GetNextHoshimiPoint(this.Location, ref this.HPNumber);
            }

            if (this.HPNumber == -10)
            {
                this.HPoint = _player.GetNearestUnfilledHP(this.Location, ref this.HPNumber);
            }

            if (HPNumber >= 0)
            {
                if (_player.HoshimiPoints[HPNumber].Full == 1)
                {
                    this.HPoint = _player.GetNearestUnfilledHP(this.Location, ref this.HPNumber);
                    this.StopMoving();
                }
            }

            if (this.State == NanoBotState.WaitingOrders)
            {
                if (this.Location == this.APoint)
                {
                    if (this.Stock == 0)
                    {
                        //Collect
                        CollectFrom(Location, this.ContainerCapacity / this.CollectTransfertSpeed);
                        return;
                    }
                    else
                    {
                        //Go Hoshimi point
                        //MoveTo(this.HPoint);
                        //string str = "Collector[" + this.ID.ToString() + "] at AP X: " + this.Location.X.ToString() + " Y: " + this.Location.Y.ToString() + "\n";
                        //Debugger.Log(2, "Local", str);
                        MoveTo(_player.Pathfinder.FindPath(this.Location, this.HPoint));
                        return;
                    }
                }
                if (this.Location == this.HPoint)
                {
                    foreach (NanoBot bot in _player.NanoBots)
                    {
                        if ((bot is Needle) && (bot.Stock == bot.ContainerCapacity) && (bot.Location == this.Location))
                        {
                            if (HPNumber >= 0)
                            {
                                _player.HoshimiPoints[HPNumber].Full = 1;
                                this.HPoint = _player.GetNearestUnfilledHP(this.Location, ref this.HPNumber);
                                //�������� ����������� "����������"!!!
                                if (Stock == 0)
                                {
                                    this.APoint = _player.GetNearestAZNPoint(this.Location, this.HPoint);
                                    //MoveTo(this.APoint);
                                    //string str = "Empty collector[" + this.ID.ToString() +"] at finished HP X: " + this.Location.X.ToString() + " Y: " + this.Location.Y.ToString() + "\n";
                                    //Debugger.Log(2, "Local", str);
                                    MoveTo(_player.Pathfinder.FindPath(this.Location, this.APoint));
                                    return;
                                }
                                else
                                {
                                    //this.MoveTo(this.HPoint);
                                    //string str = "Not empty collector[" + this.ID.ToString() +"] at finished HP X: " + this.Location.X.ToString() + " Y: " + this.Location.Y.ToString() + "\n";
                                    //Debugger.Log(2, "Local", str);
                                    MoveTo(_player.Pathfinder.FindPath(this.Location, this.HPoint));
                                    return;
                                }
                            }
                        }
                    }
                    if (Stock == 0)
                    {
                        //return to azn
                        this.APoint = _player.GetNearestAZNPoint(this.Location, this.HPoint);
                        //MoveTo(this.APoint);
                        //string str = "Empty collector[" + this.ID.ToString() +"] at HP X: " + this.Location.X.ToString() + " Y: " + this.Location.Y.ToString() + "\n";
                        //Debugger.Log(2, "Local", str);
                        MoveTo(_player.Pathfinder.FindPath(this.Location, this.APoint));
                        return;
                    }
                    else
                    {
                        //transfert
                        TransferTo(Location, this.Stock / this.CollectTransfertSpeed);
                        return;
                    }
                }
                //�������� ����������� "����������"!!!
                if (Stock == 0)
                {
                    //string str = "Empty collector[" + this.ID.ToString() +"] somewhere X: " + this.Location.X.ToString() + " Y: " + this.Location.Y.ToString() + "\n";
                    //Debugger.Log(2, "Local", str);
                    this.APoint = _player.GetNearestAZNPoint(this.Location, this.HPoint);
                    //this.MoveTo(this.APoint);
                    MoveTo(_player.Pathfinder.FindPath(this.Location, this.APoint));
                    return;
                }
                else
                {
                    //this.HPoint = _player.GetNearestUnfilledHP(this.Location, ref this.HPNumber);
                    //this.MoveTo(this.HPoint);
                    //string str = "Not empty collector[" + this.ID.ToString() +"] somewhere X: " + this.Location.X.ToString() + " Y: " + this.Location.Y.ToString() + "\n";
                    //Debugger.Log(2, "Local", str);
                    MoveTo(_player.Pathfinder.FindPath(this.Location, this.HPoint));
                    return;
                }
            }
        }
    }
    #endregion
}