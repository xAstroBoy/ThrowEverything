using System;
using System.Collections.Generic;
using System.Text;
using ThrowEverything.Models;

namespace ThrowEverything
{
    internal class State
    {
        static readonly ChargingThrow chargingThrow = new();
        static readonly ThrownItems thrownItems = new();


        internal static void ClearHeldThrowable()
        {
            chargingThrow.Stop();
        }


        internal static ChargingThrow GetChargingThrow()
        {
            return chargingThrow;
        }

        internal static ThrownItems GetThrownItems()
        {
            return thrownItems;
        }
    }
}
