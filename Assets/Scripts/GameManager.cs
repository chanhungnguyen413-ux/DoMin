using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    [Header("Board Settings")]
    public int width = 8;
    public int height = 8;
    public int mineCount = 10;

    [Header("References")]
    public GameObject tilePrefab;
    public GameObject menuPanel;
    public GameObject gameOverPanel;
    public TextMeshProUGUI gameOverText;
    public TextMeshProUGUI flagCounterText;
    public GameObject restartButton; // NEW
    public GameObject menuButton; // NEW

    private Board board;
    private Tile[,] tileViews;
    private bool firstClickDone = false;
    private bool gameEnded = false;
    private int flagsPlaced = 0;

    void Start()
    {
        // Don't build the board yet - wait for Play button
        menuPanel.SetActive(true);
        gameOverPanel.SetActive(false);
        flagCounterText.gameObject.SetActive(false);
        restartButton.gameObject.SetActive(false); // NEW
        menuButton.gameObject.SetActive(false); // NEW
    }

    // This gets called by the Play Button's OnClick event
    public void StartGame()
    {
        menuPanel.SetActive(false);
        gameOverPanel.SetActive(false);
        flagCounterText.gameObject.SetActive(true); // NEW
        restartButton.gameObject.SetActive(true); // NEW
        menuButton.gameObject.SetActive(true); // NEW
        gameEnded = false;
        firstClickDone = false;
        flagsPlaced = 0; // NEW

        // If restarting, clear the old tiles first
        if (tileViews != null)
        {
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }
        }

        board = new Board(width, height);
        GenerateVisuals(); // create tiles, but WITHOUT mines yet

        Camera.main.transform.position = new Vector3((width - 1) / 2f, (height - 1) / 2f, -10);
        Camera.main.orthographicSize = Mathf.Max(width, height) / 1.5f;
        UpdateFlagCounter(); // NEW - show starting count
    }

    public void ReturnToMenu()
    {
        gameEnded = true; // prevents any lingering clicks from doing anything

        // Clear the board visuals
        if (tileViews != null)
        {
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }
        }

        menuPanel.SetActive(true);
        gameOverPanel.SetActive(false);
        flagCounterText.gameObject.SetActive(false);
        restartButton.gameObject.SetActive(false); // NEW
        menuButton.gameObject.SetActive(false); // NEW
    }
    void PlaceMines(int safeX, int safeY)
    {
        int placed = 0;
        while (placed < mineCount)
        {
            int x = Random.Range(0, width);
            int y = Random.Range(0, height);

            // Skip the clicked cell AND its neighbors - keeps first click safe
            bool tooCloseToSafeZone = Mathf.Abs(x - safeX) <= 1 && Mathf.Abs(y - safeY) <= 1;

            if (!board.cells[x, y].hasMine && !tooCloseToSafeZone)
            {
                board.cells[x, y].hasMine = true;
                placed++;
            }
        }
    }

    void CalculateAdjacency()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (board.cells[x, y].hasMine) continue;

                int count = 0;
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        int nx = x + dx;
                        int ny = y + dy;
                        if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                        {
                            if (board.cells[nx, ny].hasMine) count++;
                        }
                    }
                }
                board.cells[x, y].adjacentMines = count;
            }
        }
    }

    void GenerateVisuals()
    {
        tileViews = new Tile[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                GameObject go = Instantiate(tilePrefab, new Vector3(x, y, 0), Quaternion.identity, transform);
                Tile tile = go.GetComponent<Tile>();
                tile.Init(x, y, this);
                tileViews[x, y] = tile;
            }
        }
    }

    public void RevealCell(int x, int y)
    {
        if (gameEnded) return;

        Cell cell = board.cells[x, y];
        if (cell.isRevealed || cell.isFlagged) return;

        // First click ever - place mines now, guaranteed safe here
        if (!firstClickDone)
        {
            PlaceMines(x, y);
            CalculateAdjacency();
            firstClickDone = true;
        }

        cell.isRevealed = true;
        tileViews[x, y].ShowRevealed(cell);

        if (cell.hasMine)
        {
            GameOver(false);
            return;
        }

        if (cell.adjacentMines == 0)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    int nx = x + dx;
                    int ny = y + dy;
                    if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                    {
                        RevealCell(nx, ny);
                    }
                }
            }
        }

        CheckWin();
    }

    public void ToggleFlag(int x, int y)
    {
        if (gameEnded) return;

        Cell cell = board.cells[x, y];
        if (cell.isRevealed) return;

        cell.isFlagged = !cell.isFlagged;
        tileViews[x, y].ShowFlag(cell.isFlagged);

        // NEW - update the counter
        if (cell.isFlagged)
            flagsPlaced++;
        else
            flagsPlaced--;

        UpdateFlagCounter();
    }
    void UpdateFlagCounter()
    {
        int remaining = mineCount - flagsPlaced;
        flagCounterText.text = "Flags: " + remaining;
    }

    void CheckWin()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Cell cell = board.cells[x, y];
                if (!cell.hasMine && !cell.isRevealed)
                {
                    return;
                }
            }
        }

        GameOver(true);
    }

    void GameOver(bool won)
    {
        gameEnded = true;

        // Reveal EVERYTHING - all mines and all numbers
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Cell cell = board.cells[x, y];
                cell.isRevealed = true;
                tileViews[x, y].ShowRevealed(cell);
            }
        }

        gameOverPanel.SetActive(true);
        gameOverText.text = won ? "!YOU WIN!" : "!GAME OVER!";
        flagCounterText.gameObject.SetActive(false); // NEW
    }

    public void quitgame()
    {
        Application.Quit();
    }
}