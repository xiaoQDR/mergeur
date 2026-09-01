using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Mergeur.Game2048
{
    public sealed class Game2048 : MonoBehaviour
    {
        private const int Size = 4;
        private const int WinValue = 2048;
        private const string BestScoreKey = "mergeur.game2048.bestScore";

        private readonly int[,] board = new int[Size, Size];

        private int score;
        private int bestScore;
        private bool gameOver;
        private bool won;
        private bool continueAfterWin;

        private Vector2 pointerStart;
        private bool pointerTracking;

        private enum Direction
        {
            Left,
            Right,
            Up,
            Down
        }

        private void Awake()
        {
            Application.targetFrameRate = 60;
            Screen.orientation = ScreenOrientation.Portrait;
            bestScore = PlayerPrefs.GetInt(BestScoreKey, 0);
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
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.rKey.wasPressedThisFrame)
            {
                NewGame();
                return;
            }

            if (gameOver || won && !continueAfterWin)
            {
                return;
            }

            if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame)
            {
                Move(Direction.Left);
            }
            else if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame)
            {
                Move(Direction.Right);
            }
            else if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame)
            {
                Move(Direction.Up);
            }
            else if (keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame)
            {
                Move(Direction.Down);
            }
        }

        private void HandlePointer()
        {
            var touchscreen = Touchscreen.current;
            if (touchscreen != null)
            {
                var touch = touchscreen.primaryTouch;
                if (touch.press.wasPressedThisFrame)
                {
                    pointerStart = touch.position.ReadValue();
                    pointerTracking = true;
                }

                if (pointerTracking && touch.press.wasReleasedThisFrame)
                {
                    TrySwipe(pointerStart, touch.position.ReadValue());
                    pointerTracking = false;
                }

                if (touch.press.isPressed || touch.press.wasPressedThisFrame || touch.press.wasReleasedThisFrame)
                {
                    return;
                }
            }

            var mouse = Mouse.current;
            if (mouse == null)
            {
                return;
            }

            if (mouse.leftButton.wasPressedThisFrame)
            {
                pointerStart = mouse.position.ReadValue();
                pointerTracking = true;
            }

            if (pointerTracking && mouse.leftButton.wasReleasedThisFrame)
            {
                TrySwipe(pointerStart, mouse.position.ReadValue());
                pointerTracking = false;
            }
        }

        private void TrySwipe(Vector2 start, Vector2 end)
        {
            if (gameOver || won && !continueAfterWin)
            {
                return;
            }

            Vector2 delta = end - start;
            float minimumSwipe = Mathf.Max(60f, Screen.dpi > 0f ? Screen.dpi * 0.22f : 90f);
            if (delta.sqrMagnitude < minimumSwipe * minimumSwipe)
            {
                return;
            }

            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            {
                Move(delta.x > 0f ? Direction.Right : Direction.Left);
            }
            else
            {
                Move(delta.y > 0f ? Direction.Up : Direction.Down);
            }
        }

        private void NewGame()
        {
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    board[x, y] = 0;
                }
            }

            score = 0;
            gameOver = false;
            won = false;
            continueAfterWin = false;

            SpawnTile();
            SpawnTile();
        }

        private void ContinueGame()
        {
            continueAfterWin = true;
            won = false;
        }

        private void Move(Direction direction)
        {
            if (gameOver || won && !continueAfterWin)
            {
                return;
            }

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

            if (!changed)
            {
                return;
            }

            score += gainedScore;
            if (score > bestScore)
            {
                bestScore = score;
                PlayerPrefs.SetInt(BestScoreKey, bestScore);
                PlayerPrefs.Save();
            }

            SpawnTile();

            if (!continueAfterWin && ContainsValueAtLeast(WinValue))
            {
                won = true;
            }

            if (!HasAvailableMove())
            {
                gameOver = true;
            }
        }

        private int[] ReadLine(int index, Direction direction)
        {
            var line = new int[Size];

            for (int i = 0; i < Size; i++)
            {
                switch (direction)
                {
                    case Direction.Left:
                        line[i] = board[i, index];
                        break;
                    case Direction.Right:
                        line[i] = board[Size - 1 - i, index];
                        break;
                    case Direction.Up:
                        line[i] = board[index, i];
                        break;
                    case Direction.Down:
                        line[i] = board[index, Size - 1 - i];
                        break;
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
                    case Direction.Left:
                        board[i, index] = line[i];
                        break;
                    case Direction.Right:
                        board[Size - 1 - i, index] = line[i];
                        break;
                    case Direction.Up:
                        board[index, i] = line[i];
                        break;
                    case Direction.Down:
                        board[index, Size - 1 - i] = line[i];
                        break;
                }
            }
        }

        private static int[] ProcessLine(int[] source, out int gainedScore)
        {
            var compact = new List<int>(Size);
            for (int i = 0; i < source.Length; i++)
            {
                if (source[i] != 0)
                {
                    compact.Add(source[i]);
                }
            }

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

            while (result.Count < Size)
            {
                result.Add(0);
            }

            return result.ToArray();
        }

        private static bool LinesEqual(int[] a, int[] b)
        {
            for (int i = 0; i < Size; i++)
            {
                if (a[i] != b[i])
                {
                    return false;
                }
            }

            return true;
        }

        private void SpawnTile()
        {
            var emptyCells = new List<Vector2Int>(Size * Size);

            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    if (board[x, y] == 0)
                    {
                        emptyCells.Add(new Vector2Int(x, y));
                    }
                }
            }

            if (emptyCells.Count == 0)
            {
                return;
            }

            Vector2Int cell = emptyCells[Random.Range(0, emptyCells.Count)];
            board[cell.x, cell.y] = Random.value < 0.9f ? 2 : 4;
        }

        private bool ContainsValueAtLeast(int target)
        {
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    if (board[x, y] >= target)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool HasAvailableMove()
        {
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    int value = board[x, y];
                    if (value == 0)
                    {
                        return true;
                    }

                    if (x + 1 < Size && board[x + 1, y] == value)
                    {
                        return true;
                    }

                    if (y + 1 < Size && board[x, y + 1] == value)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private void OnGUI()
        {
            float baseScale = Screen.width / 1080f;
            if (Screen.height / baseScale < 1200f)
            {
                baseScale = Screen.height / 1200f;
            }

            float logicalWidth = Screen.width / baseScale;
            float logicalHeight = Screen.height / baseScale;

            Matrix4x4 previousMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(baseScale, baseScale, 1f));

            DrawSolidRect(new Rect(0f, 0f, logicalWidth, logicalHeight), Hex("#FAF8EF"));

            float sidePadding = Mathf.Max(40f, (logicalWidth - 1000f) * 0.5f);
            float headerY = 72f;

            var titleStyle = CreateLabelStyle(108, FontStyle.Bold, TextAnchor.MiddleLeft, Hex("#776E65"));
            GUI.Label(new Rect(sidePadding, headerY, 430f, 130f), "2048", titleStyle);

            float statWidth = 190f;
            float statHeight = 112f;
            float statGap = 18f;
            float statsX = logicalWidth - sidePadding - statWidth * 2f - statGap;

            DrawStatCard(new Rect(statsX, headerY, statWidth, statHeight), "SCORE", score);
            DrawStatCard(new Rect(statsX + statWidth + statGap, headerY, statWidth, statHeight), "BEST", bestScore);

            var hintStyle = CreateLabelStyle(31, FontStyle.Normal, TextAnchor.MiddleLeft, Hex("#776E65"));
            GUI.Label(new Rect(sidePadding, 205f, logicalWidth - sidePadding * 2f - 250f, 66f),
                "Swipe to move. Join matching numbers and reach 2048.", hintStyle);

            var buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 30,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            if (GUI.Button(new Rect(logicalWidth - sidePadding - 210f, 210f, 210f, 62f), "NEW GAME", buttonStyle))
            {
                NewGame();
            }

            float boardTop = 320f;
            float boardSize = Mathf.Min(900f, logicalWidth - sidePadding * 2f, logicalHeight - boardTop - 70f);
            boardSize = Mathf.Max(520f, boardSize);

            float boardX = (logicalWidth - boardSize) * 0.5f;
            Rect boardRect = new Rect(boardX, boardTop, boardSize, boardSize);
            DrawSolidRect(boardRect, Hex("#BBADA0"));

            float gap = Mathf.Max(14f, boardSize * 0.018f);
            float cellSize = (boardSize - gap * 5f) / Size;

            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    Rect cellRect = new Rect(
                        boardX + gap + x * (cellSize + gap),
                        boardTop + gap + y * (cellSize + gap),
                        cellSize,
                        cellSize);

                    int value = board[x, y];
                    DrawSolidRect(cellRect, value == 0 ? Hex("#CDC1B4") : TileColor(value));

                    if (value == 0)
                    {
                        continue;
                    }

                    int fontSize = value < 100 ? 72 : value < 1000 ? 62 : value < 10000 ? 50 : 40;
                    var tileStyle = CreateLabelStyle(fontSize, FontStyle.Bold, TextAnchor.MiddleCenter, TileTextColor(value));
                    GUI.Label(cellRect, value.ToString(), tileStyle);
                }
            }

            if (won && !continueAfterWin)
            {
                DrawOverlay(boardRect, "You reached 2048!", true, buttonStyle);
            }
            else if (gameOver)
            {
                DrawOverlay(boardRect, "Game over", false, buttonStyle);
            }

            GUI.matrix = previousMatrix;
        }

        private void DrawOverlay(Rect boardRect, string message, bool canContinue, GUIStyle buttonStyle)
        {
            Color overlay = Hex("#EEE4DA");
            overlay.a = 0.88f;
            DrawSolidRect(boardRect, overlay);

            var messageStyle = CreateLabelStyle(66, FontStyle.Bold, TextAnchor.MiddleCenter, Hex("#776E65"));
            GUI.Label(new Rect(boardRect.x, boardRect.y + boardRect.height * 0.28f, boardRect.width, 120f), message, messageStyle);

            float buttonWidth = 260f;
            float buttonHeight = 78f;
            float centerX = boardRect.center.x;

            if (canContinue)
            {
                if (GUI.Button(new Rect(centerX - buttonWidth - 12f, boardRect.y + boardRect.height * 0.55f, buttonWidth, buttonHeight),
                        "KEEP GOING", buttonStyle))
                {
                    ContinueGame();
                }

                if (GUI.Button(new Rect(centerX + 12f, boardRect.y + boardRect.height * 0.55f, buttonWidth, buttonHeight),
                        "NEW GAME", buttonStyle))
                {
                    NewGame();
                }
            }
            else
            {
                if (GUI.Button(new Rect(centerX - buttonWidth * 0.5f, boardRect.y + boardRect.height * 0.55f, buttonWidth, buttonHeight),
                        "TRY AGAIN", buttonStyle))
                {
                    NewGame();
                }
            }
        }

        private static void DrawStatCard(Rect rect, string label, int value)
        {
            DrawSolidRect(rect, Hex("#BBADA0"));

            var labelStyle = CreateLabelStyle(24, FontStyle.Bold, TextAnchor.UpperCenter, Hex("#EEE4DA"));
            var valueStyle = CreateLabelStyle(42, FontStyle.Bold, TextAnchor.LowerCenter, Color.white);

            GUI.Label(new Rect(rect.x, rect.y + 12f, rect.width, 36f), label, labelStyle);
            GUI.Label(new Rect(rect.x, rect.y + 34f, rect.width, 62f), value.ToString(), valueStyle);
        }

        private static GUIStyle CreateLabelStyle(int fontSize, FontStyle fontStyle, TextAnchor alignment, Color color)
        {
            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = fontSize,
                fontStyle = fontStyle,
                alignment = alignment,
                wordWrap = false
            };
            style.normal.textColor = color;
            return style;
        }

        private static void DrawSolidRect(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private static Color TileTextColor(int value)
        {
            return value <= 4 ? Hex("#776E65") : Hex("#F9F6F2");
        }

        private static Color TileColor(int value)
        {
            switch (value)
            {
                case 2: return Hex("#EEE4DA");
                case 4: return Hex("#EDE0C8");
                case 8: return Hex("#F2B179");
                case 16: return Hex("#F59563");
                case 32: return Hex("#F67C5F");
                case 64: return Hex("#F65E3B");
                case 128: return Hex("#EDCF72");
                case 256: return Hex("#EDCC61");
                case 512: return Hex("#EDC850");
                case 1024: return Hex("#EDC53F");
                case 2048: return Hex("#EDC22E");
                default: return Hex("#3C3A32");
            }
        }

        private static Color Hex(string value)
        {
            if (ColorUtility.TryParseHtmlString(value, out Color color))
            {
                return color;
            }

            return Color.white;
        }
    }
}
