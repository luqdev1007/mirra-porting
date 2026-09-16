using System.IO;
using UnityEngine;

namespace Sandbox.ForbiddenApi
{
    /// <summary>
    /// File saves, qualified and bare. A bare File without using System.IO would be a low
    /// confidence finding, but such a file does not compile, so it lives in the tests instead.
    /// </summary>
    public static class FileIoUsage
    {
        public static void Save()
        {
            string path = Application.persistentDataPath + "/save.json";  // EXPECT API.PERSISTENT_DATA_PATH Warning High false
            File.WriteAllText(path, "{}");                                // EXPECT API.FILE_WRITE Warning High false
            System.IO.File.AppendAllText(path, "{}");                     // EXPECT API.FILE_WRITE Warning High false
            File.WriteAllBytes(path, new byte[0]);                        // EXPECT API.FILE_WRITE Warning High false
            Debug.Log(File.ReadAllText(path));                            // reading a file is allowed
        }
    }
}
