namespace AkGaming.Tournaments.Application.RegistrationRules;

public interface IGameRankSystemRegistry
{
    IGameRankSystem GetRankSystem(string gameId);
}
