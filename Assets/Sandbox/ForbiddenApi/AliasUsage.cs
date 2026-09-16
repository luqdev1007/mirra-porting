using T = UnityEngine.Time;
using Time = Sandbox.ForbiddenApi.Shadow.Time;

namespace Sandbox.ForbiddenApi
{
    /// <summary>
    /// One alias points at the Unity type, the other takes the name Time away from it. In this
    /// file Time is the game type, and the scanner has to stay quiet about it.
    /// </summary>
    public static class AliasUsage
    {
        public static void Apply()
        {
            T.timeScale = 0f;         // EXPECT API.TIMESCALE_WRITE Error High false
            Time.timeScale = 1f;      // the alias points elsewhere, nothing to report
        }
    }
}
