namespace myProject
{
    // NOTE: stub de estado de jogo (conteudo). So o que Player/Input leem. Assists default = off.
    public class SaveData
    {
        public static SaveData Instance = new SaveData();

        public Assists Assists;
        public int TotalJumps;
        public int TotalWallJumps;
        public int TotalDashes;
        public int TotalDeaths;

        public void AddDeath(AreaKey area)
        {
            TotalDeaths++;
        }
    }

    // NOTE: stub de chave de area (conteudo).
    public struct AreaKey
    {
        public int ID;
    }
}
