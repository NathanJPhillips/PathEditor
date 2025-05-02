using System.Reflection;

namespace NobleTech.Products.PathEditor.Utils;

public static class TypeUtils
{
    /// <summary>
    /// Retrieves all types that are derived from the specified base class <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The base class type to search for derived classes.</typeparam>
    /// <param name="additionalAssemblies">Optional assemblies to include in the search.</param>
    /// <returns>An enumerable of types that are derived from <typeparamref name="T"/>.</returns>
    public static IEnumerable<Type> AllDerivedClasses<T>(params Assembly[] additionalAssemblies) =>
        AllDerivedClasses<T>(true, true, true, true, additionalAssemblies);

    /// <summary>
    /// Retrieves all types that are derived from the specified base class <typeparamref name="T"/>, 
    /// with options to specify which assemblies to search in.
    /// </summary>
    /// <typeparam name="T">The base class type to search for derived classes.</typeparam>
    /// <param name="searchInDefiningAssembly">Whether to search in the assembly where <typeparamref name="T"/> is defined.</param>
    /// <param name="searchInExecutingAssembly">Whether to search in the currently executing assembly.</param>
    /// <param name="searchInEntryAssembly">Whether to search in the entry assembly of the application.</param>
    /// <param name="searchInCallingAssembly">Whether to search in the assembly of the calling code.</param>
    /// <param name="additionalAssemblies">Optional additional assemblies to include in the search.</param>
    /// <returns>An enumerable of types that are derived from <typeparamref name="T"/>.</returns>
    public static IEnumerable<Type> AllDerivedClasses<T>(
        bool searchInDefiningAssembly,
        bool searchInExecutingAssembly,
        bool searchInEntryAssembly,
        bool searchInCallingAssembly,
        params Assembly[] additionalAssemblies)
    {
        Type baseClass = typeof(T);
        IEnumerable<Assembly> assemblies = additionalAssemblies;
        if (searchInDefiningAssembly)
            assemblies = assemblies.Prepend(baseClass.Assembly);
        if (searchInExecutingAssembly)
            assemblies = assemblies.Prepend(Assembly.GetExecutingAssembly());
        if (searchInEntryAssembly && Assembly.GetEntryAssembly() is Assembly entryAssembly)
            assemblies = assemblies.Prepend(entryAssembly);
        if (searchInCallingAssembly)
            assemblies = assemblies.Prepend(Assembly.GetCallingAssembly());
        return assemblies.Distinct().SelectMany(assembly => assembly.GetExportedTypes()).Where(type => type.IsSubclassOf(baseClass));
    }

    /// <summary>
    /// Retrieves default constructors for all types derived from the specified base class <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The base class type to search for derived classes.</typeparam>
    /// <returns>
    /// An enumerable of functions that create instances of types derived from <typeparamref name="T"/> 
    /// using their default constructors.
    /// </returns>
    public static IEnumerable<Func<T>> AllDerivedClassesDefaultConstructors<T>() =>
        AllDerivedClasses<T>()
            .Select(type => type.GetConstructor([]))
            .Where(constructor => constructor is not null)
            .Select<ConstructorInfo?, Func<T>>(constructor => () => (T)constructor!.Invoke([]));
}
