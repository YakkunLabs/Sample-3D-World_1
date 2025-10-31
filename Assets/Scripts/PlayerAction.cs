using UnityEngine;

[System.Serializable]
public class PlayerAction
{
    public float posX;
    public float posY;
    public float posZ;
    public float rotY;
    public int moveState; // 0=idle, 1=walk, 2=run
    public bool didJump; 
    
    // --- THIS IS THE CORRECTED PART ---
    public int equippedWeapon; // 0=unarmed, 1=sword, 2=pistol
    public int attackType;     // 0=none, 1=sword_attack, 2=pistol_attack
    public bool isAiming;
    public Vector3 fireDirection;
}