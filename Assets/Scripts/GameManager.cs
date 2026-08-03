using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{

    [Header("Board Settings")]
    public int width = 8;
    public int height = 8;
    public int mineCount = 10;
    public float tileSpacing = 1.1f;

    [Header("References")]
    public GameObject tilePrefab;
    public GameObject menuPanel;
    public GameObject gameOverPanel;
    public TextMeshProUGUI gameOverText;
    public TextMeshProUGUI flagCounterText;
    public GameObject restartButton;
    public GameObject menuButton;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI bestScoreText;
    public GameObject scoreBackground;
    public GameObject bestScoreBackground;

    [Header("Tile Art")]
    public Sprite[] artPieces;

    private Board board;
    private Tile[,] tileViews;
    private bool firstClickDone = false;
    private bool gameEnded = false;
    private int flagsPlaced = 0;
    private float elapsedTime = 0f;
    private bool timerRunning = false;
    private int currentScore = 0;

    void Start()
    {
        menuPanel.SetActive(true);
        gameOverPanel.SetActive(false);
        flagCounterText.gameObject.SetActive(false);
        restartButton.gameObject.SetActive(false);
        menuButton.gameObject.SetActive(false);
        timerText.gameObject.SetActive(false);
        scoreBackground.SetActive(false);
        bestScoreBackground.SetActive(false);
    }

    public void StartGame()
    {
        menuPanel.SetActive(false);
        gameOverPanel.SetActive(false);
        flagCounterText.gameObject.SetActive(true);
        restartButton.gameObject.SetActive(true);
        menuButton.gameObject.SetActive(true);
        timerText.gameObject.SetActive(true);
        scoreBackground.SetActive(true);
        bestScoreBackground.SetActive(true);
        gameEnded = false;
        firstClickDone = false;
        flagsPlaced = 0;
        elapsedTime = 0f;
        timerRunning = false;
        timerText.text = "Time: 0";
        scoreText.text = "Score: 0";

        int savedBest = PlayerPrefs.GetInt("BestScore", 0);
        bestScoreText.text = "Best: " + savedBest;

        if (tileViews != null)
        {
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }
        }

        board = new Board(width, height);
        GenerateVisuals();
        AssignRandomArt();
        FitCameraToBoard();
        UpdateFlagCounter();
    }

    void FitCameraToBoard()
    {
        Camera cam = Camera.main;
        float spacing = 1.1f;
        cam.transform.position = new Vector3(((width - 1) * spacing) / 2f, ((height - 1) * spacing) / 2f, -10);

        float padding = 1f;
        float boardAspect = (float)width / height;
        float screenAspect = (float)Screen.width / Screen.height;

        if (screenAspect >= boardAspect)
        {
            cam.orthographicSize = ((height * spacing) / 2f) + padding;
        }
        else
        {
            cam.orthographicSize = (((width * spacing) / 2f) + padding) / screenAspect;
        }
    }
    public void ReturnToMenu()
    {
        gameEnded = true;
        timerRunning = false;

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
        restartButton.gameObject.SetActive(false);
        menuButton.gameObject.SetActive(false);
        timerText.gameObject.SetActive(false);
        scoreBackground.SetActive(false);
        bestScoreBackground.SetActive(false);
    }
    void PlaceMines(int safeX, int safeY)
    {
        int placed = 0;
        while (placed < mineCount)
        {
            int x = Random.Range(0, width);
            int y = Random.Range(0, height);

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
        float spacing = 1.1f;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 pos = new Vector3(x * spacing, y * spacing, 0);
                GameObject go = Instantiate(tilePrefab, pos, Quaternion.identity, transform);
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

        if (!firstClickDone)
        {
            TryGenerateSolvableBoard(x, y);
            firstClickDone = true;
            timerRunning = true;
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
        timerRunning = false;

        int revealedSafeTiles = CountRevealedSafeTiles();
        float timeUsed = Mathf.Max(elapsedTime, 1f);

        if (won)
        {
            currentScore = Mathf.RoundToInt((revealedSafeTiles * 100) / timeUsed);
        }
        else
        {
            currentScore = 0;
        }

        scoreText.text = "Score: " + currentScore;

        int savedBest = PlayerPrefs.GetInt("BestScore", 0);
        if (currentScore > savedBest)
        {
            PlayerPrefs.SetInt("BestScore", currentScore);
            PlayerPrefs.Save();
            savedBest = currentScore;
        }
        bestScoreText.text = "Best: " + savedBest;

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
        flagCounterText.gameObject.SetActive(false);
    }

    public void quitgame()
    {
        Application.Quit();
    }
    private int lastScreenWidth;
    private int lastScreenHeight;

    void Update()
    {
        if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
        {
            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;
            if (board != null) FitCameraToBoard();
        }

        if (timerRunning)
        {
            elapsedTime += Time.deltaTime;
            timerText.text = "Time: " + Mathf.FloorToInt(elapsedTime);

            int revealedSafeTiles = CountRevealedSafeTiles();
            float timeUsed = Mathf.Max(elapsedTime, 1f);
            int liveScore = Mathf.RoundToInt((revealedSafeTiles * 100) / timeUsed);
            scoreText.text = "Score: " + liveScore;
        }
    }
    int CountRevealedSafeTiles()
    {
        int count = 0;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Cell cell = board.cells[x, y];
                if (cell.isRevealed && !cell.hasMine)
                {
                    count++;
                }
            }
        }
        return count;
    }
    bool TryGenerateSolvableBoard(int safeX, int safeY)
    {
        int maxAttempts = 150;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            board = new Board(width, height);
            PlaceMines(safeX, safeY);
            CalculateAdjacency();

            if (IsSolvable(safeX, safeY))
            {
                return true;
            }
        }

        return false;
    }

    bool IsSolvable(int startX, int startY)
    {
        bool[,] known = new bool[width, height];
        bool[,] knownMine = new bool[width, height];

        SimulateReveal(startX, startY, known);

        bool progress = true;
        while (progress)
        {
            progress = false;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (!known[x, y] || board.cells[x, y].hasMine) continue;

                    int mineCountHere = board.cells[x, y].adjacentMines;
                    var neighbors = GetNeighbors(x, y);

                    int knownMinesAround = 0;
                    int unknownAround = 0;
                    List<Vector2Int> unknownNeighbors = new List<Vector2Int>();

                    foreach (var n in neighbors)
                    {
                        if (knownMine[n.x, n.y]) knownMinesAround++;
                        else if (!known[n.x, n.y])
                        {
                            unknownAround++;
                            unknownNeighbors.Add(n);
                        }
                    }

                    if (knownMinesAround == mineCountHere && unknownAround > 0)
                    {
                        foreach (var n in unknownNeighbors)
                        {
                            SimulateReveal(n.x, n.y, known);
                        }
                        progress = true;
                    }
                    else if (knownMinesAround + unknownAround == mineCountHere && unknownAround > 0)
                    {
                        foreach (var n in unknownNeighbors)
                        {
                            knownMine[n.x, n.y] = true;
                        }
                        progress = true;
                    }
                }
            }
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (!board.cells[x, y].hasMine && !known[x, y])
                {
                    return false;
                }
            }
        }

        return true;
    }

    void SimulateReveal(int x, int y, bool[,] known)
    {
        if (known[x, y]) return;
        known[x, y] = true;

        Cell cell = board.cells[x, y];
        if (cell.adjacentMines == 0)
        {
            foreach (var n in GetNeighbors(x, y))
            {
                SimulateReveal(n.x, n.y, known);
            }
        }
    }

    List<Vector2Int> GetNeighbors(int x, int y)
    {
        List<Vector2Int> result = new List<Vector2Int>();
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                int nx = x + dx;
                int ny = y + dy;
                if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                {
                    result.Add(new Vector2Int(nx, ny));
                }
            }
        }
        return result;
    }
    void AssignRandomArt()
    {
        if (artPieces == null || artPieces.Length == 0) return;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Sprite randomPiece = artPieces[Random.Range(0, artPieces.Length)];
                tileViews[x, y].SetMosaicPiece(randomPiece);
            }
        }
    }
}