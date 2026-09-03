using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

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

        private readonly Image[] tileBackgrounds = new Image[Size * Size];
        private readonly Text[] tileLabels = new Text[Size * Size];

        private Font uiFont;
        private Text scoreLabel;
        private Text bestScoreLabel;
        private GameObject resultOverlay;
        private Text resultMessage;
        private GameObject continueButtonObject;

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
            EnsureEventSystem();
            BuildUI();
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
            RefreshUI();
        }

        private void ContinueGame()
        {
            continueAfterWin = true;
            won = false;
            RefreshUI();
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

            RefreshUI();
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

        private void BuildUI()
        {
            uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var canvasObject = CreateUIObject("Game UI", transform);
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = -100;
            canvasObject.AddComponent<GraphicRaycaster>();

            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            Image background = CreateImage("Background", canvasObject.transform, Hex("#FAF8EF"));
            Stretch(background.rectTransform);

            var content = CreateUIObject("Content", canvasObject.transform).GetComponent<RectTransform>();
            content.anchorMin = new Vector2(0.5f, 1f);
            content.anchorMax = new Vector2(0.5f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(1000f, 1400f);

            Text title = CreateText("Title", content, "2048", 108, FontStyle.Bold, TextAnchor.MiddleLeft, Hex("#776E65"));
            SetRect(title.rectTransform, new Vector2(0f, -72f), new Vector2(430f, 130f), new Vector2(0f, 1f));

            scoreLabel = CreateStatCard("Score", content, "SCORE", new Vector2(600f, -72f));
            bestScoreLabel = CreateStatCard("Best", content, "BEST", new Vector2(808f, -72f));

            Text hint = CreateText("Hint", content, "Swipe to move. Join matching numbers and reach 2048.",
                31, FontStyle.Normal, TextAnchor.MiddleLeft, Hex("#776E65"));
            SetRect(hint.rectTransform, new Vector2(0f, -205f), new Vector2(730f, 66f), new Vector2(0f, 1f));

            Button newGameButton = CreateButton("New Game", content, "NEW GAME", new Vector2(790f, -210f), new Vector2(210f, 62f));
            newGameButton.onClick.AddListener(NewGame);

            Image boardBackground = CreateImage("Board", content, Hex("#BBADA0"));
            SetRect(boardBackground.rectTransform, new Vector2(50f, -320f), new Vector2(900f, 900f), new Vector2(0f, 1f));

            var gridObject = CreateUIObject("Tiles", boardBackground.transform);
            var gridRect = gridObject.GetComponent<RectTransform>();
            Stretch(gridRect, 16f);
            var grid = gridObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(205f, 205f);
            grid.spacing = new Vector2(16f, 16f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = Size;
            grid.childAlignment = TextAnchor.UpperLeft;

            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    int index = y * Size + x;
                    Image tile = CreateImage($"Tile {x + 1},{y + 1}", gridObject.transform, Hex("#CDC1B4"));
                    Text label = CreateText("Value", tile.transform, string.Empty, 72, FontStyle.Bold,
                        TextAnchor.MiddleCenter, Hex("#776E65"));
                    Stretch(label.rectTransform);
                    tileBackgrounds[index] = tile;
                    tileLabels[index] = label;
                }
            }

            BuildResultOverlay(boardBackground.transform);
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null || FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            var eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<InputSystemUIInputModule>();
        }

        private void BuildResultOverlay(Transform boardTransform)
        {
            Image overlayImage = CreateImage("Result Overlay", boardTransform, WithAlpha(Hex("#EEE4DA"), 0.88f));
            Stretch(overlayImage.rectTransform);
            resultOverlay = overlayImage.gameObject;

            resultMessage = CreateText("Message", overlayImage.transform, string.Empty, 66, FontStyle.Bold,
                TextAnchor.MiddleCenter, Hex("#776E65"));
            SetRect(resultMessage.rectTransform, new Vector2(0f, -250f), new Vector2(900f, 120f), new Vector2(0.5f, 1f));

            continueButtonObject = CreateButton("Keep Going", overlayImage.transform, "KEEP GOING",
                new Vector2(-142f, -500f), new Vector2(260f, 78f), new Vector2(0.5f, 1f)).gameObject;
            continueButtonObject.GetComponent<Button>().onClick.AddListener(ContinueGame);

            Button restartButton = CreateButton("Restart", overlayImage.transform, "NEW GAME",
                new Vector2(142f, -500f), new Vector2(260f, 78f), new Vector2(0.5f, 1f));
            restartButton.onClick.AddListener(NewGame);
        }

        private Text CreateStatCard(string name, Transform parent, string caption, Vector2 position)
        {
            Image card = CreateImage(name, parent, Hex("#BBADA0"));
            SetRect(card.rectTransform, position, new Vector2(190f, 112f), new Vector2(0f, 1f));

            Text captionLabel = CreateText("Caption", card.transform, caption, 24, FontStyle.Bold,
                TextAnchor.UpperCenter, Hex("#EEE4DA"));
            SetRect(captionLabel.rectTransform, new Vector2(0f, -12f), new Vector2(190f, 36f), new Vector2(0.5f, 1f));

            Text valueLabel = CreateText("Value", card.transform, "0", 42, FontStyle.Bold,
                TextAnchor.MiddleCenter, Color.white);
            SetRect(valueLabel.rectTransform, new Vector2(0f, -42f), new Vector2(190f, 62f), new Vector2(0.5f, 1f));
            return valueLabel;
        }

        private Button CreateButton(string name, Transform parent, string label, Vector2 position, Vector2 size,
            Vector2? anchor = null)
        {
            Image image = CreateImage(name, parent, Hex("#8F7A66"));
            image.raycastTarget = true;
            SetRect(image.rectTransform, position, size, anchor ?? new Vector2(0f, 1f));

            var button = image.gameObject.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Hex("#F2B179");
            colors.pressedColor = Hex("#EDC22E");
            button.colors = colors;

            Text text = CreateText("Label", image.transform, label, 28, FontStyle.Bold,
                TextAnchor.MiddleCenter, Color.white);
            Stretch(text.rectTransform);
            return button;
        }

        private Text CreateText(string name, Transform parent, string value, int fontSize, FontStyle fontStyle,
            TextAnchor alignment, Color color)
        {
            var textObject = CreateUIObject(name, parent);
            var text = textObject.AddComponent<Text>();
            text.font = uiFont;
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            text.resizeTextForBestFit = false;
            return text;
        }

        private static Image CreateImage(string name, Transform parent, Color color)
        {
            var imageObject = CreateUIObject(name, parent);
            var image = imageObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static GameObject CreateUIObject(string name, Transform parent)
        {
            var result = new GameObject(name, typeof(RectTransform));
            result.transform.SetParent(parent, false);
            return result;
        }

        private static void SetRect(RectTransform rect, Vector2 position, Vector2 size, Vector2 anchor)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(anchor.x, anchor.y);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect, float inset = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }

        private void RefreshUI()
        {
            if (scoreLabel == null)
            {
                return;
            }

            scoreLabel.text = score.ToString();
            bestScoreLabel.text = bestScore.ToString();

            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    int index = y * Size + x;
                    int value = board[x, y];
                    tileBackgrounds[index].color = value == 0 ? Hex("#CDC1B4") : TileColor(value);
                    tileLabels[index].text = value == 0 ? string.Empty : value.ToString();
                    tileLabels[index].color = TileTextColor(value);
                    tileLabels[index].fontSize = value < 100 ? 72 : value < 1000 ? 62 : value < 10000 ? 50 : 40;
                }
            }

            bool showWin = won && !continueAfterWin;
            bool showResult = showWin || gameOver;
            resultOverlay.SetActive(showResult);
            if (!showResult)
            {
                return;
            }

            resultMessage.text = showWin ? "You reached 2048!" : "Game over";
            continueButtonObject.SetActive(showWin);

            Button restartButton = resultOverlay.transform.Find("Restart").GetComponent<Button>();
            Text restartLabel = restartButton.GetComponentInChildren<Text>();
            restartLabel.text = showWin ? "NEW GAME" : "TRY AGAIN";
            RectTransform restartRect = restartButton.GetComponent<RectTransform>();
            restartRect.anchoredPosition = showWin ? new Vector2(142f, -500f) : new Vector2(0f, -500f);
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
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
