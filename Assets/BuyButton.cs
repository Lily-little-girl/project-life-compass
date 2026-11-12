using UnityEngine;

public class BuyButton : MonoBehaviour
{
    public SkipButton game;           // ลาก skip ใส่ช่องนี้

    void OnMouseDown()
    {
        game.BuyMiner();
    }
}