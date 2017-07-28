using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ArcheBot.Bot.Classes;

namespace CombatMaster.Modules
{
    public class MoveModule : CoreHelper
    {
        private Host Host;
        private CancellationToken token;

        private bool isSwimToCancelRequest;

        public bool IsMoving { get; set; }


        public MoveModule(Host host, CancellationToken token) : base(host)
        {
            Host = host;
            this.token = token;
        }

        public bool ComeTo(SpawnObject obj, double dist = 1, double doneDist = 1.5, Func<bool> eval = null)
        {
            IsMoving = true;
            bool result = false;

            if (eval != null) Unless(eval);

            if (!Host.me.isSwim)
            {
                result = Host.ComeTo(obj, dist, doneDist);
            }
            else
            {
                result = SwimTo(obj, dist);
            }

            IsMoving = false;

            return result;
        }

        public bool ComeTo(double x, double y, double z, double dist = 1, double doneDist = 1.5, Func<bool> eval = null)
        {
            IsMoving = true;
            bool result = false;

            if (eval != null) Unless(eval);

            if (!Host.me.isSwim)
            {
                result = Host.ComeTo(x, y, z, dist, doneDist);
            }
            else
            {
                result = SwimTo(x, y, z, dist);
            }

            IsMoving = false;

            return result;
        }

        public bool ComeToPoint(GpsPoint point, double dist = 1, double doneDist = 1.5, Func<bool> eval = null) 
            => ComeTo(point.x, point.y, point.z, dist, doneDist, eval);


        public bool SwimTo(SpawnObject obj, double dist)
        {
            isSwimToCancelRequest = false;
            Host.MoveForward(true);
            
            while (token.IsAlive() && !isSwimToCancelRequest)
            {
                try
                {
                    if (Host.dist(obj) <= dist && IsZAligned(obj.Z))
                        break;

                    if (!Host.me.isSwim)
                        break;

                    double distToCome = Host.me.dist(obj.X, obj.Y);
                    
                    double[] rc = GetRayCast(3.5);
                    double mapHeight1 = Host.getZFromHeightMap(Host.me.X, Host.me.Y);
                    double mapHeight2 = Host.getZFromHeightMap(rc[0], rc[1]);

                    double meZ = Host.me.Z;


                    Host.MoveForward(!(distToCome <= dist && (IsZAligned(obj.Z) || distToCome <= 2.5)));

                    if (distToCome > 4.5 && !IsFacingAngle(obj.X, obj.Y, 8))
                    {
                        Host.TurnDirectly(obj);
                    }


                    if (distToCome <= dist + 10)
                    {
                        AlignZHeight(obj);
                    }
                    else
                    {
                        if (Math.Abs(meZ - mapHeight1) < 1.5)
                        {
                            AlignZMapHeight(3);
                        }
                        else if ((meZ - mapHeight2) < 1.5)
                        {
                            AlignZMapHeight(rc[0], rc[1], 3);
                        }
                        else
                        {
                            Host.SwimUp(false);
                            Host.SwimDown(false);
                        }
                    }
                }
                catch
                {
                    isSwimToCancelRequest = true;
                    break;
                }


                Utils.Delay(500, token);
            }

            Host.MoveForward(false);
            Host.SwimUp(false);
            Host.SwimDown(false);


            return (!isSwimToCancelRequest && (Host.dist(obj) <= dist));
        }

        private bool SwimTo(double x, double y, double z, double dist = 2.5)
        {
            isSwimToCancelRequest = false;
            Host.MoveForward(true);

            while (token.IsAlive() && !isSwimToCancelRequest && Host.me.dist(x, y) > dist)
            {
                try
                {
                    if (!Host.me.isSwim)
                        break;

                    double distToCome = Host.me.dist(x, y);

                    if (distToCome <= dist)
                    {
                        Host.MoveForward(false);
                    }

                    if (!IsFacingAngle(x, y, 8))
                    {
                        Host.Turn(-AngleToRadians(Host.angle(Host.me, x, y)));
                    }

                    //AlignToZ(75);
                }
                catch
                {
                    isSwimToCancelRequest = true;
                    break;
                }


                Utils.Delay(50, 100, token);
            }

            Host.MoveForward(false);
            Host.SwimUp(false);
            Host.SwimDown(false);


            return (!isSwimToCancelRequest && (Host.me.dist(x, y) <= dist));
        }

        #region Helpers

        private bool IsZAligned(double zHeight, double zMargin = 1)
        {
            return (Host.me.Z < (zHeight + zMargin) && Host.me.Z > (zHeight - zMargin));
        }

        public void AlignZHeight(SpawnObject obj)
        {
            double height = Host.me.Z - obj.Z;
            double mapZHeight = Host.getZFromHeightMap(Host.me.X, Host.me.Y);

            if (Math.Abs(height) <= 1 || (Host.me.Z - mapZHeight) <= 1)
            {
                Host.SwimUp(false);
                Host.SwimDown(false);

                return;
            }


            if (height > 0)
            {
                if (Host.swimUpState) Host.SwimUp(false);

                Host.SwimDown(true);
            }
            else
            {
                if (Host.swimDownState) Host.SwimDown(false);

                Host.SwimUp(true);
            }
        }

        public void AlignZMapHeight(double x, double y, double zFloat)
        {
            double mapZHeight = Host.getZFromHeightMap(x, y);
            double height = Host.me.Z - mapZHeight;

            if (height < (zFloat + 0.7) && height > (zFloat - 0.7))
            {
                Host.SwimUp(false);
                Host.SwimDown(false);

                return;
            }


            if (height > zFloat)
            {
                if (Host.swimUpState) Host.SwimUp(false);

                Host.SwimDown(true);
            }
            else
            {
                if (Host.swimDownState) Host.SwimDown(false);

                Host.SwimUp(true);
            }
        }

        public void AlignZMapHeight(double zFloat) 
            => AlignZMapHeight(Host.me.X, Host.me.Y, zFloat);

        private void AlignToZ(double zFloat)
        {
            double height = Host.me.Z;

            if (height < (zFloat + 0.7) && height > (zFloat - 0.7))
            {
                Host.SwimUp(false);
                Host.SwimDown(false);

                return;
            }

            if (height > zFloat)
            {
                if (Host.swimUpState) Host.SwimUp(false);

                Host.SwimDown(true);
            }
            else
            {
                if (Host.swimDownState) Host.SwimDown(false);

                Host.SwimUp(true);
            }
        }

        #endregion


        private void Unless(Func<bool> eval)
        {
            Task.Run(() =>
            {
                while (token.IsAlive() && IsMoving)
                {
                    try
                    {
                        if (eval.Invoke())
                        {
                            Utils.Delay(450, 650, token);

                            Cancel();
                            break;
                        }
                    }
                    catch
                    {
                    }

                    Utils.Delay(50, token);
                }
            }, token);
        }

        public void Cancel()
        {
            isSwimToCancelRequest = true;
            Host.CancelMoveTo();
        }
    }
}
