using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SampleMiniGame : MiniGameBase
{
    public const string Id = "SampleGame";
    public override string GameId => Id;

    private enum Phase { Playing, ResultDisplay }
    private Phase _phase;

    private int _targetScore;
    private float _timeLimit;
    private float _timer;
    private int _currentScore;

    private Text _titleText;
    private Text _scoreText;
    private Text _timerText;
    private Text _resultText;

    private RectTransform _dropZone;
    private readonly List<DraggableItem> _orbs = new();
    private readonly List<ClickableItem> _targets = new();

    private float _resultTimer;
    private float _targetSpawnTimer;

    public override void StartGame()
    {
        _targetScore = Settings != null ? Settings.TargetScore : 5;
        _timeLimit = Settings != null ? Settings.TimeLimit : 10f;
        _currentScore = 0;
        _timer = _timeLimit;
        _phase = Phase.Playing;

        string gameName = Settings != null ? Settings.GameName : "点击与拖拽";

        _titleText = CreateText("Title",
            $"{gameName}\n拖拽蓝球到目标框 + 点击红点得分！目标: {_targetScore} 分", 28, Color.white,
            position: new Vector2(0, 350));

        _scoreText = CreateText("Score", "0 / " + _targetScore, 44, new Color(0.3f, 0.9f, 0.4f),
            position: new Vector2(0, 250));

        _timerText = CreateText("Timer", _timer.ToString("F1") + "s", 32, new Color(1f, 0.8f, 0.2f),
            position: new Vector2(0, 180));

        // 目标投放区（右侧）
        _dropZone = CreateDropZone("DropZone", new Vector2(300, 250), new Vector2(350, -50));

        var zoneLabel = CreateText("ZoneLabel", "拖到此处", 24, new Color(0.5f, 0.5f, 0.6f),
            position: new Vector2(350, -50));
        zoneLabel.GetComponent<RectTransform>().SetAsLastSibling();

        // 初始生成 3 个可拖拽球
        for (int i = 0; i < 3; i++)
        {
            SpawnOrb(new Vector2(-300 + i * 80, -50));
        }

        _resultText = CreateText("Result", "", 42, Color.white,
            position: new Vector2(0, -200));
        _resultText.gameObject.SetActive(false);
    }

    public override void UpdateGame()
    {
        if (_phase == Phase.Playing)
        {
            _timer -= Time.deltaTime;
            _timerText.text = Mathf.Max(0, _timer).ToString("F1") + "s";

            // 检查拖拽球是否进入目标区域
            for (int i = _orbs.Count - 1; i >= 0; i--)
            {
                var orb = _orbs[i];
                if (!orb.IsDragging && orb.gameObject.activeSelf)
                {
                    var orbRt = orb.GetComponent<RectTransform>();
                    if (IsInside(orbRt, _dropZone))
                    {
                        _currentScore++;
                        _scoreText.text = _currentScore + " / " + _targetScore;
                        orb.gameObject.SetActive(false);
                        _orbs.RemoveAt(i);

                        SpawnOrb(new Vector2(-300 + Random.Range(-40, 40), -50 + Random.Range(-40, 40)));
                    }
                }
            }

            // 定期生成点击目标
            _targetSpawnTimer -= Time.deltaTime;
            if (_targetSpawnTimer <= 0 && _targets.Count < 3)
            {
                SpawnClickTarget();
                _targetSpawnTimer = 1.5f;
            }

            if (_currentScore >= _targetScore)
            {
                ShowResult(true);
            }
            else if (_timer <= 0f)
            {
                ShowResult(false);
            }
        }
        else if (_phase == Phase.ResultDisplay)
        {
            _resultTimer -= Time.deltaTime;
            if (_resultTimer <= 0f)
            {
                IsComplete = true;
            }
        }
    }

    private void SpawnOrb(Vector2 position)
    {
        var orb = CreateDraggable("Orb", new Color(0.3f, 0.6f, 1f), new Vector2(70, 70), position);
        _orbs.Add(orb);
    }

    private void SpawnClickTarget()
    {
        Vector2 pos = new Vector2(
            Random.Range(-400, 0),
            Random.Range(-100, 150));

        ClickableItem target = null;
        target = CreateClickable("Target", new Color(1f, 0.25f, 0.25f), new Vector2(50, 50), pos,
            onClick: () =>
            {
                _currentScore++;
                _scoreText.text = _currentScore + " / " + _targetScore;
                target.gameObject.SetActive(false);
                _targets.Remove(target);
            });

        _targets.Add(target);
    }

    private void ShowResult(bool success)
    {
        _phase = Phase.ResultDisplay;
        IsSuccess = success;
        _resultTimer = 1.5f;

        _resultText.text = success ? "成功!" : "失败!";
        _resultText.color = success ? new Color(0.3f, 0.9f, 0.4f) : new Color(1f, 0.3f, 0.3f);
        _resultText.gameObject.SetActive(true);
    }

    public override void EndGame()
    {
    }
}
