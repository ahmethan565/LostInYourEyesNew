// PlayerFSM.cs
using UnityEngine;
using System.Collections.Generic; // Durumları saklamak için

public class PlayerFSM
{
    private PlayerState currentState;
    private Dictionary<System.Type, PlayerState> states = new Dictionary<System.Type, PlayerState>();

    public PlayerFSM() { }

    public void AddState(PlayerState state)
    {
        states.Add(state.GetType(), state);
    }

    public void ChangeState(System.Type newStateType)
    {
        if (currentState != null)
        {
            currentState.Exit();
        }

        if (states.TryGetValue(newStateType, out PlayerState newState))
        {
            currentState = newState;
            currentState.Enter();
        }
        else
        {
            Debug.LogError($"FSM'de {newStateType.Name} durumu bulunamadı!");
        }
    }

    public void Update()
    {
        if (currentState != null)
        {
            currentState.Execute();
        }
    }

    public PlayerState GetCurrentState()
    {
        return currentState;
    }
}
