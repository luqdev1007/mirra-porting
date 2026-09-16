namespace Luqmus.MirraPorting.Code
{
    /// <summary>
    /// A name a file declares, together with the namespace it was declared in. The namespace
    /// matters: a plugin with its own Screen in its own namespace does not shadow UnityEngine.Screen
    /// for the rest of the project.
    /// </summary>
    internal readonly struct DeclaredName
    {
        internal DeclaredName(string name, string namespaceName)
        {
            Name = name;
            Namespace = namespaceName;
        }

        internal string Name { get; }

        /// <summary>Enclosing namespace, or an empty string for the global one.</summary>
        internal string Namespace { get; }

        public override string ToString()
        {
            return Namespace.Length == 0 ? Name : Namespace + "." + Name;
        }
    }
}
