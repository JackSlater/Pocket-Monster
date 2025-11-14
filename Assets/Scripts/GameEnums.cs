using UnityEngine;

public enum ProductivityBand
{
    Thriving,   // >= 75
    Declining,  // > 0 and < 75
    Collapse    // <= 0
}

public enum PersonState
{
    Working,
    ShiftingAttention,
    PhoneAddiction,
    Idle,
    Destructive
}

public enum BuildingState
{
    UnderConstruction = 0,
    Thriving          = 1,
    Declining         = 2,
    Ruined            = 3,
    Destroyed         = 4
}
