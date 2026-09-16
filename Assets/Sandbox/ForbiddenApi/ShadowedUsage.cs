namespace Sandbox.ForbiddenApi.Shadow
{
    /// <summary>
    /// This file sits in the namespace that declares its own Time and Cursor, so these lines touch
    /// the game types, not the Unity ones. The scanner cannot know that without resolving types,
    /// so it reports them with low confidence and says why.
    /// </summary>
    public static class ShadowedUsage
    {
        public static void Apply()
        {
            Time.timeScale = 0f;      // EXPECT API.TIMESCALE_WRITE Error Low false
            Cursor.visible = false;   // EXPECT API.CURSOR_VISIBLE_WRITE Error Low false
        }
    }
}
