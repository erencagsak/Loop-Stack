using UnityEngine;
using TMPro;
using DG.Tweening;
using Cysharp.Threading.Tasks;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("UI Panels")]
    public GameObject splashScreenPanel;
    public GameObject mainScreenPanel;
    public GameObject levelScreenPanel;
    public GameObject winScreenPanel;

    [Header("Loading Bar Assets")]
    public RectTransform loadingBarRect; 
    public TextMeshProUGUI loadingText;

    [Header("In-Game UI Texts")]
    public TextMeshProUGUI levelNumberText;
    public TextMeshProUGUI cubeProgressText;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (splashScreenPanel != null) splashScreenPanel.SetActive(true);
        if (mainScreenPanel != null) mainScreenPanel.SetActive(false);
        if (levelScreenPanel != null) levelScreenPanel.SetActive(false);
        if (winScreenPanel != null) winScreenPanel.SetActive(false);

        StartLoadingSequenceAsync().Forget();
    }

    private void OnEnable()
    {
        GameManager.OnLevelLoaded += UpdateLevelText;
        GameManager.OnLevelCompleted += ShowWinScreen;
    }

    private void OnDisable()
    {
        GameManager.OnLevelLoaded -= UpdateLevelText;
        GameManager.OnLevelCompleted -= ShowWinScreen;
    }

    private async UniTaskVoid StartLoadingSequenceAsync()
    {
        if (loadingBarRect != null)
        {
            loadingBarRect.pivot = new Vector2(0f, 0.5f);
            loadingBarRect.anchorMin = new Vector2(0f, 0f);
            loadingBarRect.anchorMax = new Vector2(0f, 1f);

            float fakeProgress = 0f;

            await DOTween.To(() => fakeProgress, x => 
            {
                fakeProgress = x;
                loadingBarRect.anchorMax = new Vector2(x, 1f);
                if (loadingText != null) loadingText.text = "%" + Mathf.RoundToInt(x * 100f);
            }, 1f, 2f).SetEase(Ease.InOutQuad).AsyncWaitForCompletion();

            if (loadingText != null) loadingText.text = "%100";
        }

        if (splashScreenPanel.TryGetComponent<CanvasGroup>(out CanvasGroup cg))
            await cg.DOFade(0f, 0.5f).AsyncWaitForCompletion();
        
        splashScreenPanel.SetActive(false);
        if (mainScreenPanel != null) mainScreenPanel.SetActive(true);
    }

    public void OnStartGameButtonClicked()
    {
        if (_isTransitioning) return;

        TransitionToGameAsync(false).Forget();
    }

    public void OnNextLevelButtonClicked()
    {
        if (_isTransitioning) return;

        TransitionToGameAsync(true).Forget();
    }

    private bool _isTransitioning = false;
    private async UniTaskVoid TransitionToGameAsync(bool isNextLevel)
    {
        _isTransitioning = true;

        if (isNextLevel) GameManager.Instance.LoadNextLevel();
        else GameManager.Instance.StartGame();

        await UniTask.Delay(150); 

        if (levelScreenPanel != null) levelScreenPanel.SetActive(true);

        if (isNextLevel)
        {
            if (winScreenPanel != null && winScreenPanel.TryGetComponent<CanvasGroup>(out CanvasGroup winCg))
                await winCg.DOFade(0f, 0.5f).AsyncWaitForCompletion();
            
            if (winScreenPanel != null) winScreenPanel.SetActive(false);
        }
        else
        {
            if (mainScreenPanel != null && mainScreenPanel.TryGetComponent<CanvasGroup>(out CanvasGroup mainCg))
                await mainCg.DOFade(0f, 0.5f).AsyncWaitForCompletion();
            
            if (mainScreenPanel != null) mainScreenPanel.SetActive(false);
        }

        _isTransitioning = false;
    }

    private void UpdateLevelText(int levelIndex)
    {
        if (levelNumberText != null)
            levelNumberText.text = $"Level {levelIndex}";
    }

    public void UpdateCubeProgressText(int current, int total)
    {
        if (cubeProgressText != null)
            cubeProgressText.text = $"{current} / {total}";
    }

    private void ShowWinScreen()
    {
        if (levelScreenPanel != null) levelScreenPanel.SetActive(false); 

        ShowWinScreenDelayedAsync(1.3f).Forget();
    }

    private async UniTaskVoid ShowWinScreenDelayedAsync(float delay)
    {
        await UniTask.Delay(Mathf.RoundToInt(delay * 1000));

        if (winScreenPanel != null)
        {
            if (!winScreenPanel.TryGetComponent<CanvasGroup>(out CanvasGroup cg))
                cg = winScreenPanel.AddComponent<CanvasGroup>();

            cg.alpha = 0f;

            winScreenPanel.SetActive(true);

            cg.DOFade(1f, 0.9f);
        }
    }
}