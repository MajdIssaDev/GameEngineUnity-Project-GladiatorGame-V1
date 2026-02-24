public interface ICombatReceiver
{
    //Both PlayerCombat and EnemyCombat must implement these functions
    void OnComboWindowOpen();
    void OnFinishAttack();
}