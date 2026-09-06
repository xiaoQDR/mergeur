using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Mergeur.Game2048
{
    /// <summary>2048 rules rendered as a runtime-built 3D board below the Rive overlay.</summary>
    public sealed class Game2048 : MonoBehaviour
    {
        private const int Size = 4;
        private const int WinValue = 2048;
        private const string BestScoreKey = "mergeur.game2048.bestScore";
        private const float CellStep = 1.18f;
        private const float TileSize = 1.02f;

        private readonly int[,] board = new int[Size, Size];
        private readonly GameObject[,] tiles = new GameObject[Size, Size];
        private readonly MeshRenderer[,] renderers = new MeshRenderer[Size, Size];
        private readonly TextMesh[,] labels = new TextMesh[Size, Size];
        private readonly Dictionary<int, Material> tileMaterials = new Dictionary<int, Material>();
        [SerializeField] private GameObject[] sceneTiles = new GameObject[Size * Size];
        [SerializeField] private Material[] sceneTileMaterials = new Material[12];
        private static readonly int[] MaterialValues = { 2, 4, 8, 16, 32, 64, 128, 256, 512, 1024, 2048, 4096 };
        private int score;
        private int bestScore;
        private bool gameOver;
        private bool won;
        private bool continueAfterWin;
        private Vector2 pointerStart;
        private bool pointerTracking;

        private enum Direction { Left, Right, Up, Down }

        private void Awake()
        {
            Application.targetFrameRate = 60;
            Screen.orientation = ScreenOrientation.Portrait;
            bestScore = PlayerPrefs.GetInt(BestScoreKey, 0);
            CacheSceneObjects();
            NewGame();
        }

        private void Update()
        {
            HandleKeyboard();
            HandlePointer();
        }

        private void HandleKeyboard()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.rKey.wasPressedThisFrame) { NewGame(); return; }
            if (gameOver || won && !continueAfterWin) return;
            if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame) Move(Direction.Left);
            else if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame) Move(Direction.Right);
            else if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame) Move(Direction.Up);
            else if (keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame) Move(Direction.Down);
        }

        private void HandlePointer()
        {
            var touchscreen = Touchscreen.current;
            if (touchscreen != null)
            {
                var touch = touchscreen.primaryTouch;
                if (touch.press.wasPressedThisFrame) { pointerStart = touch.position.ReadValue(); pointerTracking = true; }
                if (pointerTracking && touch.press.wasReleasedThisFrame)
                {
                    TrySwipe(pointerStart, touch.position.ReadValue());
                    pointerTracking = false;
                }
                if (touch.press.isPressed || touch.press.wasPressedThisFrame || touch.press.wasReleasedThisFrame) return;
            }

            var mouse = Mouse.current;
            if (mouse == null) return;
            if (mouse.leftButton.wasPressedThisFrame) { pointerStart = mouse.position.ReadValue(); pointerTracking = true; }
            if (pointerTracking && mouse.leftButton.wasReleasedThisFrame)
            {
                TrySwipe(pointerStart, mouse.position.ReadValue());
                pointerTracking = false;
            }
        }

        private void TrySwipe(Vector2 start, Vector2 end)
        {
            if (gameOver || won && !continueAfterWin) return;
            Vector2 delta = end - start;
            float minimumSwipe = Mathf.Max(60f, Screen.dpi > 0f ? Screen.dpi * 0.22f : 90f);
            if (delta.sqrMagnitude < minimumSwipe * minimumSwipe) return;
            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y)) Move(delta.x > 0f ? Direction.Right : Direction.Left);
            else Move(delta.y > 0f ? Direction.Up : Direction.Down);
        }

        public void NewGame()
        {
            for (int y = 0; y < Size; y++) for (int x = 0; x < Size; x++) board[x, y] = 0;
            score = 0;
            gameOver = false;
            won = false;
            continueAfterWin = false;
            SpawnTile();
            SpawnTile();
            RefreshBoard();
        }

        public void ContinueGame()
        {
            continueAfterWin = true;
            won = false;
            RefreshBoard();
        }

        private void Move(Direction direction)
        {
            if (gameOver || won && !continueAfterWin) return;
            bool changed = false;
            int gainedScore = 0;
            for (int index = 0; index < Size; index++)
            {
                int[] original = ReadLine(index, direction);
                int[] processed = ProcessLine(original, out int lineScore);
                gainedScore += lineScore;
                if (!LinesEqual(original, processed))
                {
                    changed = true;
                    WriteLine(index, direction, processed);
                }
            }
            if (!changed) return;

            score += gainedScore;
            if (score > bestScore)
            {
                bestScore = score;
                PlayerPrefs.SetInt(BestScoreKey, bestScore);
                PlayerPrefs.Save();
            }
            SpawnTile();
            if (!continueAfterWin && ContainsValueAtLeast(WinValue)) won = true;
            if (!HasAvailableMove()) gameOver = true;
            RefreshBoard();
        }

        private int[] ReadLine(int index, Direction direction)
        {
            var line = new int[Size];
            for (int i = 0; i < Size; i++)
            {
                switch (direction)
                {
                    case Direction.Left: line[i] = board[i, index]; break;
                    case Direction.Right: line[i] = board[Size - 1 - i, index]; break;
                    case Direction.Up: line[i] = board[index, i]; break;
                    case Direction.Down: line[i] = board[index, Size - 1 - i]; break;
                }
            }
            return line;
        }

        private void WriteLine(int index, Direction direction, int[] line)
        {
            for (int i = 0; i < Size; i++)
            {
                switch (direction)
                {
                    case Direction.Left: board[i, index] = line[i]; break;
                    case Direction.Right: board[Size - 1 - i, index] = line[i]; break;
                    case Direction.Up: board[index, i] = line[i]; break;
                    case Direction.Down: board[index, Size - 1 - i] = line[i]; break;
                }
            }
        }

        private static int[] ProcessLine(int[] source, out int gainedScore)
        {
            var compact = new List<int>(Size);
            foreach (int value in source) if (value != 0) compact.Add(value);
            var result = new List<int>(Size);
            gainedScore = 0;
            for (int i = 0; i < compact.Count; i++)
            {
                int value = compact[i];
                if (i + 1 < compact.Count && compact[i + 1] == value)
                {
                    value *= 2;
                    gainedScore += value;
                    i++;
                }
                result.Add(value);
            }
            while (result.Count < Size) result.Add(0);
            return result.ToArray();
        }

        private static bool LinesEqual(int[] a, int[] b)
        {
            for (int i = 0; i < Size; i++) if (a[i] != b[i]) return false;
            return true;
        }

        private void SpawnTile()
        {
            var emptyCells = new List<Vector2Int>(Size * Size);
            for (int y = 0; y < Size; y++) for (int x = 0; x < Size; x++)
                if (board[x, y] == 0) emptyCells.Add(new Vector2Int(x, y));
            if (emptyCells.Count == 0) return;
            Vector2Int cell = emptyCells[Random.Range(0, emptyCells.Count)];
            board[cell.x, cell.y] = Random.value < 0.9f ? 2 : 4;
        }

        private bool ContainsValueAtLeast(int target)
        {
            for (int y = 0; y < Size; y++) for (int x = 0; x < Size; x++) if (board[x, y] >= target) return true;
            return false;
        }

        private bool HasAvailableMove()
        {
            for (int y = 0; y < Size; y++) for (int x = 0; x < Size; x++)
            {
                int value = board[x, y];
                if (value == 0 || x + 1 < Size && board[x + 1, y] == value ||
                    y + 1 < Size && board[x, y + 1] == value) return true;
            }
            return false;
        }

        private void CacheSceneObjects()
        {
            if (sceneTiles == null || sceneTiles.Length != Size * Size)
            {
                Debug.LogError("[2048] Bootstrap scene must contain 16 assigned Tile3D prefab instances.", this);
                enabled = false;
                return;
            }

            for (int y = 0; y < Size; y++) for (int x = 0; x < Size; x++)
            {
                GameObject tile = sceneTiles[y * Size + x];
                if (tile == null)
                {
                    Debug.LogError($"[2048] Missing scene tile at {x},{y}.", this);
                    enabled = false;
                    return;
                }

                tiles[x, y] = tile;
                renderers[x, y] = tile.GetComponent<MeshRenderer>();
                labels[x, y] = tile.GetComponentInChildren<TextMesh>(true);
            }
        }

        private void RefreshBoard()
        {
            for (int y = 0; y < Size; y++) for (int x = 0; x < Size; x++)
            {
                int value = board[x, y];
                GameObject tile = tiles[x, y];
                tile.SetActive(value != 0);
                if (value == 0) continue;
                renderers[x, y].sharedMaterial = GetTileMaterial(value);
                labels[x, y].text = value.ToString();
                labels[x, y].color = value <= 4 ? Hex("#3D312B") : Color.white;
                labels[x, y].characterSize = value < 100 ? 0.012f : value < 1000 ? 0.0105f : 0.0085f;
                float height = Mathf.Min(0.56f + Mathf.Log(value, 2f) * 0.025f, 0.88f);
                Vector3 position = CellPosition(x, y);
                tile.transform.localPosition = new Vector3(position.x, height * 0.5f + 0.06f, position.z);
                tile.transform.localScale = new Vector3(TileSize, height, TileSize);
            }
        }

        private Material GetTileMaterial(int value)
        {
            if (tileMaterials.TryGetValue(value, out Material material)) return material;
            int assetValue = value <= 2048 ? value : 4096;
            int index = System.Array.IndexOf(MaterialValues, assetValue);
            material = sceneTileMaterials != null && index >= 0 && index < sceneTileMaterials.Length
                ? sceneTileMaterials[index]
                : null;
            if (material == null) Debug.LogError($"[2048] Missing scene material for tile {assetValue}.", this);
            tileMaterials[value] = material;
            return material;
        }

        private static Vector3 CellPosition(int x, int y)
        {
            float half = (Size - 1) * 0.5f;
            return new Vector3((x - half) * CellStep, 0f, (half - y) * CellStep);
        }

        private static Color Hex(string value) => ColorUtility.TryParseHtmlString(value, out Color color) ? color : Color.white;
    }
}
