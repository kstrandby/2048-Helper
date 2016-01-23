using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.Scripts
{
    // Tuple class - instead of standard C# Tuple not supported in Unity
    public class Tuple<T1, T2>
    {
        public T1 Item1 { get; private set; }
        public T2 Item2 { get; private set; }

        public Tuple(T1 t1, T2 t2)
        {
            Item1 = t1;
            Item2 = t2;
        }
    }
}
