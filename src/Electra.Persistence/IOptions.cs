using System.Collections;

namespace Microbians.Persistence
{
    public interface IOptions
    {
        IOptionsDictionary Values { get; }
    }

    public interface ICommandOptions : IOptions
    {
    }

    public interface IOptionsDictionary : IEnumerable<KeyValuePair<string, object>>, IEnumerable
    {
        bool Contains(string name);
        T Get<T>(string name, T defaultValue = default(T));
        bool Remove(string name);
        void Set(string name, object value);
    }
}