using System;
using System.Collections.Generic;

namespace Celeste.Mod.Randomizer {

    public struct RecordTuple {
        public long Item1;
        public string Item2;
        
        public static RecordTuple Create(long a, string b) {
            return new RecordTuple {
                Item1 = a,
                Item2 = b,
            };
        }
    }

}