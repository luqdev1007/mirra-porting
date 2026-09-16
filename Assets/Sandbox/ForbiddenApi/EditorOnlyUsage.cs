using UnityEngine;

namespace Sandbox.ForbiddenApi
{
    /// <summary>
    /// The same call inside and outside an editor only branch. The branch the player build drops
    /// is only worth knowing about; the line below it is a real problem.
    /// </summary>
    public static class EditorOnlyUsage
    {
        public static void Apply()
        {
#if UNITY_EDITOR
            Time.timeScale = 0f;      // EXPECT API.TIMESCALE_WRITE Info High true
#endif
            Cursor.visible = true;    // EXPECT API.CURSOR_VISIBLE_WRITE Error High false
        }
    }
}
