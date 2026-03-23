namespace Colyseus
{
    public interface ITokenStorage
    {
        string GetToken(string key);
        void SetToken(string key, string value);
        void DeleteToken(string key);
    }
}
