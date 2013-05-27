using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using VG.Common;
//using System.Diagnostics;

namespace Anganar
{
    //Стоит и отстреливается, если что.
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
            //Запоминаем, на каком HP мы стоим
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
            //Проверяем, где враги
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
            //Если кто-то достаточно близко - стреляем
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
            //Если кто-то достаточно близко - стреляем
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

    //Ходит по NavigationPoint`ам
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
            //NPNumber == -10 означает, что все NavigationObjectives уже выполнены
            if (this.NPNumber == -10)
            {
                this.ForceAutoDestruction();
                return;
            }

            //NPNumber == -1 означает, что навигатор только появился, и ему надо назначить цель
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

            //Если вдруг обнаруживаем, что цель, к которой идём, уже кем-то посещена, то запрашиваем новую и останавливаемся.
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

            //Если мы стоим на цели в нужное время, то помечаем её как выполненную и запрашиваем новую.
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

            //Если мы стоим, причём не на своей текущей цели, то идём к ней самой.
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

    //Защищает цели, указанные в списке BTargets
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
            //Если цели ещё нет, то запрашиваем её.
            if (this.Target.X == -1)
            {
                this.Target = _player.GetBattleTarget(this.Location);
            }

            //Проверяем список противников. Если кто-то достаточно близко, останавливаемся.
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
                //Ищем ближайшую цель
                MinDistance = 1000;
                if (_player.OtherNanoBotsInfo != null)
                {
                    foreach (NanoBotInfo botEnemy in _player.OtherNanoBotsInfo)
                    {
                        if (botEnemy.PlayerID == 0)
                        {
                            this.Distance = _player.GeomDist(botEnemy.Location, this.Location);
                            //Если вражеский AI достаточно близко, то он пользуется абсолютным приоритетом!
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
                //Если противник близко, то стреляем в него.
                if (MinDistance < this.DefenseDistance)
                {
                    this.DefendTo(ShootAt, 3);
                    return;
                }
                //Если не дошли до цели - идём к ней.
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

    //Атакует цели, указанные в списке ATargets
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
            //Если цели ещё нет, то запрашиваем её.
            if (this.Target.X == -1)
            {
                this.Target = _player.GetTargetToAtack(this.Location);
            }

            //Проверяем список противников. Если кто-то достаточно близко, останавливаемся.
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
                //Ищем ближайшую цель
                MinDistance = 1000;
                if (_player.OtherNanoBotsInfo != null)
                {
                    foreach (NanoBotInfo botEnemy in _player.OtherNanoBotsInfo)
                    {
                        if (botEnemy.PlayerID == 0)
                        {
                            this.Distance = _player.GeomDist(botEnemy.Location, this.Location);
                            //Если вражеский AI достаточно близко, то он пользуется абсолютным приоритетом!
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
                //Если противник близко, то стреляем в него.
                if (MinDistance < this.DefenseDistance)
                {
                    this.DefendTo(ShootAt, 3);
                    return;
                }

                //Идём к цели.
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

    //Входит в состав конвоев
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
         * Всё управление осуществляется из объекта "Convoy", в который данный контейнер входит.
         */
        public ConvoyContainer()
        {
            ConvoyNumber = -1;
        }

        public int ConvoyNumber;

        //Эту функцию вызывают извне при записывании контейнера в конвой
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

    //Входить в состав конвоев
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
         * Всё управление осуществляется из объекта "Convoy", в который данный дефендер входит.
         */
        public ConvoyDefender()
        {
            ConvoyNumber = -1;
        }

        public int ConvoyNumber;

        //Эту функцию вызывают извне при записывании дефендера в конвой
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

    //Входить в состав конвоев
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
         * Всё управление осуществляется из объекта "Convoy", в который данный дефендер входит.
         */
        public ConvoyDefender2()
        {
            ConvoyNumber = -1;
        }

        public int ConvoyNumber;

        //Эту функцию вызывают извне при записывании дефендера в конвой
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

    //Входит в состав конвоев
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
         * Всё управление осуществляется из объекта "Convoy", в который данный контейнер входит.
         */
        public ConvoyCollector()
        {
            ConvoyNumber = -1;
        }

        public int ConvoyNumber;

        //Эту функцию вызывают извне при записывании контейнера в конвой
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

    //Входит в состав конвоев
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
         * Всё управление осуществляется из объекта "Convoy", в который данный контейнер входит.
         */
        public BigConvoyContainer()
        {
            ConvoyNumber = -1;
        }

        public int ConvoyNumber;

        //Эту функцию вызывают извне при записывании контейнера в конвой
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

    //Входить в состав конвоев
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
         * Всё управление осуществляется из объекта "Convoy", в который данный дефендер входит.
         */
        public ConvoyDefender3()
        {
            ConvoyNumber = -1;
        }

        public int ConvoyNumber;

        //Эту функцию вызывают извне при записывании дефендера в конвой
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

    //Ходит вместе с AI
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

        //Отвечает за анализ обстановки и стрельбу.
        //Возвращает ответ на вопрос "Грозит ли AI опасность?"
        public bool Alarm(MyAI _player)
        {
            bool ahtung = false;
            double Distance;
            double MinDistance;
            Point ShootAt = new Point();

            //Если в пределах досягаемости кто-то есть, останавливаемся
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
                            //И запоминаем, что надо предупредить AI об опасности.
                            ahtung = true;
                        }
                    }
                }
            }
            
            //Если стоим
            if (this.State == NanoBotState.WaitingOrders)
            {
                //Ищем ближайшую цель
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
                //Если она достаточно близко, стреляем и завершаем действия
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

    //Ходит за AI. Пока что то, что он далеко видит, не нужно, но есть планы.
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
            //Проверяем список противников. Если кто-то достаточно близко, останавливаемся.
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

            //NPNumber == -1 означает, что навигатор только появился, и ему надо назначить цель
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
                //Если вдруг обнаруживаем, что цель, к которой идём, уже кем-то посещена, то запрашиваем новую и останавливаемся.
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
                //Если мы стоим на цели в нужное время, то помечаем её как выполненную и запрашиваем новую.
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
                    //Ищем ближайшую цель
                    MinDistance = 1000;
                    if (_player.OtherNanoBotsInfo != null)
                    {
                        foreach (NanoBotInfo botEnemy in _player.OtherNanoBotsInfo)
                        {
                            if (botEnemy.PlayerID == 0)
                            {
                                this.Distance = _player.GeomDist(botEnemy.Location, this.Location);
                                //Если вражеский AI достаточно близко, то он пользуется абсолютным приоритетом!
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
                    //Если противник близко, то стреляем в него.
                    if (MinDistance < this.DefenseDistance)
                    {
                        this.DefendTo(ShootAt, 3);
                        return;
                    }
                }

                //Если все NP посещены, то стоим
                if (this.NPNumber == -10)
                {
                    this.ForceAutoDestruction();
                    return;
                }

                //Если мы на AZNPoint и нужна дозаправка, то дозаправляемся
                if (this.Location == this.APoint)
                {
                    if (this.Stock == 0)
                    {
                        CollectFrom(Location, this.ContainerCapacity / this.CollectTransfertSpeed);
                        return;
                    }
                }
                //Если пустые, то находим AZN и идём за ним
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

    //На данный момент не используется
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

    //На данный момент не используется
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

    //На данный момент не используется
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
                                //Добавить возможность "дозаправки"!!!
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
                //Добавить возможность "дозаправки"!!!
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

    //На данный момент не используется
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
                                //Добавить возможность "дозаправки"!!!
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
                //Добавить возможность "дозаправки"!!!
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