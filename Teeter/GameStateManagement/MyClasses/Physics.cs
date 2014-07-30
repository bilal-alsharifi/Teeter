using System;
using System.Collections.Generic;
using System.Text;

namespace Teeter.MyClasses
{
    class Physics
    {
        public static float Calc_Friction_Force(float gravity, float angle, float mass, float frictionFactor)
        {
            return (float)(mass * gravity * frictionFactor * Math.Cos(angle));
        }
        public static float Calc_Weight_Force(float gravity, float angle, float mass)
        {
            return (float)(mass * gravity * Math.Sin(angle));
        }
        public static bool Check_Movement(float gravity, float angle, float mass, float frictionFactor, float speed)
        {
            bool result = false;
            float friction_Force = Calc_Friction_Force(gravity, angle, mass, frictionFactor);
            float weight_Force = Calc_Weight_Force(gravity, angle, mass);
            if ((Math.Abs(weight_Force) >= Math.Abs(friction_Force)) || (Math.Abs(speed) > 0.1f))
                result = true;
            return result;
        }
        public static float Calc_Speed(float gravity, float angle, float time, float speed0, float frictionFactor, float frictionDirection)
        {
            return (float)(gravity * (Math.Sin(angle) + frictionDirection * (Math.Cos(angle) * frictionFactor)) * time + speed0);
        }
        public static float Calc_Distance(float gravity, float angle, float time, float speed0, float distance0, float frictionFactor, float frictionDirection)
        {
            return (float)((0.5 * gravity * (Math.Sin(angle) + frictionDirection * (Math.Cos(angle) * frictionFactor)) * Math.Pow(time, 2)) + (speed0 * time) + (distance0));
        }
    }
}
