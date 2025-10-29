// This isn't a MonoBehaviour, it's just a data container.
// [System.Serializable] lets Unity's JsonUtility convert it.
[System.Serializable]
public class PlayerAction
{
    public float posX;
    public float posY;
    public float posZ;
    public float rotY;
    public int moveState; // 0=idle, 1=walk, 2=run
}
