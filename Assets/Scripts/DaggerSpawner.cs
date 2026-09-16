using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class DaggerSpawner : MonoBehaviour
{
    // ссылка на внутренние объекты
    private Ball _Ball;
    // ссылки на внешние компоненты
    public TextMeshProUGUI statsTextInfo; // ссылка на UI текст с информацией о статах
    private Ball opponentBall; // Ball шара оппонента
    // префабы
    public GameObject daggerPrefab; // префаб кинжала
    
    // данные о кд спавна кинжалов
    private float startCooldown = 1.6f; // стартовый кулдаун
    private float minCooldown   = 0.4f; // минимальный кулдаун
    private float curCooldown; // текущий кулдаун
    private float cooldownDecreaseOnHit = 0.035f; // ускорение спавна за одно попадание
    private float visualAttackSpeed = 1f; // визуализация атак спида кинжалов
    private float spawnDistance = 0.75f; // дистанция спавна кинжалов относительно центра шара
    // текущий таймер
    private float timer;
    // данные об ультимейте
    private float ultimateDuration      = 2f;   // продолжительность ультимейта
    private float ultimateSpawnCooldown = 0.1f; // кд спавна кинжалов во время ультимейта
    private bool ultimateActive; // активен ли ульт
    private readonly List<Dagger> pendingUltimateDaggers = new(); // список кинжалов появляемый во время ульты


    private void Awake()
    {
        _Ball = GetComponent<Ball>();

        // присвоение начального кд
        curCooldown = startCooldown;
    }

    private void Start()
    {
        foreach (Ball ball in FindObjectsByType<Ball>(FindObjectsSortMode.None))
        {
            if (ball != _Ball)
            {
                opponentBall = ball;
                break;
            }
        }

        statsTextInfo.text = $"Attack Speed: {visualAttackSpeed:F2}";
    }

    private void Update()
    {
        if (ultimateActive) return; // во время ульты спавном управляет корутина

        // начало ультимейта
        if (!WorldStasis.AnyUltimateActive && _Ball.curUltimateCharge >= _Ball.maxUltimateCharge)
        {
            StartCoroutine(UltimateRoutine());
            return;
        }

        timer += Time.deltaTime;
        if (timer < curCooldown) return;
        timer = 0f;

        // спавн кинжалов
        SpawnDagger(isDormant: false);
    
        statsTextInfo.text = $"Attack Speed: {visualAttackSpeed:F2}";
    }

    private void SpawnDagger(bool isDormant)
    {
        // нахождение направление движения кинжалов
        Vector3 direction = (opponentBall.transform.position - transform.position).normalized;
        Vector3 spawnPosition = transform.position + direction * spawnDistance;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        // создание кинжалов и выставление значений переменнных
        GameObject dagger = Instantiate(daggerPrefab, spawnPosition, Quaternion.Euler(0f, 0f, angle));
        Dagger daggerScript = dagger.GetComponent<Dagger>();
        daggerScript.ChangeDirection(direction);
        daggerScript.ownerBall = _Ball;
        daggerScript.spawner = this;
        daggerScript.ChangeIsDormant(isDormant);
        daggerScript.ChangeIsUltimateDagger(isDormant);
        if (isDormant)
            daggerScript.ChangePhaseForUlt();

        // добавление в список кинжалов
        if (isDormant) pendingUltimateDaggers.Add(daggerScript);
    }

    private IEnumerator UltimateRoutine()
    {
        ultimateActive = true;
        WorldStasis.AnyUltimateActive = true;
        pendingUltimateDaggers.Clear();

        // сохранение кд перед началом ульты
        float savedCooldown = curCooldown;
        curCooldown = ultimateSpawnCooldown;

        // начало заморозки времени
        WorldStasis.IsFrozen = true;
        WorldStasis.ExemptBall = _Ball;

        // выключение коллайдера оппонента
        opponentBall.GetComponent<Collider2D>().enabled = false;

        // спавн кинжалов во время ульты
        float elapsed = 0f;
        float spawnTimer = 0f;
        while (elapsed < ultimateDuration)
        {
            elapsed += Time.deltaTime;
            spawnTimer += Time.deltaTime;

            if (spawnTimer >= curCooldown)
            {
                spawnTimer = 0f;
                SpawnDagger(isDormant: true);
            }
            yield return null;
        }

        // конец заморозки времени
        WorldStasis.IsFrozen = false;

        // включение коллайдера оппонента
        opponentBall.GetComponent<Collider2D>().enabled = true;

        // одновременный запуск всех кинжалов
        foreach (Dagger d in pendingUltimateDaggers)
            if (d != null) d.ChangeIsDormant(false);
        pendingUltimateDaggers.Clear();

        // возвращение кулдауна, накопленного от обычных попаданий
        curCooldown = savedCooldown;

        // шкала и перемееные после конца ульты
        _Ball.ResetUltimateCharge();
        ultimateActive = false;
        WorldStasis.AnyUltimateActive = false;
    }

    public void IncreaseSpawnSpeed()
    {
        curCooldown = Mathf.Max(curCooldown - cooldownDecreaseOnHit, minCooldown);
        visualAttackSpeed += 0.06f;
    }
}