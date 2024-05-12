namespace Electra.Persistence;

public interface IStoredProcRepository
{
    void ExecStoredProc(string name, params object[] parameters);
    object ExecStoredProc<U>(string name, params object[] parameters);
    Task ExecStoredProcAsync(string name, params object[] parameters);
    Task<object> ExecStoredProcAsync<U>(string name, params object[] parameters);
}