using UnityEngine;

public class Building : MonoBehaviour
{
    public BuildingState currentState = BuildingState.UnderConstruction;

    // Stub methods so other scripts compile.
    public void UpdateStateFromProductivity(ProductivityBand band)
    {
        // TODO: implement real logic later
    }

    public void ForceCollapse()
    {
        // TODO: implement real logic later
    }
}
