public class Board
{
    public int width;
    public int height;
    public Cell[,] cells; // a grid of Cell cards - [,] means 2D grid

    public Board(int width, int height)
    {
        this.width = width;
        this.height = height;
        cells = new Cell[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                cells[x, y] = new Cell(); // give every square a blank card
            }
        }
    }
}