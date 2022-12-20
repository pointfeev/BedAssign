using System.Reflection;
using CompatUtils;
using RimWorld;
using Verse;

namespace BedAssign
{
    [StaticConstructorOnStartup]
    public static class ModCompatibility
    {
        private static readonly MethodInfo HospitalityIsGuestBedMethod;

        static ModCompatibility() => HospitalityIsGuestBedMethod
            = Compatibility.GetConsistentMethod("Orion.Hospitality", "Hospitality.Utilities.BedUtility", "IsGuestBed",
                                                new[] { typeof(Building_Bed) }, true);

        public static bool IsHospitalityGuestBed(this Building_Bed bed) => !(HospitalityIsGuestBedMethod is null)
                                                                        && (bool)HospitalityIsGuestBedMethod.Invoke(
                                                                               null, new object[] { bed });
    }
}