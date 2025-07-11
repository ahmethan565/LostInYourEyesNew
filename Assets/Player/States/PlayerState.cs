// PlayerState.cs
using UnityEngine;

public abstract class PlayerState
{
    protected FPSPlayerController player; // FPSPlayerController'a erişim için
    protected PlayerFSM fsm; // FSM'ye erişim için

    public PlayerState(FPSPlayerController player, PlayerFSM fsm)
    {
        this.player = player;
        this.fsm = fsm;
    }

    public virtual void Enter() { } // Duruma girerken çalışır
    public virtual void Exit() { }  // Durumdan çıkarken çalışır
    public virtual void Execute() { } // Her Update'de çalışır
}
