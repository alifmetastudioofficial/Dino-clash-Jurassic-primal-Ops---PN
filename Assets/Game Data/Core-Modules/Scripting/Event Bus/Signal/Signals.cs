using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Signals 
{
    public class OnLoadNewScreen : ISignal
    {
        public bool OpenLastScreen = false;
    }

}
