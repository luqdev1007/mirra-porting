using static UnityEngine.Application;
using static UnityEngine.Time;

namespace Sandbox.ForbiddenApi
{
    /// <summary>
    /// Bare member names brought in by using static. They are the weakest evidence there is, so
    /// they are reported with low confidence — and declarations of the same names are not
    /// reported at all.
    /// </summary>
    public static class UsingStaticUsage
    {
        public static float Read()
        {
            return timeScale;         // EXPECT API.TIMESCALE_READ Warning Low false
        }

        public static void Exit()
        {
            Quit();                   // EXPECT API.QUIT Error Low false
        }

        public static void Declared(float timeScale)
        {
        }
    }

    /// <summary>The same names, declared instead of used. Nothing here is a finding.</summary>
    public static class UsingStaticDeclarations
    {
        public static float timeScale;

        public static void Quit()
        {
        }
    }
}
