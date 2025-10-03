namespace PokerGame;
public enum Rank
{
    Two = 2,
    Three = 3,
    Four = 4,
    Five = 5,
    Six = 6,
    Seven = 7,
    Eight = 8,
    Nine = 9,
    Ten = 10,
    Jack = 11,
    Queen = 12,
    King = 13,
    Ace = 14
}

public enum Suit
{
    Hearts,
    Diamonds,
    Clubs,
    Spades
}
public enum PlayerAction
{
    Fold,
    Check,
    Call,
    Raise,
    AllIn
}

public enum GameEventType
{
    GameStarted,
    RoundStart,
    RoundEnded,
    PlayerFolded,
    PlayerRaised,
    PlayerAllin,
    PlayerChecked,   
    PlayerCalled     
}
public enum ChipType
{
    White = 10,
    Red = 50,
    Green = 100,
    Black = 1000
}