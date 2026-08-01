using UnityEngine;
using TMPro;

public class Tile : MonoBehaviour
{
    public SpriteRenderer sr;
    public TextMeshPro numberText;

    public Color hiddenColor = Color.gray;
    public Color revealedColor = Color.white;
    public Color flagColor = Color.yellow;
    public Color mineColor = Color.red;

    private int x, y;
    private GameManager game;

    public void Init(int x, int y, GameManager game)
    {
        this.x = x;
        this.y = y;
        this.game = game;
        sr.color = hiddenColor;
        numberText.text = ""; // empty until revealed
    }

    void OnMouseDown()
    {
        game.RevealCell(x, y);
    }

    void OnMouseOver()
    {
        if (Input.GetMouseButtonDown(1))
        {
            game.ToggleFlag(x, y);
        }
    }

    public void ShowRevealed(Cell cell)
    {
        if (cell.hasMine)
        {
            sr.color = mineColor;
        }
        else
        {
            sr.color = revealedColor;

            if (cell.adjacentMines > 0)
            {
                numberText.text = cell.adjacentMines.ToString();
            }
        }
    }

    public void ShowFlag(bool flagged)
    {
        sr.color = flagged ? flagColor : hiddenColor;
    }
}