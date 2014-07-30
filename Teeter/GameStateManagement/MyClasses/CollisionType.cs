using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;

namespace Teeter.MyClasses
{
    class CollisionType
    {
        public bool wall = false;
        public bool block = false;
        public bool hole = false;
        public Vector3 holeCenter;
        public bool winningHole = false;
    }
}
