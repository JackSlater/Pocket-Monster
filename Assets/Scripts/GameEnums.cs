using UnityEngine;

/// <summary>
/// Overall productivity state of the city, driven by ProductivityManager.
/// </summary>
public enum ProductivityBand
{
    Thriving = 0,   // high productivity
    Declining = 1,  // starting to slip
    Collapse  = 2   // very low / effectively broken
}

/// <summary>
/// Behaviour state of an individual villager.
/// </summary>
public enum PersonState
{
    Working,            // contributing to buildings
    Idle,               // not helping
    ShiftingAttention,  // distracted but not fully lost
    PhoneAddiction,     // fully lost to phones
    Destructive         // violent or building-destroyer
}

/// <summary>
/// Lifecycle state of a building.
/// </summary>
public enum BuildingState
{
    UnderConstruction = 0,
    Completed         = 1,
    Destroyed         = 2
}

/// <summary>
/// Type of phone being dropped (maps to different effects).
/// </summary>
public enum PhoneType
{
    SocialMediaRed,     // red: violent / chaos via collector
    StreamingYellow,    // yellow: nearest few go idle/binge
    MainstreamBlue,     // blue: slows everyone down
    GamblingGreen       // green: one villager destroys buildings
}
