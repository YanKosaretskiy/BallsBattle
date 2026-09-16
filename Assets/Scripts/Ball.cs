using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Ball : MonoBehaviour
{
    // сслыки на внутренние компоненты
    private Rigidbody2D _Rigidbody2D;
    private TextMeshPro _TextMeshProHp; // сслыка на визуальный хп
    // ссылки на внешние компоненты
    public Slider ultimateSlider; // ссылка на слайдер
    public Ball opponent { get; private set; } // ссылка на другой шар

    // данные о шаре
    public bool isPlayer;
    public static event System.Action<Ball> OnBallDied;
    // данные о здоровье
    private int hp = 100; // реальное значение хп
    // данные о позиции
    private Vector3 spawnPos = new(-2, 0, 0); // начальная позиция
    // данные о направлении движения
    private Vector3 startDirection = new(-0.9f, -0.2f, 0f); // начальное направление движение
    public Vector3 curDirection { get; private set; }       // текущее направление движения
    // данные о скорости и ускорении
    private float startSpeed = 3f; // начальная скорость
    private float maxSpeed = 7f; // максимальная скорость
    private float accelerationTime = 2f; // сколько секунд разгоняется от startSpeed до maxSpeed
    private float curSpeed;              // текущая скорость
    private float acceleration;          // прирост скорости в секунду

    // взаимодействие со слайдером
    public float maxUltimateCharge {get; private set;} // максимальнео значение слайдера
    public float curUltimateCharge { get; private set; } // текущий заряд ульты(значение слайдера)
    private float ultimateChargeRate = 5f;  // скорость заполнения слайдера с течением времени
    private bool isHidden;                  // для ульты мечника — не двигается, невидим, неуязвим

    // граница поля
    private float limit = 2.5f;


    private void Awake()
    {
        _Rigidbody2D = GetComponent<Rigidbody2D>();
        _Rigidbody2D.bodyType = RigidbodyType2D.Kinematic;
        _Rigidbody2D.useFullKinematicContacts = true;

        // присвоение хп шару
        _TextMeshProHp = transform.Find("Hp").GetComponent<TextMeshPro>();
        _TextMeshProHp.text = hp.ToString();

        // вычисление скорости и ускорения
        curSpeed = startSpeed;
        acceleration = (maxSpeed - startSpeed) / accelerationTime;

        // выставление значений слайдеру
        maxUltimateCharge = 100f;
        ultimateSlider.maxValue = maxUltimateCharge;
        ultimateSlider.minValue = 0f;
        ultimateSlider.value = 0f;
    }

    private void Start()
    {
        // поиск шара оппонента
        foreach (Ball ball in FindObjectsByType<Ball>(FindObjectsSortMode.None))
        {
            if (ball != this)
            {
                opponent = ball;
                break;
            }
        }

        // расстановка по стартовым позициям и выставление начального направления движения в зависимости от того игрок или враг
        if (isPlayer)
        {
            transform.position = spawnPos;
            curDirection = startDirection;
        }
        else
        {
            transform.position = -spawnPos;
            curDirection = -startDirection;
        }
    }

    private void Update()
    {
        if (WorldStasis.IsFrozen && this != WorldStasis.ExemptBall) return; // заморозка от ульты кинжальщика
        if (isHidden) return; // спрятан во время своей собственной ульты

        // увеличение скорости на acceleration, до ограничения в maxSpeed
        curSpeed = Mathf.MoveTowards(curSpeed, maxSpeed, acceleration * Time.deltaTime);

        // отражение от стены
        Vector3 reflected = curDirection;
        bool bounced = false;

        if (transform.position.x <= -limit)
        {
            reflected.x = Mathf.Abs(reflected.x);
            bounced = true;
        }
        else if (transform.position.x >= limit)
        {
            reflected.x = -Mathf.Abs(reflected.x);
            bounced = true;
        }

        if (transform.position.y <= -limit)
        {
            reflected.y = Mathf.Abs(reflected.y);
            bounced = true;
        }
        else if (transform.position.y >= limit)
        {
            reflected.y = -Mathf.Abs(reflected.y);
            bounced = true;
        }

        if (bounced && opponent != null)
        {
            Vector3 thisToOther = (opponent.transform.position - transform.position).normalized;
            curDirection = Vector3.Slerp(reflected.normalized, thisToOther, 0.25f + Random.Range(-0.1f, 0.1f)).normalized; // приближение к оппоненту при отскоке от стены
        }

        transform.Translate(curDirection * curSpeed * Time.deltaTime);

        AddUltimateCharge(ultimateChargeRate * Time.deltaTime);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Ball otherBall = collision.gameObject.GetComponent<Ball>();
        if (otherBall == null) return;

        Vector3 normal = (transform.position - otherBall.transform.position).normalized; // вычисление нормали поверхности шара(оппонента)

        curDirection = Vector3.Reflect(curDirection, normal).normalized; // вычисление направления отскока от шара(оппонента)
        curDirection += new Vector3(Random.Range(-0.1f, 0.1f), Random.Range(-0.1f, 0.1f), 0f);
    }


    public void GetDamage(int damage)
    {
        hp -= damage;
        if (hp <= 0)
        {
            gameObject.SetActive(false);
            OnBallDied?.Invoke(this);
        }

        _TextMeshProHp.text = hp.ToString();
    }

    public void AddUltimateCharge(float amount)
    {
        curUltimateCharge = Mathf.Min(curUltimateCharge + amount, maxUltimateCharge);
        ultimateSlider.value = curUltimateCharge;
    }

    public void ResetUltimateCharge()
    {
        curUltimateCharge = 0f;
        ultimateSlider.value = 0f;
    }

    public void SetHidden(bool hidden)
    {
        isHidden = hidden;
        GetComponent<Collider2D>().enabled = !hidden;
        transform.GetComponent<SpriteRenderer>().enabled = !hidden;
        transform.Find("Inside").GetComponent<SpriteRenderer>().enabled = !hidden;
        transform.Find("SwordCenter").GetComponent<LineRenderer>().enabled = !hidden;
        _TextMeshProHp.GetComponent<TextMeshPro>().enabled = !hidden;
    }

}