namespace Sandbox.ForbiddenApi
{
    /// <summary>
    /// No using UnityEngine here on purpose: a qualified name is certain on its own, and the
    /// things that only look like calls must stay silent.
    /// </summary>
    public static class QualifiedUsage
    {
        public static string Name;

        public static void Apply()
        {
            UnityEngine.Time.timeScale = 1f;                    // EXPECT API.TIMESCALE_WRITE Error High false
            global::UnityEngine.Application.Quit();             // EXPECT API.QUIT Error High false
            Name = nameof(UnityEngine.Time.timeScale);          // named, not touched
            // UnityEngine.Time.timeScale = 0f;                 // a comment is not code
            UnityEngine.Debug.Log("UnityEngine.Time.timeScale = 0f;"); // a string is not code
            UnityEngine.Debug.Log(Name);
        }
    }
}
