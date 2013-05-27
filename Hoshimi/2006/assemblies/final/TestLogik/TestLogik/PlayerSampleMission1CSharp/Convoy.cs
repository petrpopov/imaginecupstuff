using System.Drawing;
using System.Collections.Generic;
using VG.Common;

namespace MIPT
{
    //Описывает все возможные состояния конвоя.
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

        //"Регистрация" нового бота в конвое
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

        //Уничтожение конвоя
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


        //Основная функция. Управляет всеми движениями конвоя
        public void Action(MyAI _player)
        {
            double Distance;
            double MinDistance;
            Point ShootAt = new Point();

            //Если конвоя ещё нет, то и делать нечего
            if (State == ConvoyState.UnderConstruction)
                return;

            //Устанавливаем все значения: State, Location, Stock, etc
            SetState(_player);

            //Если боец жив, проверяем наличие противников
            if (MyDefender != null)
            {
                //Если в пределах досягаемости кто-то есть, останавливаем конвой
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
                //Если конвой стоит
                if (this.State == ConvoyState.Waiting)
                {
                    //Ищем ближайшую цель
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
                    //Если она достаточно близко, стреляем и завершаем действия
                    if (MinDistance < MyDefender.DefenseDistance)
                    {
                        MyDefender.DefendTo(ShootAt, 3);
                        return;
                    }
                }
            }

            //Если конвой стоит
            if (State == ConvoyState.Waiting)
            {
                //Если мы не в куче, то собираемся в неё самую
                if (this.NeedGathering())
                {
                    this.Gather(_player);
                    return;
                }

                //Если мы на AZNPoint
                if (this.Location == this.APoint)
                {
                    AZNChosen = false;
                    //Если контейнеры пустые, то заполняем их
                    if (this.Stock == 0)
                    {
                        this.FillContainers();
                        return;
                    }
                    //Если не пустые, то идём к HoshimiPoint
                    else
                    {
                        if (this.Location == this.HPoint)
                            return;
                        this.Path = _player.Pathfinder.FindPath(this.Location, this.HPoint);
                        this.Move();
                        return;
                    }
                }

                //Если мы на HoshimiPoint
                if (this.Location == this.HPoint)
                {
                    //Если это на самом деле NavPoint
                    if (NavMission)
                    {
                        //Если ждать не надо или бесполезно
                        if (_player.CurrentTurn < _player.NavigationPoints[HPNumber].StartTurn)
                        {
                            return;
                        }
                        else if (_player.CurrentTurn >= _player.NavigationPoints[HPNumber].StartTurn
                            && _player.CurrentTurn <= _player.NavigationPoints[HPNumber].EndTurn)
                        {
                            //
                            //Отмечаем, что миссия выполнена
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
                            //Запрашиваем нормальный HP
                            this.HPoint = _player.GetNextHPForConvoy(this.Location, ref this.HPNumber, ref this.NavMission);
                            //Если пустые, то идём за AZN
                            if (this.Stock == 0)
                            {
                                this.APoint = _player.GetNearestAZNPoint(this.Location, this.HPoint);
                                AZNChosen = true;
                                this.Path = _player.Pathfinder.FindPath(this.Location, this.APoint);
                                this.Move();
                                return;
                            }
                            //Иначе сразу к HP
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
                            //Отмечаем, что миссия выполнена
                            _player.NavigationPoints[HPNumber].Complete = true;
                            //Запрашиваем нормальный HP
                            this.HPoint = _player.GetNextHPForConvoy(this.Location, ref this.HPNumber, ref this.NavMission);
                            //Если пустые, то идём за AZN
                            if (this.Stock == 0)
                            {
                                this.APoint = _player.GetNearestAZNPoint(this.Location, this.HPoint);
                                AZNChosen = true;
                                this.Path = _player.Pathfinder.FindPath(this.Location, this.APoint);
                                this.Move();
                                return;
                            }
                            //Иначе сразу к HP
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

                    //Если это валидный HP, то проверяем, наш ли он
                    if (HPNumber >= 0)
                    {
                        //Если нет, то переключаемся на новый
                        if (_player.HoshimiPoints[HPNumber].Needle == 2)
                        {
                            //this.HPoint = _player.GetNearestHPForConvoy(this.Location, ref this.HPNumber);
                            int fafa = HPNumber;
                            this.HPoint = _player.GetNextHPForConvoy(this.Location, ref this.HPNumber, ref this.NavMission);
                            //Если пустые, то идём за AZN
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
                            //Иначе сразу к HP
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
                    //Проверяем все Needle`ы
                    foreach (NanoBot bot in _player.NanoBots)
                    {
                        //Если находим Needle на этой точке и он полный
                        if ((bot is Needle) && (bot.Stock == bot.ContainerCapacity) && (bot.Location == this.Location))
                        {
                            if (HPNumber >= 0)
                            {
                                //То отмечаем этот факт
                                _player.HoshimiPoints[HPNumber].Full = 1;
                                //Запрашиваем новую HP
                                //this.HPoint = _player.GetNearestHPForConvoy(this.Location, ref this.HPNumber);
                                int fafa = HPNumber;
                                this.HPoint = _player.GetNextHPForConvoy(this.Location, ref this.HPNumber, ref this.NavMission);
                                //Если пустые, то идём за AZN
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
                                //Иначе сразу к HP
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
                        //Если стоит наш Needle
                        if (_player.HoshimiPoints[HPNumber].Needle == 1)
                        {
                            //И Needle ещё не заполнен
                            //Если мы уже разгрузились, то идём за AZN
                            if (this.Stock == 0)
                            {
                                this.APoint = _player.GetNearestAZNPoint(HPNumber, HPNumber);
                                AZNChosen = true;
                                this.Path = _player.Pathfinder.FindPath(this.Location, this.APoint);
                                this.Move();
                                return;
                            }
                            //Иначе разгружаемся
                            else
                            {
                                this.Transfert();
                                return;
                            }
                        }
                    }
                }

                //Если мы чёрте где
                //Если пустые, то за AZN
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
                //иначе на HP
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

        //Разгрузить AZN
        private void Transfert()
        {
            if (MyContainer[0] != null)
                MyContainer[0].TransferTo(this.Location, MyContainer[0].Stock / MyContainer[0].CollectTransfertSpeed);
            if (MyContainer[1] != null)
                MyContainer[1].TransferTo(this.Location, MyContainer[1].Stock / MyContainer[1].CollectTransfertSpeed);
        }

        //Двигаться
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

        //Загрузить AZN
        private void FillContainers()
        {
            if (MyContainer[0] != null)
                MyContainer[0].CollectFrom(this.Location, MyContainer[0].ContainerCapacity / MyContainer[0].CollectTransfertSpeed);
            if (MyContainer[1] != null)
                MyContainer[1].CollectFrom(this.Location, MyContainer[1].ContainerCapacity / MyContainer[1].CollectTransfertSpeed);
        }

        //Определяет текущее состояние конвоя
        private void SetState(MyAI _player)
        {
            //Проверка, живы ли боты
            if (MyDefender != null && MyDefender.HitPoint <= 0)
                MyDefender = null;
            if (MyContainer[0] != null && MyContainer[0].HitPoint <= 0)
                MyContainer[0] = null;
            if (MyContainer[1] != null && MyContainer[1].HitPoint <= 0)
                MyContainer[1] = null;

            //Если ботов нет, то конвой уничтожен
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

            //Обновляем данные для Stock и Location
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

            //Если HP не определён, то определяем
            if (this.HPNumber == -1)
            {
                //this.HPoint = _player.GetNearestHPForConvoy(this.Location, ref HPNumber);

                this.HPoint = _player.GetNextHPForConvoy(this.Location, ref this.HPNumber, ref this.NavMission);
            }

            //Если HP занят противником или заполнен или уже брошен, запрашиваем новый
            if (HPNumber >= 0 && !IsNavigating)
            {
                if ((_player.HoshimiPoints[HPNumber].Needle == 2) || (_player.HoshimiPoints[HPNumber].Full == 1) || (_player.HoshimiPoints[HPNumber].InPast == 1 && _player.HoshimiPoints[HPNumber].Needle != 1))
                {
                    //this.HPoint = _player.GetNearestHPForConvoy(this.Location, ref HPNumber);
                    this.HPoint = _player.GetNextHPForConvoy(this.Location, ref this.HPNumber, ref this.NavMission);
                    this.Stop();
                }
            }

            //Если Defender в бою, то значит и конвой в бою
            if (MyDefender != null && MyDefender.State == NanoBotState.Defending)
            {
                this.State = ConvoyState.Fighting;
                return;
            }

            //Долгие-долгие извращения из-за того, что неясно, кто жив, а кто deadlink
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

        //Останавливает конвой
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

        //Проверяет, вместе ли боты или нет.
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

        //Собраться вместе, если по каким-то причинам всё-таки разделились.
        //Теоретически не нужна. Может понадобиться только в случае лагов,
        //если вдруг часть конвоя походит, а часть нет.
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

        //"Регистрация" нового бота в конвое
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

        //Уничтожение конвоя
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


        //Основная функция. Управляет всеми движениями конвоя
        public void Action(MyAI _player)
        {
            double Distance;
            double MinDistance;
            Point ShootAt = new Point();

            //Если конвоя ещё нет, то и делать нечего
            if (State == ConvoyState.UnderConstruction)
                return;

            //Устанавливаем все значения: State, Location, Stock, etc
            SetState(_player);

            //Если боец жив, проверяем наличие противников
            if (MyDefender != null)
            {
                //Если в пределах досягаемости кто-то есть, останавливаем конвой
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
                //Если конвой стоит
                if (this.State == ConvoyState.Waiting)
                {
                    //Ищем ближайшую цель
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
                    //Если она достаточно близко, стреляем и завершаем действия
                    if (MinDistance < MyDefender.DefenseDistance)
                    {
                        MyDefender.DefendTo(ShootAt, 3);
                        return;
                    }
                }
            }

            //Если конвой стоит
            if (State == ConvoyState.Waiting)
            {
                //Если мы не в куче, то собираемся в неё самую
                if (this.NeedGathering())
                {
                    this.Gather(_player);
                    return;
                }

                //Если мы на AZNPoint
                if (this.Location == this.APoint)
                {
                    //Если контейнеры пустые, то заполняем их
                    if (this.Stock == 0)
                    {
                        this.FillContainers();
                        return;
                    }
                    //Если не пустые, то идём к HoshimiPoint
                    else
                    {
                        if (this.Location == this.HPoint)
                            return;
                        this.Path = _player.Pathfinder.FindPath(this.Location, this.HPoint);
                        this.Move();
                        return;
                    }
                }

                //Если мы на NavPoint
                if (this.Location == this.HPoint)
                {
                    //
                    //Если ждать не надо или бесполезно
                    if (_player.CurrentTurn < _player.NavigationPoints[HPNumber].StartTurn)
                    {
                        return;
                    }
                    else if (_player.CurrentTurn >= _player.NavigationPoints[HPNumber].StartTurn
                        && _player.CurrentTurn <= _player.NavigationPoints[HPNumber].EndTurn)
                    {
                        //
                        //Отмечаем, что миссия выполнена
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
                        //Запрашиваем нормальный HP
                        this.HPoint = _player.GetNextHPForConvoyWithCollector(this.Location, ref this.HPNumber);
                        if (HPNumber == -10)
                        {
                            this.Delete();
                            this.State = ConvoyState.Deleted;
                            return;
                        }
                        //Если пустые, то идём за AZN
                        if (this.Stock == 0)
                        {
                            this.APoint = _player.GetNearestAZNPoint(this.Location, this.HPoint);
                            this.Path = _player.Pathfinder.FindPath(this.Location, this.APoint);
                            this.Move();
                            return;
                        }
                        //Иначе сразу к HP
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
                        //Отмечаем, что миссия выполнена
                        _player.NavigationPoints[HPNumber].Complete = true;
                        //Запрашиваем нормальный HP
                        this.HPoint = _player.GetNextHPForConvoyWithCollector(this.Location, ref this.HPNumber);
                        if (HPNumber == -10)
                        {
                            this.Delete();
                            this.State = ConvoyState.Deleted;
                            return;
                        }
                        //Если пустые, то идём за AZN
                        if (this.Stock == 0)
                        {
                            this.APoint = _player.GetNearestAZNPoint(this.Location, this.HPoint);
                            this.Path = _player.Pathfinder.FindPath(this.Location, this.APoint);
                            this.Move();
                            return;
                        }
                        //Иначе сразу к HP
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
                    //Если ждать не надо или бесполезно
                    if (_player.CurrentTurn > _player.NavigationPoints[HPNumber].StartTurn)
                    {
                        //Отмечаем, что миссия выполнена
                        _player.NavigationPoints[HPNumber].Complete = true;
                        //Запрашиваем новый HP
                        this.HPoint = _player.GetNextHPForConvoyWithCollector(this.Location, ref this.HPNumber);
                        if (HPNumber == -10)
                        {
                            this.Delete();
                            this.State = ConvoyState.Deleted;
                            return;
                        }
                        //Если пустые, то идём за AZN
                        if (this.Stock == 0)
                        {
                            this.APoint = _player.GetNearestAZNPoint(this.Location, this.HPoint);
                            this.Path = _player.Pathfinder.FindPath(this.Location, this.APoint);
                            this.Move();
                            return;
                        }
                        //Иначе сразу к HP
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

                //Если мы чёрте где
                //Если пустые, то за AZN
                if (this.Stock == 0)
                {
                    this.APoint = _player.GetNearestAZNPoint(this.Location, this.HPoint);
                    this.Path = _player.Pathfinder.FindPath(this.Location, this.APoint);
                    this.Move();
                    return;
                }
                //иначе на HP
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

        //Разгрузить AZN
        private void Transfert()
        {
            if (MyContainer != null)
                MyContainer.TransferTo(this.Location, MyContainer.Stock / MyContainer.CollectTransfertSpeed);
        }

        //Двигаться
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

        //Загрузить AZN
        private void FillContainers()
        {
            if (MyContainer != null)
                MyContainer.CollectFrom(this.Location, MyContainer.ContainerCapacity / MyContainer.CollectTransfertSpeed);
        }

        //Определяет текущее состояние конвоя
        private void SetState(MyAI _player)
        {
            if (State == ConvoyState.Deleted)
                return;

            //Проверка, живы ли боты
            if (MyDefender != null && MyDefender.HitPoint <= 0)
                MyDefender = null;
            if (MyContainer != null && MyContainer.HitPoint <= 0)
                MyContainer = null;

            //Если ботов нет, то конвой уничтожен
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

            //Обновляем данные для Stock и Location
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

            //Если HP не определён, то определяем
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

            //Если Defender в бою, то значит и конвой в бою
            if (MyDefender != null && MyDefender.State == NanoBotState.Defending)
            {
                this.State = ConvoyState.Fighting;
                return;
            }

            //Долгие-долгие извращения из-за того, что неясно, кто жив, а кто deadlink
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

        //Останавливает конвой
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

        //Проверяет, вместе ли боты или нет.
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

        //Собраться вместе, если по каким-то причинам всё-таки разделились.
        //Теоретически не нужна. Может понадобиться только в случае лагов,
        //если вдруг часть конвоя походит, а часть нет.
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

        //"Регистрация" нового бота в конвое
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

        //Уничтожение конвоя
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


        //Основная функция. Управляет всеми движениями конвоя
        public void Action(MyAI _player)
        {
            double Distance;
            double MinDistance;
            Point ShootAt = new Point();

            //Если конвоя ещё нет, то и делать нечего
            if (State == ConvoyState.UnderConstruction)
                return;

            //Устанавливаем все значения: State, Location, Stock, etc
            SetState(_player);

            //Если боец жив, проверяем наличие противников
            if (MyDefender != null)
            {
                //Если в пределах досягаемости кто-то есть, останавливаем конвой
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
                //Если конвой стоит
                if (this.State == ConvoyState.Waiting)
                {
                    //Ищем ближайшую цель
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
                    //Если она достаточно близко, стреляем и завершаем действия
                    if (MinDistance < MyDefender.DefenseDistance)
                    {
                        MyDefender.DefendTo(ShootAt, 3);
                        return;
                    }
                }
            }

            //Если конвой стоит
            if (State == ConvoyState.Waiting)
            {
                //Если мы не в куче, то собираемся в неё самую
                if (this.NeedGathering())
                {
                    this.Gather(_player);
                    return;
                }

                //Если мы на AZNPoint
                if (this.Location == this.APoint)
                {
                    //Если контейнеры пустые, то заполняем их
                    if (this.Stock == 0)
                    {
                        this.FillContainers();
                        return;
                    }
                    //Если не пустые, то идём к HoshimiPoint
                    else
                    {
                        if (this.Location == this.HPoint)
                            return;
                        this.Path = _player.Pathfinder.FindPath(this.Location, this.HPoint);
                        this.Move();
                        return;
                    }
                }

                //Если мы на NavPoint
                if (this.Location == this.HPoint)
                {
                    //
                    //Если ждать не надо или бесполезно
                    if (_player.CurrentTurn < _player.NavigationPoints[HPNumber].StartTurn)
                    {
                        return;
                    }
                    else if (_player.CurrentTurn >= _player.NavigationPoints[HPNumber].StartTurn
                        && _player.CurrentTurn <= _player.NavigationPoints[HPNumber].EndTurn)
                    {
                        //
                        //Отмечаем, что миссия выполнена
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
                        //Запрашиваем новый HP
                        this.HPoint = _player.GetNextHPForConvoyWithBigContainer(this.Location, ref this.HPNumber);
                        if (HPNumber == -10)
                        {
                            this.Delete();
                            this.State = ConvoyState.Deleted;
                            return;
                        }
                        //Если пустые, то идём за AZN
                        if (this.Stock == 0)
                        {
                            this.APoint = _player.GetNearestAZNPoint(this.Location, this.HPoint);
                            this.Path = _player.Pathfinder.FindPath(this.Location, this.APoint);
                            this.Move();
                            return;
                        }
                        //Иначе сразу к HP
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
                        //Отмечаем, что миссия выполнена
                        _player.NavigationPoints[HPNumber].Complete = true;
                        //Запрашиваем новый HP
                        this.HPoint = _player.GetNextHPForConvoyWithBigContainer(this.Location, ref this.HPNumber);
                        if (HPNumber == -10)
                        {
                            this.Delete();
                            this.State = ConvoyState.Deleted;
                            return;
                        }
                        //Если пустые, то идём за AZN
                        if (this.Stock == 0)
                        {
                            this.APoint = _player.GetNearestAZNPoint(this.Location, this.HPoint);
                            this.Path = _player.Pathfinder.FindPath(this.Location, this.APoint);
                            this.Move();
                            return;
                        }
                        //Иначе сразу к HP
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
                    //Если ждать не надо или бесполезно
                    if (_player.CurrentTurn > _player.NavigationPoints[HPNumber].StartTurn)
                    {
                        //Отмечаем, что миссия выполнена
                        _player.NavigationPoints[HPNumber].Complete = true;
                        //Запрашиваем новый HP
                        this.HPoint = _player.GetNextHPForConvoyWithBigContainer(this.Location, ref this.HPNumber);
                        if (HPNumber == -10)
                        {
                            this.Delete();
                            this.State = ConvoyState.Deleted;
                            return;
                        }
                        //Если пустые, то идём за AZN
                        if (this.Stock == 0)
                        {
                            this.APoint = _player.GetNearestAZNPoint(this.Location, this.HPoint);
                            this.Path = _player.Pathfinder.FindPath(this.Location, this.APoint);
                            this.Move();
                            return;
                        }
                        //Иначе сразу к HP
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

                //Если мы чёрте где
                //Если пустые, то за AZN
                if (this.Stock == 0)
                {
                    this.APoint = _player.GetNearestAZNPoint(this.Location, this.HPoint);
                    this.Path = _player.Pathfinder.FindPath(this.Location, this.APoint);
                    this.Move();
                    return;
                }
                //иначе на HP
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

        //Разгрузить AZN
        private void Transfert()
        {
            if (MyContainer != null)
                MyContainer.TransferTo(this.Location, MyContainer.Stock / MyContainer.CollectTransfertSpeed);
        }

        //Двигаться
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

        //Загрузить AZN
        private void FillContainers()
        {
            if (MyContainer != null)
                MyContainer.CollectFrom(this.Location, MyContainer.ContainerCapacity / MyContainer.CollectTransfertSpeed);
        }

        //Определяет текущее состояние конвоя
        private void SetState(MyAI _player)
        {
            if (State == ConvoyState.Deleted)
                return;

            //Проверка, живы ли боты
            if (MyDefender != null && MyDefender.HitPoint <= 0)
                MyDefender = null;
            if (MyContainer != null && MyContainer.HitPoint <= 0)
                MyContainer = null;

            //Если ботов нет, то конвой уничтожен
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

            //Обновляем данные для Stock и Location
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

            //Если HP не определён, то определяем
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

            //Если Defender в бою, то значит и конвой в бою
            if (MyDefender != null && MyDefender.State == NanoBotState.Defending)
            {
                this.State = ConvoyState.Fighting;
                return;
            }

            //Долгие-долгие извращения из-за того, что неясно, кто жив, а кто deadlink
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

        //Останавливает конвой
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

        //Проверяет, вместе ли боты или нет.
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

        //Собраться вместе, если по каким-то причинам всё-таки разделились.
        //Теоретически не нужна. Может понадобиться только в случае лагов,
        //если вдруг часть конвоя походит, а часть нет.
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