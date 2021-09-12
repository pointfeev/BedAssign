using CompatUtils;
using RimWorld;
using System;
using System.Reflection;
using Verse;

namespace BedAssign
{
    [StaticConstructorOnStartup]
    public static class ModCompatibility
    {
        public static MethodInfo hospitalityIsGuestBedMethod;

        static ModCompatibility()
        {
            hospitalityIsGuestBedMethod = Compatibility.GetConsistentMethod("orion.hospitality", "Hospitality.BedUtility", "IsGuestBed", new Type[] {
                typeof(Building_Bed)
            }, logError: true);
        }

        public static bool IsHospitalityGuestBed(this Building_Bed bed)
        {
            return !(hospitalityIsGuestBedMethod is null) && (bool)hospitalityIsGuestBedMethod.Invoke(null, new object[] { bed });
        }
    }
}
