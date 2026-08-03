using UnityEngine;
using TMPro;

public class Tile : MonoBehaviour
{
    public SpriteRenderer sr;
    public TextMeshPro numberText;

    public Sprite tileSprite;
    public Color hiddenColor = new Color(0.85f, 0.78f, 0.95f);
    public Color revealedColor = new Color(1f, 0.95f, 0.9f);
    public Color flagColor = new Color(1f, 0.85f, 0.9f);
    public Color mineColor = new Color(1f, 0.7f, 0.75f);

    public float tileSize = 1f;

    private int x, y;
    private GameManager game;
    private Sprite mosaicPiece;

    public void Init(int x, int y, GameManager game)
    {
        this.x = x;
        this.y = y;
        this.game = game;

        sr.drawMode = SpriteDrawMode.Sliced;
        sr.size = new Vector2(tileSize, tileSize);
        sr.color = hiddenColor;
        numberText.text = "";
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

    public void SetMosaicPiece(Sprite piece)
    {
        mosaicPiece = piece;
    }

    public void ShowRevealed(Cell cell)
    {
        sr.drawMode = SpriteDrawMode.Sliced;
        sr.size = new Vector2(tileSize, tileSize);

        if (cell.hasMine)
        {
            sr.sprite = tileSprite;
            sr.color = mineColor;
        }
        else
        {
            if (mosaicPiece != null)
            {
                sr.sprite = mosaicPiece;
                sr.color = Color.white;
            }
            else
            {
                sr.sprite = tileSprite;
                sr.color = revealedColor;
            }

            if (cell.adjacentMines > 0)
            {
                numberText.text = cell.adjacentMines.ToString();
            }
        }
    }

    public void ShowFlag(bool flagged)
    {
        sr.drawMode = SpriteDrawMode.Sliced;
        sr.size = new Vector2(tileSize, tileSize);
        sr.sprite = flagged ? tileSprite : tileSprite;
        sr.color = flagged ? flagColor : hiddenColor;
    }
}