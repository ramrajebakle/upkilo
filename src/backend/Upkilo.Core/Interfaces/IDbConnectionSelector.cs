namespace Upkilo.Core.Interfaces;

public interface IDbConnectionSelector
{
    string GetConnectionString();
    void UseReplica(bool useReplica = true);
    void MarkPrimaryDown(bool isDown);
}
